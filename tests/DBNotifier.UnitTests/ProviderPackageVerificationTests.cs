// Module purpose: Verifies Provider Package Verification Tests behaviour and protects the documented project contract.
using System.Runtime.InteropServices;
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
        Assert.Equal(64, result.ContentSha256.Length);
        Assert.Equal(["provider.dll"], result.FilePaths);
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

    [Fact]
    public async Task SnapshotDoesNotObserveMutationAfterVerification()
    {
        using PackageFixture fixture = await PackageFixture.CreateAsync();
        ProviderPackageVerifier verifier = new(new Dictionary<string, string> { ["test-key"] = fixture.PublicKey });
        VerifiedProviderPackage result = await verifier.VerifyAsync(fixture.PackageRoot);

        await File.WriteAllTextAsync(Path.Combine(fixture.PackageRoot, "provider.dll"), "mutated-after-verification");

        using Stream stream = result.OpenFile("provider.dll");
        using StreamReader reader = new(stream);
        Assert.Equal("not-an-assembly-and-never-loaded", await reader.ReadToEndAsync());
        Assert.False(stream.CanWrite);
    }

    [Fact]
    public async Task RejectsExtraFilesAndPortableCaseCollisions()
    {
        using PackageFixture extraFixture = await PackageFixture.CreateAsync();
        await File.WriteAllTextAsync(Path.Combine(extraFixture.PackageRoot, "extra.txt"), "unsigned");
        ProviderPackageVerifier extraVerifier = new(
            new Dictionary<string, string> { ["test-key"] = extraFixture.PublicKey });
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await extraVerifier.VerifyAsync(extraFixture.PackageRoot));

        using PackageFixture caseFixture = await PackageFixture.CreateAsync();
        ProviderPackageFile original = Assert.Single(caseFixture.Manifest.Files);
        await caseFixture.RewriteManifestAsync(caseFixture.Manifest with
        {
            Files = [original, original with { Path = "Provider.dll" }],
        });
        ProviderPackageVerifier caseVerifier = new(
            new Dictionary<string, string> { ["test-key"] = caseFixture.PublicKey });
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await caseVerifier.VerifyAsync(caseFixture.PackageRoot));
    }

    [Fact]
    public async Task RejectsHardLinkedSignedFilesOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PackageFixture fixture = await PackageFixture.CreateAsync();
        string linkedPath = Path.Combine(fixture.PackageRoot, "linked.dll");
        Assert.True(CreateHardLink(linkedPath, Path.Combine(fixture.PackageRoot, "provider.dll"), IntPtr.Zero));
        ProviderPackageFile original = Assert.Single(fixture.Manifest.Files);
        await fixture.RewriteManifestAsync(fixture.Manifest with
        {
            Files = [original, original with { Path = "linked.dll" }],
        });
        ProviderPackageVerifier verifier = new(new Dictionary<string, string> { ["test-key"] = fixture.PublicKey });

        await Assert.ThrowsAsync<InvalidDataException>(async () => await verifier.VerifyAsync(fixture.PackageRoot));
    }

    private sealed class PackageFixture(
        string discoveryRoot,
        string packageRoot,
        string publicKey,
        RSA signingKey,
        ProviderPackageManifest manifest) : IDisposable
    {
        public string DiscoveryRoot { get; } = discoveryRoot;
        public string PackageRoot { get; } = packageRoot;
        public string PublicKey { get; } = publicKey;
        public ProviderPackageManifest Manifest { get; private set; } = manifest;

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
            RSA rsa = RSA.Create(2048);
            byte[] signature = rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            await File.WriteAllTextAsync(Path.Combine(package, "dbnotifier-provider.sig"), Convert.ToBase64String(signature));
            return new(root, package, rsa.ExportSubjectPublicKeyInfoPem(), rsa, manifest);
        }

        public async Task RewriteManifestAsync(ProviderPackageManifest manifest)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, SerializerOptions);
            await File.WriteAllBytesAsync(Path.Combine(PackageRoot, "dbnotifier-provider.json"), bytes);
            byte[] signature = signingKey.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            await File.WriteAllTextAsync(
                Path.Combine(PackageRoot, "dbnotifier-provider.sig"), Convert.ToBase64String(signature));
            Manifest = manifest;
        }

        public void Dispose()
        {
            signingKey.Dispose();
            if (Directory.Exists(DiscoveryRoot))
            {
                Directory.Delete(DiscoveryRoot, true);
            }
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string fileName, string existingFileName, IntPtr securityAttributes);
}
