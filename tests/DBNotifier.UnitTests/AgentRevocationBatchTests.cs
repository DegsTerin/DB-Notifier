// Module purpose: Verifies monotonic Agent revocation and restart-safe certificate reconciliation with ephemeral SQLite state.
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Application.Access;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Persistence.Server.PostgreSql;
using DBNotifier.Server.Api.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DBNotifier.UnitTests;

/// <summary>Exercises certificate cardinality and interruption boundaries without a real PKI or central database.</summary>
public sealed class AgentRevocationBatchTests
{
    private const string SubjectId = "oidc:r3-revocation-fixture";
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 19, 0, 0, TimeSpan.Zero);

    /// <summary>Proves all required cardinalities complete without coupling principal revocation to one certificate batch.</summary>
    /// <param name="certificateCount">Number of active synthetic certificate metadata rows.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(128)]
    [InlineData(129)]
    [InlineData(513)]
    public async Task RevocationCompletesAcrossRequiredCertificateCardinalities(int certificateCount)
    {
        await using RevocationFixture fixture = await RevocationFixture.CreateAsync(certificateCount);
        AgentRevocationOutcome first = await fixture.Store.RevokeAgentAsync(
            SubjectId,
            fixture.AgentId,
            PlatformPermissions.AgentsRevoke,
            "security-review",
            Now,
            CancellationToken.None);

        Assert.Equal(AgentRevocationDisposition.Revoked, first.Disposition);
        await fixture.AssertRevokedAsync(certificateCount, first.RevokedAt);

        AgentRevocationOutcome repeated = await fixture.Store.RevokeAgentAsync(
            SubjectId,
            fixture.AgentId,
            PlatformPermissions.AgentsRevoke,
            "security-review",
            Now.AddMinutes(1),
            CancellationToken.None);
        Assert.Equal(AgentRevocationDisposition.AlreadyRevoked, repeated.Disposition);
        Assert.Equal(first.RevokedAt, repeated.RevokedAt);
        await fixture.AssertRevokedAsync(certificateCount, first.RevokedAt);
    }

    /// <summary>Proves a failed certificate batch leaves authentication denied and a later call resumes safely.</summary>
    [Fact]
    public async Task InterruptedCertificateReconciliationNeverReactivatesTheAgent()
    {
        FailFirstCertificateSaveInterceptor interceptor = new();
        await using RevocationFixture fixture = await RevocationFixture.CreateAsync(129, interceptor, createUsableCertificate: true);
        Assert.NotNull(await fixture.IdentityValidator.ValidateAsync(fixture.PresentedCertificate!, CancellationToken.None));
        interceptor.Arm();

        AgentRevocationOutcome interrupted = await fixture.Store.RevokeAgentAsync(
            SubjectId,
            fixture.AgentId,
            PlatformPermissions.AgentsRevoke,
            "security-review",
            Now,
            CancellationToken.None);

        Assert.Equal(AgentRevocationDisposition.CertificatesReconciling, interrupted.Disposition);
        Assert.Equal("agent.certificate_reconciliation_pending", interrupted.ErrorCode);
        Assert.Null(await fixture.IdentityValidator.ValidateAsync(fixture.PresentedCertificate!, CancellationToken.None));
        Assert.Equal("Revoked", await fixture.ReadAgentStateAsync());

        AgentRevocationOutcome resumed = await fixture.Store.RevokeAgentAsync(
            SubjectId,
            fixture.AgentId,
            PlatformPermissions.AgentsRevoke,
            "security-review",
            Now.AddSeconds(1),
            CancellationToken.None);
        Assert.Equal(AgentRevocationDisposition.AlreadyRevoked, resumed.Disposition);
        Assert.Equal(interrupted.RevokedAt, resumed.RevokedAt);
        await fixture.AssertRevokedAsync(129, interrupted.RevokedAt);
    }

    /// <summary>Owns one isolated Server schema, store and optional in-memory certificate.</summary>
    private sealed class RevocationFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly DbContextOptions<ServerDbContext> options;

        private RevocationFixture(
            SqliteConnection connection,
            DbContextOptions<ServerDbContext> options,
            Guid agentId,
            X509Certificate2? presentedCertificate)
        {
            this.connection = connection;
            this.options = options;
            AgentId = agentId;
            PresentedCertificate = presentedCertificate;
            ServerContextFactory factory = new(options);
            Store = new AgentFleetStore(factory, AgentAssignmentValidationFixture.Create());
            IdentityValidator = new AgentCertificateIdentityValidator(factory, new FixedTimeProvider(Now));
        }

        internal Guid AgentId { get; }

        internal AgentFleetStore Store { get; }

        internal AgentCertificateIdentityValidator IdentityValidator { get; }

        internal X509Certificate2? PresentedCertificate { get; }

        internal static async Task<RevocationFixture> CreateAsync(
            int certificateCount,
            IInterceptor? interceptor = null,
            bool createUsableCertificate = false)
        {
            SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            DbContextOptionsBuilder<ServerDbContext> builder = new DbContextOptionsBuilder<ServerDbContext>()
                .UseSqlite(connection);
            if (interceptor is not null)
            {
                builder.AddInterceptors(interceptor);
            }
            DbContextOptions<ServerDbContext> options = builder.Options;
            X509Certificate2? certificate = createUsableCertificate ? CreateClientCertificate() : null;
            Guid agentId = Guid.NewGuid();
            try
            {
                await using ServerDbContext context = new(options);
                await context.Database.EnsureCreatedAsync();
                SeedAccess(context, agentId);
                for (int index = 0; index < certificateCount; index++)
                {
                    string thumbprint = index == 0 && certificate is not null
                        ? NormaliseThumbprint(certificate.Thumbprint)
                        : Convert.ToHexString(SHA256.HashData(BitConverter.GetBytes(index)));
                    string? publicKeyDigest = index == 0 && certificate is not null
                        ? ComputePublicKeyDigest(certificate)
                        : null;
                    context.AgentCertificates.Add(new AgentCertificateRow
                    {
                        AgentCertificateId = Guid.NewGuid(),
                        AgentId = agentId,
                        Thumbprint = thumbprint,
                        PublicKeySha256 = publicKeyDigest,
                        CertificateSigningRequestSha256 = null,
                        State = "Active",
                        IssuedAt = Now.AddMinutes(-5),
                        NotBefore = Now.AddMinutes(-5),
                        NotAfter = Now.AddHours(1),
                        ConcurrencyToken = Guid.NewGuid(),
                    });
                }
                await context.SaveChangesAsync();
                return new RevocationFixture(connection, options, agentId, certificate);
            }
            catch
            {
                certificate?.Dispose();
                await connection.DisposeAsync();
                throw;
            }
        }

        internal async Task AssertRevokedAsync(int certificateCount, DateTimeOffset? revokedAt)
        {
            await using ServerDbContext context = new(options);
            RegisteredAgentRow agent = await context.Agents.AsNoTracking().SingleAsync(row => row.AgentId == AgentId);
            Assert.Equal("Revoked", agent.State);
            Assert.Equal(revokedAt, agent.RevokedAt);
            Assert.Equal(certificateCount, await context.AgentCertificates.CountAsync(row =>
                row.AgentId == AgentId && row.State == "Revoked" && row.RevokedAt == revokedAt));
        }

        internal async Task<string> ReadAgentStateAsync()
        {
            await using ServerDbContext context = new(options);
            return await context.Agents.Where(row => row.AgentId == AgentId).Select(row => row.State).SingleAsync();
        }

        public async ValueTask DisposeAsync()
        {
            PresentedCertificate?.Dispose();
            await connection.DisposeAsync();
        }

        private static void SeedAccess(ServerDbContext context, Guid agentId)
        {
            Guid userId = Guid.NewGuid();
            Guid roleId = Guid.NewGuid();
            Guid permissionId = Guid.NewGuid();
            context.Agents.Add(new RegisteredAgentRow
            {
                AgentId = agentId,
                InstallationId = "installation:r3-revocation",
                DisplayName = "R3 revocation fixture",
                Environment = "test",
                Platform = "windows-x64",
                AgentVersion = "1.0.0-test",
                CertificateThumbprint = new string('A', 64),
                State = "Active",
                EnrolledAt = Now.AddMinutes(-5),
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Users.Add(new PlatformUserRow
            {
                UserId = userId,
                SubjectId = SubjectId,
                DisplayName = "R3 fixture operator",
                State = "Active",
                CreatedAt = Now.AddMinutes(-10),
                UpdatedAt = Now.AddMinutes(-10),
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Roles.Add(new RoleRow
            {
                RoleId = roleId,
                Name = "R3 revocation role",
                Description = "Synthetic local revocation fixture.",
                IsSystem = false,
                ConcurrencyToken = Guid.NewGuid(),
            });
            context.Permissions.Add(new PermissionRow
            {
                PermissionId = permissionId,
                Code = PlatformPermissions.AgentsRevoke,
                Description = "Revoke one Agent.",
            });
            context.RolePermissions.Add(new RolePermissionRow { RoleId = roleId, PermissionId = permissionId });
            context.RoleAssignments.Add(new RoleAssignmentRow
            {
                RoleAssignmentId = Guid.NewGuid(),
                UserId = userId,
                RoleId = roleId,
                ScopeType = "Global",
                ScopeValue = "*",
                GrantedByUserId = userId,
                GrantedAt = Now.AddMinutes(-5),
                ExpiresAt = Now.AddHours(1),
            });
        }

        private static X509Certificate2 CreateClientCertificate()
        {
            using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CertificateRequest request = new("CN=DB-Notifier R3 Fixture", key, HashAlgorithmName.SHA256);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            OidCollection usages = new();
            usages.Add(new Oid("1.3.6.1.5.5.7.3.2"));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(usages, false));
            using X509Certificate2 ephemeral = request.CreateSelfSigned(Now.AddMinutes(-5), Now.AddHours(1));
            return X509CertificateLoader.LoadPkcs12(
                ephemeral.Export(X509ContentType.Pkcs12),
                null,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
        }

        private static string ComputePublicKeyDigest(X509Certificate2 certificate)
        {
            using ECDsa publicKey = certificate.GetECDsaPublicKey() ??
                throw new InvalidOperationException("The synthetic certificate did not expose its public key.");
            return Convert.ToHexString(SHA256.HashData(publicKey.ExportSubjectPublicKeyInfo()));
        }

        private static string NormaliseThumbprint(string value) =>
            value.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    /// <summary>Fails exactly the first armed certificate save while leaving the principal transaction untouched.</summary>
    private sealed class FailFirstCertificateSaveInterceptor : SaveChangesInterceptor
    {
        private int armed;

        internal void Arm() => Interlocked.Exchange(ref armed, 1);

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<AgentCertificateRow>()
                    .Any(entry => entry.State == EntityState.Modified) == true &&
                Interlocked.CompareExchange(ref armed, 0, 1) == 1)
            {
                throw new DbUpdateException("Synthetic certificate reconciliation interruption.");
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class ServerContextFactory(DbContextOptions<ServerDbContext> options)
        : IDbContextFactory<ServerDbContext>
    {
        /// <inheritdoc />
        public ServerDbContext CreateDbContext() => new(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }
}
