// Module purpose: Physically validates the bounded R-NET PostgreSQL TLS boundary against a runner-owned loopback fixture.
using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using DBNotifier.Application.Security;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Provider.Abstractions;
using DBNotifier.Providers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Exercises the production central-store and provider TLS contracts against one disposable PostgreSQL 16 cell.
/// Each test remains inert unless the owning runner supplies the exact local-homologation marker and bounded fixture.
/// </summary>
public sealed class RNetPostgreSqlTlsHomologationTests
{
    private const string ActivationVariable = "DBNOTIFIER_RNET_LOCAL_HOMOLOGATION";
    private const string ActivationValue = "local-test";
    private const string PortVariable = "DBNOTIFIER_RNET_POSTGRESQL_PORT";
    private const string HostVariable = "DBNOTIFIER_RNET_POSTGRESQL_HOST";
    private const string SecretFileVariable = "DBNOTIFIER_RNET_POSTGRESQL_SECRET_FILE";
    private const string FixtureRootVariable = "DBNOTIFIER_RNET_POSTGRESQL_FIXTURE_ROOT";
    private const string ExpectedHost = "rnet-postgresql.localhost";
    private const string DatabaseName = "dbn_fixture";
    private const string AdministratorName = "dbn_fixture_admin";
    private const string MonitorName = "dbn_fixture_monitor";

    /// <summary>
    /// Proves that the central factory pins loopback, authenticates the original hostname with VerifyFull and exposes
    /// an SSL-backed PostgreSQL session through <c>pg_stat_ssl</c>.
    /// </summary>
    /// <returns>A task completing after the physical session has been inspected and the pool disposed.</returns>
    [Fact]
    public async Task CentralFactoryUsesVerifyFullAndPgStatSsl()
    {
        if (!TryLoadFixture(out PostgreSqlTlsFixture fixture))
        {
            return;
        }

        RNetFixturePaths paths = RNetFixturePaths.LoadRequired();
        using X509Certificate2 trustedRoot =
            RNetFixturePaths.LoadPublicCertificate(paths.RootCertificate);
        Assert.True(RNetPkiAssertions.IsAbsentFromCurrentUserRoot(trustedRoot));
        RecordingAuthorizer systemTrustAuthorizer = RecordingAuthorizer.Approved(
            NetworkEgressPolicyIds.ServerDatabase,
            fixture.Host,
            fixture.Port);
        await using (NetworkBoundServerDbContextFactory systemTrustFactory = new(
            BuildCentralConnectionString(fixture),
            systemTrustAuthorizer))
        {
            await using ServerDbContext systemTrustContext =
                await systemTrustFactory.CreateDbContextAsync();
            await Assert.ThrowsAsync<NpgsqlException>(
                () => systemTrustContext.Database.OpenConnectionAsync());
        }
        Assert.Equal(1, systemTrustAuthorizer.CallCount);

        using RNetCustomRootTrustPolicyFactory trustPolicyFactory =
            RNetCustomRootTrustPolicyFactory.Load(paths.RootCertificate);
        RecordingAuthorizer authorizer = RecordingAuthorizer.Approved(
            NetworkEgressPolicyIds.ServerDatabase,
            fixture.Host,
            fixture.Port);
        await using NetworkBoundServerDbContextFactory factory = new(
            BuildCentralConnectionString(fixture),
            authorizer,
            trustPolicyFactory.CreateServerAuthentication);
        await using ServerDbContext context = await factory.CreateDbContextAsync();
        await context.Database.OpenConnectionAsync();

        bool ssl = await ExecuteBooleanScalarAsync(
            context.Database.GetDbConnection(),
            "SELECT ssl FROM pg_stat_ssl WHERE pid = pg_backend_pid()");
        long serverVersion = await ExecuteLongScalarAsync(
            context.Database.GetDbConnection(),
            "SELECT current_setting('server_version_num')::bigint");

        Assert.True(ssl);
        Assert.InRange(serverVersion, 160000, 169999);
        Assert.Equal(1, authorizer.CallCount);
        Assert.Equal(NetworkEgressPolicyIds.ServerDatabase, authorizer.LastRequest?.PolicyId);
        Assert.Equal(fixture.Host, authorizer.LastRequest?.Host);
        Assert.Equal(fixture.Port, authorizer.LastRequest?.Port);
    }

