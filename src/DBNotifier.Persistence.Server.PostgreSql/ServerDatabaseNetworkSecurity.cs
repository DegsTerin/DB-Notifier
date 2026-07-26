// Module purpose: Creates central PostgreSQL contexts only from one policy-authorised, IP-pinned TLS data source.
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Provider.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DBNotifier.Persistence.Server.PostgreSql;

/// <summary>
/// Exposes only the static central-database configuration decision needed before a readiness probe may perform I/O.
/// </summary>
public interface IServerDatabaseConfigurationReadiness
{
    /// <summary>
    /// Gets whether a complete, bounded and TLS-verifying central-database configuration was supplied.
    /// </summary>
    bool IsConfiguredAndValid { get; }
}

/// <summary>
/// Creates <see cref="ServerDbContext"/> instances over one lazily authorised and immutable Npgsql data source.
/// DNS is resolved exactly once, all returned addresses must pass the Server database egress policy, and Npgsql
/// connects to one approved IP while TLS continues to authenticate the original configured host.
/// </summary>
public sealed class NetworkBoundServerDbContextFactory :
    IDbContextFactory<ServerDbContext>,
    IServerDatabaseConfigurationReadiness,
    IAsyncDisposable
{
    private const int MaximumConnectionStringLength = 4096;
    private const string ConfigurationFailureCode = "server.database.configuration_invalid";
    private readonly INetworkEgressAuthorizer authorizer;
    private readonly ValidatedServerDatabaseConfiguration? configuration;
    private readonly Lazy<Task<NpgsqlDataSource>> dataSource;
    private readonly Func<X509ChainPolicy> serverChainPolicyFactory;
    private int disposed;

    /// <summary>Initialises a central context factory without resolving DNS or opening a connection.</summary>
    /// <param name="connectionString">Potential central PostgreSQL connection string from the host configuration.</param>
    /// <param name="authorizer">Immutable network-egress authority used once before data-source construction.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="authorizer"/> is null.</exception>
    public NetworkBoundServerDbContextFactory(
        string? connectionString,
        INetworkEgressAuthorizer authorizer)
        : this(
            connectionString,
            authorizer,
            OfflineCertificateChainPolicy.CreateServerAuthentication)
    {
    }

    /// <summary>Initialises a central context factory with an isolated certificate-policy seam for local tests.</summary>
    /// <param name="connectionString">Potential central PostgreSQL connection string from the host configuration.</param>
    /// <param name="authorizer">Immutable network-egress authority used once before data-source construction.</param>
    /// <param name="serverChainPolicyFactory">Creates one fresh server-certificate policy per TLS connection.</param>
    internal NetworkBoundServerDbContextFactory(
        string? connectionString,
        INetworkEgressAuthorizer authorizer,
        Func<X509ChainPolicy> serverChainPolicyFactory)
    {
        ArgumentNullException.ThrowIfNull(authorizer);
        ArgumentNullException.ThrowIfNull(serverChainPolicyFactory);
        this.authorizer = authorizer;
        this.serverChainPolicyFactory = serverChainPolicyFactory;
        configuration = TryValidate(connectionString, out ValidatedServerDatabaseConfiguration? validated)
            ? validated
            : null;
        dataSource = new Lazy<Task<NpgsqlDataSource>>(
            InitialiseDataSourceAsync,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public bool IsConfiguredAndValid => configuration is not null;

    /// <inheritdoc />
    public ServerDbContext CreateDbContext() =>
        CreateDbContextAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<ServerDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default)
    {
        NpgsqlDataSource source = await dataSource.Value
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        DbContextOptionsBuilder<ServerDbContext> options = new();
        options.UseNpgsql(source);
        return new ServerDbContext(options.Options);
    }

    /// <summary>Disposes the shared Npgsql pools if initialisation was attempted successfully.</summary>
    /// <returns>A task that completes after any constructed data source has been disposed.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0 || !dataSource.IsValueCreated)
        {
            return;
        }

        try
        {
            NpgsqlDataSource source = await dataSource.Value.ConfigureAwait(false);
            await source.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // A failed, sanitised initialisation owns no usable pool that still requires disposal.
        }
    }

    /// <summary>
    /// Applies the original configured host and the shared offline chain policy to one Npgsql TLS handshake.
    /// </summary>
    /// <param name="options">Fresh Npgsql TLS options for one connection.</param>
    /// <param name="originalHost">Original DNS name or IP literal whose certificate identity must be proved.</param>
    internal static void ApplyTlsPolicy(
        SslClientAuthenticationOptions options,
        string originalHost) =>
        ApplyTlsPolicy(
            options,
            originalHost,
            OfflineCertificateChainPolicy.CreateServerAuthentication);

    /// <summary>Applies the original host and one independently created certificate-chain policy.</summary>
    /// <param name="options">Fresh Npgsql TLS options for one connection.</param>
    /// <param name="originalHost">Original DNS name or IP literal whose certificate identity must be proved.</param>
    /// <param name="serverChainPolicyFactory">Creates one fresh policy for this physical connection.</param>
    private static void ApplyTlsPolicy(
        SslClientAuthenticationOptions options,
        string originalHost,
        Func<X509ChainPolicy> serverChainPolicyFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalHost);
        ArgumentNullException.ThrowIfNull(serverChainPolicyFactory);
        options.TargetHost = originalHost;
        options.CertificateRevocationCheckMode = X509RevocationMode.Offline;
        options.CertificateChainPolicy = serverChainPolicyFactory() ??
            throw new InvalidOperationException("The TLS policy factory returned no policy.");
    }

    /// <summary>Validates static connection settings without resolving, connecting, logging or returning secrets.</summary>
    /// <param name="connectionString">Untrusted central persistence configuration.</param>
    /// <param name="configuration">Validated private configuration when successful.</param>
    /// <returns><see langword="true"/> only for one bounded PostgreSQL host with mandatory TLS controls.</returns>
    internal static bool TryValidate(
        string? connectionString,
        out ValidatedServerDatabaseConfiguration? configuration)
    {
        configuration = null;
        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString.Length > MaximumConnectionStringLength)
        {
            return false;
        }

        try
        {
            NpgsqlConnectionStringBuilder builder = new()
            {
                ConnectionString = connectionString,
            };
            string host = builder.Host ?? string.Empty;
            if (!IsSingleTcpHost(host) ||
                string.IsNullOrWhiteSpace(builder.Database) ||
                builder.Port is < 1 or > 65535 ||
                builder.SslMode != SslMode.VerifyFull ||
                !builder.CheckCertificateRevocation ||
                HasEnabledSetting(builder, "Trust Server Certificate") ||
                builder.PersistSecurityInfo ||
                builder.IncludeErrorDetail ||
                builder.LogParameters ||
                !string.IsNullOrWhiteSpace(builder.RootCertificate))
            {
                return false;
            }

            configuration = new ValidatedServerDatabaseConfiguration(
                builder.ConnectionString,
                host,
                builder.Port);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Resolves once, pins one approved address and constructs the immutable shared Npgsql data source.</summary>
    /// <returns>The policy-bound data source without opening a database connection.</returns>
    /// <exception cref="InvalidOperationException">Thrown with a stable code when configuration or admission fails.</exception>
    private async Task<NpgsqlDataSource> InitialiseDataSourceAsync()
    {
        if (configuration is null)
        {
            throw new InvalidOperationException(ConfigurationFailureCode);
        }

        NetworkEgressResolution resolution = await authorizer
            .ResolveAndAuthoriseAsync(
                new NetworkEgressRequest(
                    NetworkEgressPolicyIds.ServerDatabase,
                    configuration.OriginalHost,
                    configuration.Port),
                CancellationToken.None)
            .ConfigureAwait(false);
        if (!resolution.IsApproved)
        {
            throw new InvalidOperationException(
                resolution.FailureCode ?? NetworkEgressFailureCodes.AddressDenied);
        }

        IPAddress pinnedAddress = resolution.Addresses[0];
        NpgsqlConnectionStringBuilder pinned = new()
        {
            ConnectionString = configuration.ConnectionString,
            Host = pinnedAddress.ToString(),
            SslMode = SslMode.VerifyFull,
            CheckCertificateRevocation = true,
            PersistSecurityInfo = false,
            IncludeErrorDetail = false,
            LogParameters = false,
        };
        NpgsqlDataSourceBuilder builder = new(pinned.ConnectionString);
        builder.UseSslClientAuthenticationOptionsCallback(
            options => ApplyTlsPolicy(
                options,
                configuration.OriginalHost,
                serverChainPolicyFactory));
        return builder.Build();
    }

    /// <summary>Checks one bounded DNS name or unscoped IP literal and excludes multi-host and socket paths.</summary>
    /// <param name="host">Parsed Npgsql host.</param>
    /// <returns><see langword="true"/> only for one TCP host accepted by the network policy boundary.</returns>
    private static bool IsSingleTcpHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host) ||
            host.Length > 253 ||
            host != host.Trim() ||
            host.Contains(',') ||
            host.Contains('/') ||
            host.Contains('\\') ||
            host.EndsWith('.') ||
            host.Any(char.IsControl))
        {
            return false;
        }

        if (IPAddress.TryParse(host, out IPAddress? literal))
        {
            return literal.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ||
                (literal.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
                    literal.ScopeId == 0);
        }

        try
        {
            string asciiHost = new IdnMapping().GetAscii(host);
            return string.Equals(asciiHost, host, StringComparison.OrdinalIgnoreCase) &&
                Uri.CheckHostName(host) == UriHostNameType.Dns;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Reads an obsolete compatibility keyword through the neutral builder dictionary.</summary>
    /// <param name="builder">Parsed Npgsql settings.</param>
    /// <param name="keyword">Exact compatibility keyword.</param>
    /// <returns><see langword="true"/> only when the explicitly supplied value enables the unsafe setting.</returns>
    private static bool HasEnabledSetting(
        NpgsqlConnectionStringBuilder builder,
        string keyword) =>
        builder.TryGetValue(keyword, out object? value) &&
        value is bool enabled &&
        enabled;

    /// <summary>Retains the private values required to build one data source after network authorisation.</summary>
    /// <param name="ConnectionString">Canonical private Npgsql connection string; never exposed diagnostically.</param>
    /// <param name="OriginalHost">Single configured host used for policy selection and TLS identity.</param>
    /// <param name="Port">Configured PostgreSQL TCP port.</param>
    internal sealed record ValidatedServerDatabaseConfiguration(
        string ConnectionString,
        string OriginalHost,
        int Port);
}
