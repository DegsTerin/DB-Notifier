// Module purpose: Verifies fail-closed Agent certificate identity binding against isolated central persistence.
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.UnitTests;

/// <summary>
/// Exercises public-key binding, stored validity and the explicit legacy backfill boundary without a real PKI.
/// </summary>
public sealed class AgentCertificateIdentityValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Proves that only exact current certificate evidence or the explicit null-digest legacy row resolves an Agent.
    /// </summary>
    /// <param name="scenario">Stored certificate evidence variant.</param>
    /// <param name="expectedToResolve">Whether the active Agent identity must resolve.</param>
    /// <returns>A task that completes after the isolated persistence assertion.</returns>
    [Theory]
    [InlineData(CertificateEvidenceScenario.ExactDigest, true)]
    [InlineData(CertificateEvidenceScenario.DifferentDigest, false)]
    [InlineData(CertificateEvidenceScenario.MalformedDigest, false)]
    [InlineData(CertificateEvidenceScenario.FutureValidity, false)]
    [InlineData(CertificateEvidenceScenario.ExpiredValidity, false)]
    [InlineData(CertificateEvidenceScenario.LegacyNullDigest, true)]
    public async Task StoredCertificateEvidenceFailsClosed(
        CertificateEvidenceScenario scenario,
        bool expectedToResolve)
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ServerDbContext> options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseSqlite(connection)
            .Options;
        using X509Certificate2 certificate = CreateClientCertificate();
        Guid agentId = Guid.NewGuid();
        await SeedIdentityAsync(options, agentId, certificate, scenario);
        AgentCertificateIdentityValidator validator = new(
            new ServerContextFactory(options),
            new FixedTimeProvider(Now));

        Guid? resolved = await validator.ValidateAsync(certificate, CancellationToken.None);

        if (expectedToResolve)
        {
            Assert.Equal(agentId, resolved);
        }
        else
        {
            Assert.Null(resolved);
        }
    }

    /// <summary>Creates one ephemeral P-256 client certificate for exact public-key comparisons.</summary>
    /// <returns>An exportable certificate whose private key remains process-local.</returns>
    private static X509Certificate2 CreateClientCertificate()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest request = new("CN=DB-Notifier Identity Validator Fixture", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        OidCollection usages = new()
        {
            new Oid("1.3.6.1.5.5.7.3.2"),
        };
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(usages, false));
        using X509Certificate2 ephemeral = request.CreateSelfSigned(Now.AddMinutes(-5), Now.AddHours(1));
        return X509CertificateLoader.LoadPkcs12(
            ephemeral.Export(X509ContentType.Pkcs12),
            null,
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
    }

    /// <summary>Seeds one active Agent and the exact certificate evidence variant under test.</summary>
    /// <param name="options">Isolated SQLite context options.</param>
    /// <param name="agentId">Agent identity shared by both rows.</param>
    /// <param name="certificate">Presented certificate whose thumbprint is persisted.</param>
    /// <param name="scenario">Evidence variant controlling digest and stored validity.</param>
    /// <returns>A task that completes after the rows are committed.</returns>
    private static async Task SeedIdentityAsync(
        DbContextOptions<ServerDbContext> options,
        Guid agentId,
        X509Certificate2 certificate,
        CertificateEvidenceScenario scenario)
    {
        string exactDigest = ComputePublicKeyDigest(certificate);
        string? storedDigest = scenario switch
        {
            CertificateEvidenceScenario.DifferentDigest =>
                Convert.ToHexString(SHA256.HashData("different-public-key"u8)),
            CertificateEvidenceScenario.MalformedDigest => "NOT-HEX",
            CertificateEvidenceScenario.LegacyNullDigest => null,
            _ => exactDigest,
        };
        DateTimeOffset notBefore = scenario == CertificateEvidenceScenario.FutureValidity
            ? Now.AddTicks(1)
            : Now.AddMinutes(-5);
        DateTimeOffset notAfter = scenario == CertificateEvidenceScenario.ExpiredValidity
            ? Now
            : Now.AddHours(1);

        await using ServerDbContext context = new(options);
        await context.Database.EnsureCreatedAsync();
        context.Agents.Add(new RegisteredAgentRow
        {
            AgentId = agentId,
            InstallationId = $"identity-validator-{agentId:N}",
            DisplayName = "Identity validator fixture",
            Environment = "test",
            Platform = "windows-x64",
            AgentVersion = "1.0.0-test",
            CertificateThumbprint = certificate.Thumbprint,
            State = "Active",
            EnrolledAt = Now.AddMinutes(-5),
            ConcurrencyToken = Guid.NewGuid(),
        });
        context.AgentCertificates.Add(new AgentCertificateRow
        {
            AgentCertificateId = Guid.NewGuid(),
            AgentId = agentId,
            Thumbprint = certificate.Thumbprint,
            PublicKeySha256 = storedDigest,
            CertificateSigningRequestSha256 = null,
            State = "Active",
            IssuedAt = Now.AddMinutes(-5),
            NotBefore = notBefore,
            NotAfter = notAfter,
            ConcurrencyToken = Guid.NewGuid(),
        });
        await context.SaveChangesAsync();
    }

    /// <summary>Computes the canonical uppercase digest of the subject public-key information.</summary>
    /// <param name="certificate">Certificate exposing the P-256 public key.</param>
    /// <returns>Uppercase SHA-256 hexadecimal text.</returns>
    private static string ComputePublicKeyDigest(X509Certificate2 certificate)
    {
        using ECDsa publicKey = certificate.GetECDsaPublicKey() ??
            throw new InvalidOperationException("The synthetic certificate did not expose an ECDSA public key.");
        return Convert.ToHexString(SHA256.HashData(publicKey.ExportSubjectPublicKeyInfo()));
    }

    /// <summary>Identifies the bounded stored certificate evidence variants used by the theory.</summary>
    public enum CertificateEvidenceScenario
    {
        /// <summary>The stored digest and validity exactly admit the certificate.</summary>
        ExactDigest,

        /// <summary>A well-formed digest identifies different public-key evidence.</summary>
        DifferentDigest,

        /// <summary>The stored digest is not valid hexadecimal text.</summary>
        MalformedDigest,

        /// <summary>The stored validity interval begins after the trusted server time.</summary>
        FutureValidity,

        /// <summary>The stored validity interval ends at the trusted server time.</summary>
        ExpiredValidity,

        /// <summary>A null digest represents the explicit legacy backfill compatibility row.</summary>
        LegacyNullDigest,
    }

    /// <summary>Creates fresh Server contexts over the isolated connection owned by the test.</summary>
    /// <param name="options">Context options sharing the open in-memory SQLite connection.</param>
    private sealed class ServerContextFactory(DbContextOptions<ServerDbContext> options)
        : IDbContextFactory<ServerDbContext>
    {
        /// <inheritdoc />
        public ServerDbContext CreateDbContext() => new(options);
    }

    /// <summary>Returns one deterministic trusted server instant.</summary>
    /// <param name="now">UTC instant returned to the validator.</param>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }
}
