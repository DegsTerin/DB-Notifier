using System.Security.Cryptography;
using System.Text.Json;

namespace DBNotifier.Infrastructure.Providers;

public sealed record ProviderPackageFile(string Path, string Sha256);

public sealed record ProviderPackageManifest(
    int SchemaVersion,
    string ProviderId,
    string PackageId,
    string PackageVersion,
    string TargetFramework,
    string EntryAssembly,
    string EntryType,
    string SigningKeyId,
    IReadOnlyList<ProviderPackageFile> Files);

public sealed record VerifiedProviderPackage(string RootPath, ProviderPackageManifest Manifest);

public sealed record ProviderPackageDiagnostic(string PackagePath, string Code);

public sealed record ProviderPackageDiscoveryResult(
    IReadOnlyList<VerifiedProviderPackage> VerifiedPackages,
    IReadOnlyList<ProviderPackageDiagnostic> Diagnostics);

public sealed class ProviderPackageVerifier(IReadOnlyDictionary<string, string> trustedPublicKeys)
{
    private const int MaximumManifestBytes = 64 * 1024;
    private const long MaximumPackageFileBytes = 100 * 1024 * 1024;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<VerifiedProviderPackage> VerifyAsync(
        string packageRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageRoot);
        string root = Path.GetFullPath(packageRoot);
        EnsureOrdinaryDirectory(root);
        string manifestPath = SafePath(root, "dbnotifier-provider.json");
        string signaturePath = SafePath(root, "dbnotifier-provider.sig");
        byte[] manifestBytes = await ReadBoundedAsync(manifestPath, MaximumManifestBytes, cancellationToken)
            .ConfigureAwait(false);
        byte[] signatureBytes;
        try
        {
            string signature = System.Text.Encoding.ASCII.GetString(
                await ReadBoundedAsync(signaturePath, 16 * 1024, cancellationToken).ConfigureAwait(false)).Trim();
            signatureBytes = Convert.FromBase64String(signature);
        }
        catch (Exception exception) when (exception is FormatException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException("The provider package signature is invalid.", exception);
        }

        ProviderPackageManifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<ProviderPackageManifest>(manifestBytes, SerializerOptions)
                ?? throw new InvalidDataException("The provider manifest is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The provider manifest is invalid.", exception);
        }

        ValidateManifest(manifest);
        if (!trustedPublicKeys.TryGetValue(manifest.SigningKeyId, out string? publicKeyPem))
        {
            throw new InvalidDataException("The provider signing key is not trusted.");
        }

        using RSA rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(publicKeyPem);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("The configured provider signing key is invalid.", exception);
        }

        bool signatureValid;
        try
        {
            signatureValid = rsa.VerifyData(manifestBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }
        catch (CryptographicException exception)
        {
            throw new InvalidDataException("The provider manifest signature is invalid.", exception);
        }

        if (!signatureValid)
        {
            throw new InvalidDataException("The provider manifest signature does not match.");
        }

        foreach (ProviderPackageFile file in manifest.Files)
        {
            string path = SafePath(root, file.Path);
            FileInfo info = new(path);
            EnsureOrdinaryFile(info);
            if (info.Length > MaximumPackageFileBytes)
            {
                throw new InvalidDataException("A provider package file exceeds the size policy.");
            }

            await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(hash, Convert.FromHexString(file.Sha256)))
            {
                throw new InvalidDataException("A provider package file hash does not match.");
            }
        }

        if (!manifest.Files.Any(file => string.Equals(file.Path, manifest.EntryAssembly, StringComparison.Ordinal)))
        {
            throw new InvalidDataException("The provider entry assembly is not covered by the signed hash list.");
        }

        return new VerifiedProviderPackage(root, manifest);
    }

    private static void ValidateManifest(ProviderPackageManifest manifest)
    {
        StringComparer pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        if (manifest.SchemaVersion != 1 || string.IsNullOrWhiteSpace(manifest.ProviderId) || manifest.ProviderId.Length > 64 ||
            string.IsNullOrWhiteSpace(manifest.PackageId) || manifest.PackageId.Length > 200 ||
            string.IsNullOrWhiteSpace(manifest.PackageVersion) || manifest.PackageVersion.Length > 64 ||
            manifest.TargetFramework != "net10.0" || string.IsNullOrWhiteSpace(manifest.EntryAssembly) ||
            manifest.EntryAssembly.Length > 260 || string.IsNullOrWhiteSpace(manifest.EntryType) ||
            manifest.EntryType.Length > 500 || string.IsNullOrWhiteSpace(manifest.SigningKeyId) ||
            manifest.SigningKeyId.Length > 200 ||
            manifest.Files is null || manifest.Files.Count is < 1 or > 256 ||
            manifest.Files.Select(file => file.Path).Distinct(pathComparer).Count() != manifest.Files.Count)
        {
            throw new InvalidDataException("The provider manifest violates package policy.");
        }

        foreach (ProviderPackageFile file in manifest.Files)
        {
            if (string.IsNullOrWhiteSpace(file.Path) || file.Path.Length > 260 || file.Sha256?.Length != 64 ||
                !file.Sha256.All(Uri.IsHexDigit))
            {
                throw new InvalidDataException("The provider file manifest is invalid.");
            }
        }
    }

    private static string SafePath(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException("Provider package paths must be relative.");
        }

        string candidate = Path.GetFullPath(Path.Combine(root, relativePath));
        string prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!candidate.StartsWith(prefix, comparison))
        {
            throw new InvalidDataException("Provider package path traversal was rejected.");
        }

        string? parent = Path.GetDirectoryName(candidate);
        while (!string.IsNullOrEmpty(parent) && !string.Equals(parent, root, comparison))
        {
            DirectoryInfo directory = new(parent);
            if (directory.Exists && (directory.LinkTarget is not null || directory.Attributes.HasFlag(FileAttributes.ReparsePoint)))
            {
                throw new InvalidDataException("Provider package links and reparse points are not allowed.");
            }

            parent = directory.Parent?.FullName;
        }

        return candidate;
    }

    private static void EnsureOrdinaryDirectory(string path)
    {
        DirectoryInfo info = new(path);
        if (!info.Exists || info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException("The provider package root is not an ordinary directory.");
        }
    }

    private static void EnsureOrdinaryFile(FileInfo info)
    {
        if (!info.Exists || info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException("Provider package links and reparse points are not allowed.");
        }
    }

    private static async ValueTask<byte[]> ReadBoundedAsync(string path, int maximumBytes, CancellationToken cancellationToken)
    {
        FileInfo info = new(path);
        EnsureOrdinaryFile(info);
        if (info.Length > maximumBytes)
        {
            throw new InvalidDataException("The provider manifest exceeds the size policy.");
        }

        return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ProviderPackageDiscovery(ProviderPackageVerifier verifier)
{
    public async ValueTask<ProviderPackageDiscoveryResult> DiscoverAsync(
        string packagesRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagesRoot);
        string root = Path.GetFullPath(packagesRoot);
        DirectoryInfo rootInfo = new(root);
        if (!rootInfo.Exists || rootInfo.LinkTarget is not null || rootInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException("The provider discovery root is not an ordinary directory.");
        }

        DirectoryInfo[] candidates = rootInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Take(101).ToArray();
        if (candidates.Length > 100)
        {
            throw new InvalidDataException("The provider discovery root exceeds the package count policy.");
        }

        List<VerifiedProviderPackage> verified = [];
        List<ProviderPackageDiagnostic> diagnostics = [];
        foreach (DirectoryInfo candidate in candidates)
        {
            try
            {
                verified.Add(await verifier.VerifyAsync(candidate.FullName, cancellationToken).ConfigureAwait(false));
            }
            catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException or CryptographicException)
            {
                diagnostics.Add(new(candidate.FullName, "provider.package_verification_failed"));
            }
        }

        return new(verified, diagnostics);
    }
}
