// Module purpose: Verifies central PostgreSQL admission, one-time IP pinning, offline TLS and readiness containment.
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Provider.Abstractions;
using DBNotifier.Server.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DBNotifier.UnitTests;

/// <summary>Protects the fail-closed central PostgreSQL network boundary without opening a real connection.</summary>
public sealed class ServerDatabaseNetworkSecurityTests
{
    private const string ValidConnectionString =
        "Host=database.example;Port=5432;Database=dbnotifier;" +
        "SSL Mode=VerifyFull;Check Certificate Revocation=true";

    /// <summary>Proves that absent central configuration performs no DNS or policy admission.</summary>
    [Fact]
    public async Task MissingConfigurationNeverInvokesNetworkAuthorizer()
    {
        StubAuthorizer authorizer = new(NetworkEgressResolution.Approved(
            [IPAddress.Parse("203.0.113.10")]));
        await using NetworkBoundServerDbContextFactory factory = new(null, authorizer);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.CreateDbContextAsync());

        Assert.False(factory.IsConfiguredAndValid);
        Assert.Equal("server.database.configuration_invalid", exception.Message);
        Assert.Equal(0, authorizer.CallCount);
    }

    /// <summary>Proves that insecure TLS and diagnostics options are refused before network admission.</summary>
    /// <param name="connectionString">Insecure connection configuration under test.</param>
    [Theory]
    [InlineData("Host=database.example;Database=dbnotifier;SSL Mode=Prefer;Check Certificate Revocation=true")]
    [InlineData("Host=database.example;Database=dbnotifier;SSL Mode=VerifyCA;Check Certificate Revocation=true")]
    [InlineData("Host=database.example;Database=dbnotifier;SSL Mode=VerifyFull;Check Certificate Revocation=false")]
    [InlineData("Host=database.example;Database=dbnotifier;SSL Mode=VerifyFull;Check Certificate Revocation=true;Trust Server Certificate=true")]
    [InlineData("Host=database.example;Database=dbnotifier;SSL Mode=VerifyFull;Check Certificate Revocation=true;Include Error Detail=true")]
    [InlineData("Host=database.example;Database=dbnotifier;SSL Mode=VerifyFull;Check Certificate Revocation=true;Log Parameters=true")]
    [InlineData("Host=database.example,backup.example;Database=dbnotifier;SSL Mode=VerifyFull;Check Certificate Revocation=true")]
    [InlineData("Host=/var/run/postgresql;Database=dbnotifier;SSL Mode=VerifyFull;Check Certificate Revocation=true")]
    [InlineData("Host=database.example;Database=dbnotifier;SSL Mode=VerifyFull;Check Certificate Revocation=true;Root Certificate=root.pem")]
    public void InsecureConfigurationIsRejectedBeforeResolution(string connectionString)
    {
        bool valid = NetworkBoundServerDbContextFactory.TryValidate(connectionString, out _);

        Assert.False(valid);
    }

    /// <summary>Proves that one configured host is authorised once and replaced by an approved IP in Npgsql.</summary>
    [Fact]
    public async Task ValidConfigurationResolvesOnceAndPinsApprovedAddress()
    {
        IPAddress pinnedAddress = IPAddress.Parse("203.0.113.10");
        StubAuthorizer authorizer = new(NetworkEgressResolution.Approved([pinnedAddress]));
        await using NetworkBoundServerDbContextFactory factory = new(ValidConnectionString, authorizer);

        await using ServerDbContext first = await factory.CreateDbContextAsync();
        await using ServerDbContext second = await factory.CreateDbContextAsync();
        NpgsqlConnectionStringBuilder actual = new(first.Database.GetDbConnection().ConnectionString);

        Assert.True(factory.IsConfiguredAndValid);
        Assert.Equal(1, authorizer.CallCount);
        Assert.Equal(NetworkEgressPolicyIds.ServerDatabase, authorizer.LastRequest?.PolicyId);
        Assert.Equal("database.example", authorizer.LastRequest?.Host);
        Assert.Equal(5432, authorizer.LastRequest?.Port);
        Assert.Equal(pinnedAddress.ToString(), actual.Host);
        Assert.Equal(SslMode.VerifyFull, actual.SslMode);
        Assert.True(actual.CheckCertificateRevocation);
        Assert.False(
            actual.TryGetValue("Trust Server Certificate", out object? trustValue) &&
            trustValue is true);
    }

    /// <summary>Proves that a policy refusal is sanitised and cached rather than triggering later DNS rebinding.</summary>
    [Fact]
    public async Task DeniedResolutionFailsClosedAndIsNotRepeated()
    {
        StubAuthorizer authorizer = new(
            NetworkEgressResolution.Denied(NetworkEgressFailureCodes.AddressDenied));
        await using NetworkBoundServerDbContextFactory factory = new(ValidConnectionString, authorizer);

        InvalidOperationException first = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.CreateDbContextAsync());
        InvalidOperationException second = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.CreateDbContextAsync());

        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, first.Message);
        Assert.Equal(first.Message, second.Message);
        Assert.DoesNotContain("database.example", first.Message, StringComparison.Ordinal);
        Assert.Equal(1, authorizer.CallCount);
    }

    /// <summary>Proves that Npgsql authenticates the original host under a no-download offline chain policy.</summary>
    [Fact]
    public void PostgreSqlTlsUsesOriginalTargetAndOfflineRevocation()
    {
        SslClientAuthenticationOptions options = new();

        NetworkBoundServerDbContextFactory.ApplyTlsPolicy(options, "database.example");

        Assert.Equal("database.example", options.TargetHost);
        Assert.Equal(X509RevocationMode.Offline, options.CertificateRevocationCheckMode);
        Assert.NotNull(options.CertificateChainPolicy);
        Assert.True(options.CertificateChainPolicy.DisableCertificateDownloads);
        Assert.Equal(X509RevocationMode.Offline, options.CertificateChainPolicy.RevocationMode);
        Assert.Equal(X509RevocationFlag.EntireChain, options.CertificateChainPolicy.RevocationFlag);
        Assert.Equal(X509VerificationFlags.NoFlag, options.CertificateChainPolicy.VerificationFlags);
        Assert.Equal(X509ChainTrustMode.System, options.CertificateChainPolicy.TrustMode);
        Assert.Empty(options.CertificateChainPolicy.CustomTrustStore);
        Assert.Contains(
            options.CertificateChainPolicy.ApplicationPolicy.Cast<Oid>(),
            usage => usage.Value == "1.3.6.1.5.5.7.3.1");
        Assert.Null(options.RemoteCertificateValidationCallback);
    }

    /// <summary>Proves that production readiness trusts the shared static decision and performs no database I/O.</summary>
    [Fact]
    public async Task ReadinessRejectsSharedInvalidStateWithoutDatabaseCall()
    {
        StubReadinessDatabase database = new();
        ServerReadinessProbe probe = new(
            new ConfigurationBuilder().Build(),
            database,
            new StubConfigurationReadiness(false));

        ServerReadinessResult result = await probe.CheckAsync(CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.Equal("server.readiness.configuration_invalid", result.Code);
        Assert.Equal(0, database.CallCount);
    }

    private sealed class StubAuthorizer(NetworkEgressResolution resolution) : INetworkEgressAuthorizer
    {
        internal int CallCount { get; private set; }

        internal NetworkEgressRequest? LastRequest { get; private set; }

        /// <inheritdoc />
        public ValueTask<NetworkEgressResolution> ResolveAndAuthoriseAsync(
            NetworkEgressRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastRequest = request;
            return ValueTask.FromResult(resolution);
        }
    }

    private sealed record StubConfigurationReadiness(bool IsConfiguredAndValid) :
        IServerDatabaseConfigurationReadiness;

    private sealed class StubReadinessDatabase : IServerReadinessDatabase
    {
        internal int CallCount { get; private set; }

        /// <inheritdoc />
        public ValueTask<ServerReadinessDatabaseResult> CheckAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return ValueTask.FromResult(new ServerReadinessDatabaseResult(true, true));
        }
    }
}
