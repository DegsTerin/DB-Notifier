// Module purpose: Verifies Provider Package Verification Tests behaviour and protects the documented project contract.
using System.Security.Cryptography;
using System.Text.Json;
using DBNotifier.Infrastructure.Providers;

namespace DBNotifier.UnitTests;

public sealed class ProviderPackageVerificationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task VerifiesSignedNet10PackageWithoutLoadingAssembly()
    {
        using PackageFixture fixture = await PackageFixture.CreateAsync();
        ProviderPackageVerifier verifier = new(new Dictionary<string, string> { ["test-key"] = fixture.PublicKey });
        VerifiedProviderPackage result = await verifier.VerifyAsync(fixture.PackageRoot);
        Assert.Equal("postgresql", result.Manifest.ProviderId);
        Assert.Equal("net10.0", result.Manifest.TargetFramework);
    }

    [Fact]
    public async Task RejectsTamperedFileAndUntrustedSignature()
    {
        using PackageFixture fixture = await PackageFixture.CreateAsync();
        await File.WriteAllTextAsync(Path.Combine(fixture.PackageRoot, "provider.dll"), "tampered");
        ProviderPackageVerifier trusted = new(new Dictionary<string, string> { ["test-key"] = fixture.PublicKey });
        await Assert.ThrowsAsync<InvalidDataException>(async () => await trusted.VerifyAsync(fixture.PackageRoot));
        ProviderPackageVerifier untrusted = new(new Dictionary<string, string>());
        await Assert.ThrowsAsync<InvalidDataException>(async () => await untrusted.VerifyAsync(fixture.PackageRoot));
    }

    [Fact]
    public async Task DiscoveryFailsClosedAndReportsInvalidPackages()
    {
        using PackageFixture fixture = await PackageFixture.CreateAsync();
        Directory.CreateDirectory(Path.Combine(fixture.DiscoveryRoot, "unsigned"));
        ProviderPackageDiscovery discovery = new(new ProviderPackageVerifier(
            new Dictionary<string, string> { ["test-key"] = fixture.PublicKey }));
        ProviderPackageDiscoveryResult result = await discovery.DiscoverAsync(fixture.DiscoveryRoot);
        Assert.Single(result.VerifiedPackages);
        Assert.Single(result.Diagnostics);
    }

    private sealed class PackageFixture(string discoveryRoot, string packageRoot, string publicKey) : IDisposable
    {
        public string DiscoveryRoot { get; } = discoveryRoot;
        public string PackageRoot { get; } = packageRoot;
        public string PublicKey { get; } = publicKey;

        public static async Task<PackageFixture> CreateAsync()
        {
            string root = Path.Combine(Path.GetTempPath(), $"dbnotifier-provider-{Guid.NewGuid():N}");
            string package = Path.Combine(root, "postgresql");
            Directory.CreateDirectory(package);
            string binary = Path.Combine(package, "provider.dll");
            await File.WriteAllTextAsync(binary, "not-an-assembly-and-never-loaded");
            string hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(binary))).ToLowerInvariant();
            ProviderPackageManifest manifest = new(1, "postgresql", "DBNotifier.Provider.PostgreSql", "0.1.0",
                "net10.0", "provider.dll", "Fixture.Provider", "test-key", [new("provider.dll", hash)]);
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, SerializerOptions);
            await File.WriteAllBytesAsync(Path.Combine(package, "dbnotifier-provider.json"), bytes);
            using RSA rsa = RSA.Create(2048);
            byte[] signature = rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            await File.WriteAllTextAsync(Path.Combine(package, "dbnotifier-provider.sig"), Convert.ToBase64String(signature));
            return new(root, package, rsa.ExportSubjectPublicKeyInfoPem());
        }

        public void Dispose() { if (Directory.Exists(DiscoveryRoot)) Directory.Delete(DiscoveryRoot, true); }
    }
}