    /// <summary>Proves that the provider contract returns Healthy for the valid least-privilege fixture credential.</summary>
    /// <returns>A task completing after one fixed authenticated query over verified TLS.</returns>
    [Fact]
    public async Task ProviderCredentialIsHealthyOverVerifiedTls()
    {
        if (!TryLoadFixture(out PostgreSqlTlsFixture fixture))
        {
            return;
        }

        RNetFixturePaths paths = RNetFixturePaths.LoadRequired();
        PostgreSqlAuthenticatedResult systemTrustResult =
            await ExecuteProviderWithSystemTrustAsync(
                fixture,
                fixture.Host,
                fixture.MonitorPassword);
        PostgreSqlAuthenticatedResult result = await ExecuteProviderAsync(
            fixture,
            fixture.Host,
            fixture.MonitorPassword,
            paths.RootCertificate);

        Assert.Equal(PostgreSqlAuthenticatedState.Unavailable, systemTrustResult.State);
        Assert.Equal(PostgreSqlAuthenticatedState.Healthy, result.State);
        Assert.Null(result.ErrorCode);
    }

    /// <summary>Proves that PostgreSQL credential rejection remains distinct from TLS or reachability failure.</summary>
    /// <returns>A task completing after the deliberately derived non-matching credential is rejected.</returns>
    [Fact]
    public async Task WrongPasswordIsAuthenticationFailed()
    {
        if (!TryLoadFixture(out PostgreSqlTlsFixture fixture))
        {
            return;
        }

        RNetFixturePaths paths = RNetFixturePaths.LoadRequired();
        string nonMatchingPassword = string.Concat(fixture.MonitorPassword, "-mismatch");
        PostgreSqlAuthenticatedResult result = await ExecuteProviderAsync(
            fixture,
            fixture.Host,
            nonMatchingPassword,
            paths.RootCertificate);

        Assert.Equal(PostgreSqlAuthenticatedState.AuthenticationFailed, result.State);
        Assert.Null(result.ErrorCode);
    }

    /// <summary>Proves that IP pinning does not weaken certificate identity verification for the original hostname.</summary>
    /// <returns>A task completing after the wrong hostname is normalised to an unavailable endpoint.</returns>
    [Fact]
    public async Task WrongHostnameIsRefusedByVerifyFull()
    {
        if (!TryLoadFixture(out PostgreSqlTlsFixture fixture))
        {
            return;
        }

        RNetFixturePaths paths = RNetFixturePaths.LoadRequired();
        PostgreSqlAuthenticatedResult result = await ExecuteProviderAsync(
            fixture,
            "wrong-rnet-postgresql.localhost",
            fixture.MonitorPassword,
            paths.RootCertificate);
        await AssertCentralTlsCertificateRefusalAsync(
            fixture,
            "wrong-rnet-postgresql.localhost",
            paths.RootCertificate);

        Assert.Equal(PostgreSqlAuthenticatedState.Unavailable, result.State);
        Assert.Null(result.ErrorCode);
    }

    /// <summary>Proves that a server chain rooted outside the process-local trust fixture fails closed.</summary>
    /// <returns>A task completing after the TLS refusal is normalised without provider-native diagnostics.</returns>
    [Fact]
    public async Task UntrustedCertificateIsRefusedByOfflineTls()
    {
        if (!TryLoadFixture(out PostgreSqlTlsFixture fixture))
        {
            return;
        }

        RNetFixturePaths paths = RNetFixturePaths.LoadRequired();
        await AssertTlsCertificateRefusalAsync(fixture, paths.RootCertificate);
    }

    /// <summary>Proves that Offline revocation fails closed when the trusted issuer has no local CRL material.</summary>
    /// <returns>A task completing after the missing-CRL refusal is normalised.</returns>
    [Fact]
    public async Task MissingCrlCertificateIsRefusedByOfflineTls()
    {
        if (!TryLoadFixture(out PostgreSqlTlsFixture fixture))
        {
            return;
        }

        RNetFixturePaths paths = RNetFixturePaths.LoadRequired();
        await AssertTlsCertificateRefusalAsync(
            fixture,
            paths.MissingCrlRootCertificate);
    }

    /// <summary>
    /// Proves that the Npgsql TLS callback enforces the locally registered CRL even though the connection-string
    /// compatibility flag is disabled before callback construction.
    /// </summary>
    /// <returns>A task completing after the revoked leaf is normalised to an unavailable endpoint.</returns>
    [Fact]
    public async Task RevokedCertificateIsRefusedByOfflineTls()
    {
        if (!TryLoadFixture(out PostgreSqlTlsFixture fixture))
        {
            return;
        }

        RNetFixturePaths paths = RNetFixturePaths.LoadRequired();
        await AssertTlsCertificateRefusalAsync(fixture, paths.RootCertificate);
    }

    /// <summary>
    /// Proves that a policy denial returns InvalidConfiguration before the executor reads the credential a second
    /// time to materialise a password, while sampled server session counts remain zero.
    /// </summary>
    /// <returns>A task completing after the credential-read count and sampled server session counts are checked.</returns>
    [Fact]
    public async Task PolicyDenialAvoidsSecretMaterialisationAndLeavesSampledSessionCountZero()
    {
        if (!TryLoadFixture(out PostgreSqlTlsFixture fixture))
        {
            return;
        }

        RNetFixturePaths paths = RNetFixturePaths.LoadRequired();
        using RNetCustomRootTrustPolicyFactory trustPolicyFactory =
            RNetCustomRootTrustPolicyFactory.Load(paths.RootCertificate);
        await using NetworkBoundServerDbContextFactory factory = new(
            BuildCentralConnectionString(fixture),
            RecordingAuthorizer.Approved(
                NetworkEgressPolicyIds.ServerDatabase,
                fixture.Host,
                fixture.Port),
            trustPolicyFactory.CreateServerAuthentication);
        await using ServerDbContext context = await factory.CreateDbContextAsync();
        await context.Database.OpenConnectionAsync();
        long before = await CountMonitorSessionsAsync(context.Database.GetDbConnection());

        RecordingAuthorizer denied = RecordingAuthorizer.Denied(
            NetworkEgressPolicyIds.ProviderMonitoring,
            fixture.Host,
            fixture.Port,
            NetworkEgressFailureCodes.AddressDenied);
        NpgsqlAuthenticatedExecutor executor = new(denied);
        using CountingProviderCredential credential = new(
            fixture.MonitorUser,
            fixture.MonitorPassword.AsSpan());
        PostgreSqlAuthenticatedResult result = await executor.ExecuteAsync(
            Endpoint(fixture.Host, fixture.Port),
            credential,
            TimeSpan.FromSeconds(8),
            CancellationToken.None);

        long after = await CountMonitorSessionsAsync(context.Database.GetDbConnection());
        Assert.Equal(PostgreSqlAuthenticatedState.InvalidConfiguration, result.State);
        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, result.ErrorCode);
        Assert.Equal(1, denied.CallCount);
        Assert.Equal(1, credential.SecretReadCount);
        Assert.Equal(0, before);
        Assert.Equal(before, after);
    }

    /// <summary>Executes one provider probe through an approved loopback resolution and the original TLS hostname.</summary>
    /// <param name="fixture">Validated runner-owned connection material.</param>
    /// <param name="originalHost">Hostname that TLS must authenticate after IP pinning.</param>
    /// <param name="password">Ephemeral synthetic credential value retained only for this call.</param>
    /// <param name="rootCertificatePath">Exact process-local public trust anchor for this fixture cell.</param>
    /// <returns>The canonical provider result without leaking native failure details.</returns>
    private static async Task<PostgreSqlAuthenticatedResult> ExecuteProviderAsync(
        PostgreSqlTlsFixture fixture,
        string originalHost,
        string password,
        string rootCertificatePath)
    {
        using RNetCustomRootTrustPolicyFactory trustPolicyFactory =
            RNetCustomRootTrustPolicyFactory.Load(rootCertificatePath);
        NpgsqlAuthenticatedExecutor executor = new(
            RecordingAuthorizer.Approved(
                NetworkEgressPolicyIds.ProviderMonitoring,
                originalHost,
                fixture.Port),
            trustPolicyFactory.CreateServerAuthentication);
        using ProviderCredentialLease credential = new(fixture.MonitorUser, password.AsSpan());
        return await executor.ExecuteAsync(
            Endpoint(originalHost, fixture.Port),
            credential,
            TimeSpan.FromSeconds(8),
            CancellationToken.None);
    }

    /// <summary>Executes one provider probe with the unmodified production system-trust constructor.</summary>
    /// <param name="fixture">Validated runner-owned connection material.</param>
    /// <param name="originalHost">Hostname retained for TLS identity.</param>
    /// <param name="password">Ephemeral synthetic credential retained only for this call.</param>
    /// <returns>The canonical provider result.</returns>
    private static async Task<PostgreSqlAuthenticatedResult> ExecuteProviderWithSystemTrustAsync(
        PostgreSqlTlsFixture fixture,
        string originalHost,
        string password)
    {
        NpgsqlAuthenticatedExecutor executor = new(
            RecordingAuthorizer.Approved(
                NetworkEgressPolicyIds.ProviderMonitoring,
                originalHost,
                fixture.Port));
        using ProviderCredentialLease credential = new(fixture.MonitorUser, password.AsSpan());
        return await executor.ExecuteAsync(
            Endpoint(originalHost, fixture.Port),
            credential,
            TimeSpan.FromSeconds(8),
            CancellationToken.None);
    }

    /// <summary>Asserts the stable fail-closed result shared by certificate-chain and revocation failures.</summary>
    /// <param name="fixture">Validated runner-owned connection material for the current negative TLS cell.</param>
    /// <param name="rootCertificatePath">Exact process-local public trust anchor for this fixture cell.</param>
    /// <returns>A task completing after the canonical result is checked.</returns>
    private static async Task AssertTlsCertificateRefusalAsync(
        PostgreSqlTlsFixture fixture,
        string rootCertificatePath)
    {
        PostgreSqlAuthenticatedResult result = await ExecuteProviderAsync(
            fixture,
            fixture.Host,
            fixture.MonitorPassword,
            rootCertificatePath);
        await AssertCentralTlsCertificateRefusalAsync(
            fixture,
            fixture.Host,
            rootCertificatePath);
        Assert.Equal(PostgreSqlAuthenticatedState.Unavailable, result.State);
        Assert.Null(result.ErrorCode);
    }

    /// <summary>Asserts that the central factory also fails closed for one physical TLS identity or chain refusal.</summary>
    /// <param name="fixture">Validated runner-owned connection material for the current PostgreSQL cell.</param>
    /// <param name="originalHost">Hostname that the central TLS path must authenticate.</param>
    /// <param name="rootCertificatePath">Exact process-local public trust anchor for this fixture cell.</param>
    /// <returns>A task completing after Npgsql refuses to open the central connection.</returns>
    private static async Task AssertCentralTlsCertificateRefusalAsync(
        PostgreSqlTlsFixture fixture,
        string originalHost,
        string rootCertificatePath)
    {
        using RNetCustomRootTrustPolicyFactory trustPolicyFactory =
            RNetCustomRootTrustPolicyFactory.Load(rootCertificatePath);
        RecordingAuthorizer authorizer = RecordingAuthorizer.Approved(
            NetworkEgressPolicyIds.ServerDatabase,
            originalHost,
            fixture.Port);
        await using NetworkBoundServerDbContextFactory factory = new(
            BuildCentralConnectionString(fixture, originalHost),
            authorizer,
            trustPolicyFactory.CreateServerAuthentication);
        await using ServerDbContext context = await factory.CreateDbContextAsync();

        await Assert.ThrowsAsync<NpgsqlException>(
            () => context.Database.OpenConnectionAsync());
        Assert.Equal(1, authorizer.CallCount);
    }

    /// <summary>Creates one endpoint that retains the supplied hostname solely for policy selection and TLS identity.</summary>
    /// <param name="host">Original DNS hostname.</param>
    /// <param name="port">Runner-owned loopback port.</param>
    /// <returns>A bounded PostgreSQL endpoint using the production VerifyFull contract.</returns>
    private static PostgreSqlEndpoint Endpoint(string host, int port) =>
        new(host, port, DatabaseName, PostgreSqlEndpoint.DefaultExecutable, null, "verify-full");

    /// <summary>Builds the private central-store connection string only in memory from the synthetic secret file.</summary>
    /// <param name="fixture">Validated runner-owned connection material.</param>
    /// <returns>A connection string accepted by <see cref="NetworkBoundServerDbContextFactory"/>.</returns>
    private static string BuildCentralConnectionString(
        PostgreSqlTlsFixture fixture,
        string? originalHost = null) =>
        new NpgsqlConnectionStringBuilder
        {
            Host = originalHost ?? fixture.Host,
            Port = fixture.Port,
            Database = fixture.Database,
            Username = fixture.AdministratorUser,
            Password = fixture.AdministratorPassword,
            SslMode = SslMode.VerifyFull,
            CheckCertificateRevocation = true,
            Pooling = false,
            Timeout = 8,
            CommandTimeout = 8,
            PersistSecurityInfo = false,
            IncludeErrorDetail = false,
            LogParameters = false,
            ApplicationName = "dbn-rnet-central-fixture",
        }.ConnectionString;

    /// <summary>Reads one boolean scalar from the already opened central fixture session.</summary>
    /// <param name="connection">Open central PostgreSQL connection.</param>
    /// <param name="commandText">Fixed non-secret fixture query.</param>
    /// <returns>The required boolean result.</returns>
    private static async Task<bool> ExecuteBooleanScalarAsync(
        DbConnection connection,
        string commandText)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        object? value = await command.ExecuteScalarAsync();
        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }

    /// <summary>Reads one integer scalar from the already opened central fixture session.</summary>
    /// <param name="connection">Open central PostgreSQL connection.</param>
    /// <param name="commandText">Fixed non-secret fixture query.</param>
    /// <returns>The required 64-bit integer result.</returns>
    private static async Task<long> ExecuteLongScalarAsync(
        DbConnection connection,
        string commandText)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        object? value = await command.ExecuteScalarAsync();
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    /// <summary>Counts sessions owned by the exact synthetic monitoring role without retaining query text from clients.</summary>
    /// <param name="connection">Open administrator connection used only for fixture inspection.</param>
    /// <returns>The current monitoring-session count.</returns>
    private static async Task<long> CountMonitorSessionsAsync(DbConnection connection)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT count(*) FROM pg_stat_activity WHERE usename = 'dbn_fixture_monitor'";
        object? value = await command.ExecuteScalarAsync();
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    /// <summary>Loads and validates the exact marker, paths, host, port and closed synthetic-secret document.</summary>
    /// <param name="fixture">Validated fixture when the test runner activated the physical lab.</param>
    /// <returns><see langword="false"/> only when the physical lab was deliberately not activated.</returns>
    private static bool TryLoadFixture(out PostgreSqlTlsFixture fixture)
    {
        fixture = default!;
        if (!string.Equals(
                Environment.GetEnvironmentVariable(ActivationVariable),
                ActivationValue,
                StringComparison.Ordinal))
        {
            return false;
        }

        string fixtureRoot = Path.GetFullPath(
            Environment.GetEnvironmentVariable(FixtureRootVariable) ??
            throw new InvalidDataException("rnet.postgresql_fixture_root_missing"));
        string secretFile = Path.GetFullPath(
            Environment.GetEnvironmentVariable(SecretFileVariable) ??
            throw new InvalidDataException("rnet.postgresql_secret_file_missing"));
        DirectoryInfo root = new(fixtureRoot);
        if (!root.Exists ||
            !root.Name.StartsWith("DBNotifier-R-Net-", StringComparison.Ordinal) ||
            !Path.IsPathFullyQualified(fixtureRoot) ||
            !Path.IsPathFullyQualified(secretFile) ||
            !string.Equals(
                Path.GetDirectoryName(secretFile),
                fixtureRoot,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                Path.GetFileName(secretFile),
                "postgres-secrets.json",
                StringComparison.Ordinal))
        {
            throw new InvalidDataException("rnet.postgresql_fixture_path_invalid");
        }

        FileInfo secret = new(secretFile);
        if (!secret.Exists || secret.Length is < 32 or > 4096)
        {
            throw new InvalidDataException("rnet.postgresql_secret_file_invalid");
        }

        string host = Environment.GetEnvironmentVariable(HostVariable) ??
            throw new InvalidDataException("rnet.postgresql_host_missing");
        if (!string.Equals(host, ExpectedHost, StringComparison.Ordinal))
        {
            throw new InvalidDataException("rnet.postgresql_host_invalid");
        }

        if (!int.TryParse(
                Environment.GetEnvironmentVariable(PortVariable),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int port) ||
            port is < 1 or > 65535)
        {
            throw new InvalidDataException("rnet.postgresql_port_invalid");
        }

        using FileStream stream = new(
            secretFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.SequentialScan);
        using JsonDocument document = JsonDocument.Parse(
            stream,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 3,
            });
        JsonElement rootElement = document.RootElement;
        string[] expectedProperties =
        [
            "administratorPassword",
            "administratorUser",
            "database",
            "monitorPassword",
            "monitorUser",
        ];
        string[] actualProperties = rootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (rootElement.ValueKind != JsonValueKind.Object ||
            !actualProperties.SequenceEqual(expectedProperties, StringComparer.Ordinal))
        {
            throw new InvalidDataException("rnet.postgresql_secret_schema_invalid");
        }

        string database = RequiredBoundedString(rootElement, "database");
        string administratorUser = RequiredBoundedString(rootElement, "administratorUser");
        string administratorPassword = RequiredBoundedString(rootElement, "administratorPassword");
        string monitorUser = RequiredBoundedString(rootElement, "monitorUser");
        string monitorPassword = RequiredBoundedString(rootElement, "monitorPassword");
        if (!string.Equals(database, DatabaseName, StringComparison.Ordinal) ||
            !string.Equals(administratorUser, AdministratorName, StringComparison.Ordinal) ||
            !string.Equals(monitorUser, MonitorName, StringComparison.Ordinal) ||
            string.Equals(administratorPassword, monitorPassword, StringComparison.Ordinal))
        {
            throw new InvalidDataException("rnet.postgresql_secret_identity_invalid");
        }

        fixture = new(
            fixtureRoot,
            host,
            port,
            database,
            administratorUser,
            administratorPassword,
            monitorUser,
            monitorPassword);
        return true;
    }

    /// <summary>Reads one required bounded string from the closed synthetic-secret schema.</summary>
    /// <param name="root">Parsed secret document object.</param>
    /// <param name="propertyName">Expected exact property name.</param>
    /// <returns>A non-empty string no longer than 256 characters.</returns>
    private static string RequiredBoundedString(JsonElement root, string propertyName)
    {
        JsonElement value = root.GetProperty(propertyName);
        string? text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        if (string.IsNullOrWhiteSpace(text) || text.Length > 256 || text.Any(char.IsControl))
        {
            throw new InvalidDataException("rnet.postgresql_secret_value_invalid");
        }

        return text;
    }

    /// <summary>Returns one closed approved or denied resolution while recording the exact production policy request.</summary>
    private sealed class RecordingAuthorizer : INetworkEgressAuthorizer
    {
        private readonly string expectedPolicy;
        private readonly string expectedHost;
        private readonly int expectedPort;
        private readonly NetworkEgressResolution resolution;

        /// <summary>Initialises a strict authoriser for one exact production boundary request.</summary>
        /// <param name="expectedPolicy">Expected stable policy identifier.</param>
        /// <param name="expectedHost">Expected original hostname.</param>
        /// <param name="expectedPort">Expected runner-owned port.</param>
        /// <param name="resolution">Preselected immutable resolution.</param>
        private RecordingAuthorizer(
            string expectedPolicy,
            string expectedHost,
            int expectedPort,
            NetworkEgressResolution resolution)
        {
            this.expectedPolicy = expectedPolicy;
            this.expectedHost = expectedHost;
            this.expectedPort = expectedPort;
            this.resolution = resolution;
        }

        /// <summary>Gets the number of exact policy calls observed.</summary>
        internal int CallCount { get; private set; }

        /// <summary>Gets the last request after exact policy, host and port validation.</summary>
        internal NetworkEgressRequest? LastRequest { get; private set; }

        /// <summary>Creates one authoriser that pins only IPv4 loopback.</summary>
        /// <param name="policy">Expected production policy identifier.</param>
        /// <param name="host">Expected original hostname.</param>
        /// <param name="port">Expected runner-owned port.</param>
        /// <returns>A strict approved authoriser.</returns>
        internal static RecordingAuthorizer Approved(string policy, string host, int port) =>
            new(policy, host, port, NetworkEgressResolution.Approved([IPAddress.Loopback]));

        /// <summary>Creates one authoriser that returns a stable sanitised denial without a socket.</summary>
        /// <param name="policy">Expected production policy identifier.</param>
        /// <param name="host">Expected original hostname.</param>
        /// <param name="port">Expected runner-owned port.</param>
        /// <param name="failureCode">Stable network refusal code.</param>
        /// <returns>A strict denied authoriser.</returns>
        internal static RecordingAuthorizer Denied(
            string policy,
            string host,
            int port,
            string failureCode) =>
            new(policy, host, port, NetworkEgressResolution.Denied(failureCode));

        /// <inheritdoc />
        public ValueTask<NetworkEgressResolution> ResolveAndAuthoriseAsync(
            NetworkEgressRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(request.PolicyId, expectedPolicy, StringComparison.Ordinal) ||
                !string.Equals(request.Host, expectedHost, StringComparison.Ordinal) ||
                request.Port != expectedPort)
            {
                throw new InvalidOperationException("rnet.postgresql_policy_request_mismatch");
            }

            CallCount++;
            LastRequest = request;
            return ValueTask.FromResult(resolution);
        }
    }

    /// <summary>
    /// Owns one synthetic credential while recording every secret-memory request so the denial boundary can prove
    /// that execution stops after the executor's initial non-empty validation.
    /// </summary>
    private sealed class CountingProviderCredential : IProviderCredential
    {
        private char[]? secret;
        private int secretReadCount;

        /// <summary>Initialises a disposable copy of one runner-owned synthetic credential.</summary>
        /// <param name="userName">Synthetic monitoring role.</param>
        /// <param name="secret">Synthetic credential copied into clearable memory.</param>
        internal CountingProviderCredential(string? userName, ReadOnlySpan<char> secret)
        {
            UserName = userName;
            this.secret = secret.ToArray();
        }

        /// <inheritdoc />
        public string? UserName { get; }

        /// <inheritdoc />
        public DateTimeOffset? ExpiresAt => null;

        /// <inheritdoc />
        public ReadOnlyMemory<char> Secret
        {
            get
            {
                char[] value = secret ??
                    throw new ObjectDisposedException(nameof(CountingProviderCredential));
                Interlocked.Increment(ref secretReadCount);
                return value;
            }
        }

        /// <summary>Gets the exact number of times the executor requested the credential memory.</summary>
        internal int SecretReadCount => Volatile.Read(ref secretReadCount);

        /// <summary>Clears and releases the owned synthetic credential exactly once.</summary>
        public void Dispose()
        {
            char[]? value = Interlocked.Exchange(ref secret, null);
            if (value is not null)
            {
                CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(value.AsSpan()));
            }
        }
    }

    /// <summary>
    /// Retains only runner-owned synthetic values required by one test process; the owning runner deletes the file
    /// and all certificate material immediately after the campaign.
    /// </summary>
    /// <param name="Root">Validated process-scoped temporary root.</param>
    /// <param name="Host">Exact TLS hostname.</param>
    /// <param name="Port">Loopback-only published PostgreSQL port.</param>
    /// <param name="Database">Synthetic fixture database.</param>
    /// <param name="AdministratorUser">Synthetic fixture administrator role.</param>
    /// <param name="AdministratorPassword">Synthetic fixture administrator credential.</param>
    /// <param name="MonitorUser">Synthetic least-privilege monitoring role.</param>
    /// <param name="MonitorPassword">Synthetic monitoring credential.</param>
    private sealed record PostgreSqlTlsFixture(
        string Root,
        string Host,
        int Port,
        string Database,
        string AdministratorUser,
        string AdministratorPassword,
        string MonitorUser,
        string MonitorPassword);
}
