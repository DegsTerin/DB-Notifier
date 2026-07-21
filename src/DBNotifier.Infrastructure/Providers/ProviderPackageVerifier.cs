// Module purpose: Verifies provider packages into bounded immutable snapshots without loading provider code.
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace DBNotifier.Infrastructure.Providers;

/// <summary>Describes one signed provider-package file.</summary>
/// <param name="Path">Canonical package-relative path using forward slashes.</param>
/// <param name="Sha256">Expected lowercase or uppercase SHA-256 digest.</param>
public sealed record ProviderPackageFile(string Path, string Sha256);

/// <summary>Defines the signed, provider-neutral package manifest accepted by the disabled package boundary.</summary>
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

/// <summary>
/// Holds an immutable, content-addressed in-memory snapshot of a verified provider package.
/// </summary>
/// <remarks>
/// No source-directory path is exposed. Consumers can read only bytes captured during verification;
/// the class does not load assemblies and source mutations cannot alter the snapshot.
/// </remarks>
public sealed class VerifiedProviderPackage
{
    private readonly IReadOnlyDictionary<string, byte[]> files;

    internal VerifiedProviderPackage(
        string contentSha256,
        ProviderPackageManifest manifest,
        IReadOnlyDictionary<string, byte[]> files)
    {
        ContentSha256 = contentSha256;
        Manifest = manifest with
        {
            Files = new ReadOnlyCollection<ProviderPackageFile>(manifest.Files.ToArray()),
        };
        this.files = new ReadOnlyDictionary<string, byte[]>(files.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.Ordinal));
        FilePaths = Array.AsReadOnly(this.files.Keys.Order(StringComparer.Ordinal).ToArray());
    }

    /// <summary>Gets the SHA-256 identity of the complete verified snapshot.</summary>
    public string ContentSha256 { get; }

    /// <summary>Gets the validated manifest detached from the mutable source directory.</summary>
    public ProviderPackageManifest Manifest { get; }

    /// <summary>Gets the canonical paths present in the signed snapshot.</summary>
    public IReadOnlyList<string> FilePaths { get; }

    /// <summary>Opens a read-only stream over a file captured in the verified snapshot.</summary>
    /// <param name="relativePath">Canonical signed package path.</param>
    /// <returns>A non-writable stream whose buffer is not publicly visible.</returns>
    /// <exception cref="FileNotFoundException">The path is not part of the signed snapshot.</exception>
    public Stream OpenFile(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (!files.TryGetValue(relativePath, out byte[]? content))
        {
            throw new FileNotFoundException("The file is not present in the verified provider snapshot.");
        }
        return new MemoryStream(content, 0, content.Length, writable: false, publiclyVisible: false);
    }
}

/// <summary>Records a sanitised package-discovery failure.</summary>
public sealed record ProviderPackageDiagnostic(string PackagePath, string Code);

/// <summary>Returns immutable verified snapshots and fail-closed diagnostics from local discovery.</summary>
public sealed record ProviderPackageDiscoveryResult(
    IReadOnlyList<VerifiedProviderPackage> VerifiedPackages,
    IReadOnlyList<ProviderPackageDiagnostic> Diagnostics);

/// <summary>
/// Verifies a bounded exact package tree, signature and file hashes before producing an immutable snapshot.
/// </summary>
public sealed class ProviderPackageVerifier(IReadOnlyDictionary<string, string> trustedPublicKeys)
{
    private const int MaximumManifestBytes = 64 * 1024;
    private const int MaximumSignatureBytes = 16 * 1024;
    private const long MaximumPackageFileBytes = 64 * 1024 * 1024;
    private const long MaximumPackageBytes = 128 * 1024 * 1024;
    private const int MaximumTreeEntries = 512;
    private const string ManifestName = "dbnotifier-provider.json";
    private const string SignatureName = "dbnotifier-provider.sig";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Verifies one local package without executing or dynamically loading any package content.</summary>
    /// <param name="packageRoot">Absolute or relative path to the candidate package directory.</param>
    /// <param name="cancellationToken">Cancellation for all bounded reads.</param>
    /// <returns>An immutable, content-addressed snapshot.</returns>
    /// <exception cref="InvalidDataException">The tree, signature, identity, size or content violates policy.</exception>
    public async ValueTask<VerifiedProviderPackage> VerifyAsync(
        string packageRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageRoot);
        string root = Path.GetFullPath(packageRoot);
        EnsureOrdinaryDirectory(root);
        PackageTree initialTree = EnumerateExactTree(root);
        PackageFileSnapshot manifestSnapshot = await ReadSnapshotAsync(
            SafePath(root, ManifestName), MaximumManifestBytes, cancellationToken).ConfigureAwait(false);
        PackageFileSnapshot signatureSnapshot = await ReadSnapshotAsync(
            SafePath(root, SignatureName), MaximumSignatureBytes, cancellationToken).ConfigureAwait(false);

        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(Encoding.ASCII.GetString(signatureSnapshot.Content).Trim());
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("The provider package signature is invalid.", exception);
        }

        ProviderPackageManifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<ProviderPackageManifest>(manifestSnapshot.Content, SerializerOptions)
                ?? throw new InvalidDataException("The provider manifest is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The provider manifest is invalid.", exception);
        }

        ValidateManifest(manifest);
        HashSet<string> expectedFiles = new(StringComparer.Ordinal) { ManifestName, SignatureName };
        foreach (ProviderPackageFile file in manifest.Files)
        {
            expectedFiles.Add(file.Path);
        }
        HashSet<string> expectedDirectories = RequiredDirectories(expectedFiles);
        if (!initialTree.Files.SetEquals(expectedFiles) || !initialTree.Directories.SetEquals(expectedDirectories))
        {
            throw new InvalidDataException("The provider package tree differs from the signed exact tree.");
        }

        if (!trustedPublicKeys.TryGetValue(manifest.SigningKeyId, out string? publicKeyPem))
        {
            throw new InvalidDataException("The provider signing key is not trusted.");
        }
        using RSA rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(publicKeyPem);
            if (!rsa.VerifyData(
                manifestSnapshot.Content,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss))
            {
                throw new InvalidDataException("The provider manifest signature does not match.");
            }
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException)
        {
            throw new InvalidDataException("The provider signing material is invalid.", exception);
        }

        Dictionary<string, PackageFileSnapshot> snapshots = new(StringComparer.Ordinal)
        {
            [ManifestName] = manifestSnapshot,
            [SignatureName] = signatureSnapshot,
        };
        long aggregateBytes = checked(manifestSnapshot.Content.LongLength + signatureSnapshot.Content.LongLength);
        foreach (ProviderPackageFile file in manifest.Files.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            PackageFileSnapshot snapshot = await ReadSnapshotAsync(
                SafePath(root, file.Path), MaximumPackageFileBytes, cancellationToken).ConfigureAwait(false);
            aggregateBytes = checked(aggregateBytes + snapshot.Content.LongLength);
            if (aggregateBytes > MaximumPackageBytes)
            {
                throw new InvalidDataException("The provider package exceeds the aggregate size policy.");
            }
            if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(snapshot.Sha256), Convert.FromHexString(file.Sha256)))
            {
                throw new InvalidDataException("A provider package file hash does not match.");
            }
            snapshots.Add(file.Path, snapshot);
        }

        PackageTree finalTree = EnumerateExactTree(root);
        if (!initialTree.Equals(finalTree))
        {
            throw new InvalidDataException("The provider package tree changed during verification.");
        }
        foreach ((string relativePath, PackageFileSnapshot expected) in snapshots)
        {
            PackageFileSnapshot current = await ReadSnapshotAsync(
                SafePath(root, relativePath),
                relativePath == ManifestName ? MaximumManifestBytes :
                    relativePath == SignatureName ? MaximumSignatureBytes : MaximumPackageFileBytes,
                cancellationToken).ConfigureAwait(false);
            if (!expected.Identity.SameFile(current.Identity) ||
                !string.Equals(expected.Sha256, current.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException("A provider package file changed during verification.");
            }
        }

        if (!manifest.Files.Any(file => string.Equals(file.Path, manifest.EntryAssembly, StringComparison.Ordinal)))
        {
            throw new InvalidDataException("The provider entry assembly is not covered by the signed hash list.");
        }

        string snapshotHash = ComputeSnapshotHash(snapshots);
        Dictionary<string, byte[]> signedFiles = manifest.Files.ToDictionary(
            file => file.Path,
            file => snapshots[file.Path].Content,
            StringComparer.Ordinal);
        return new VerifiedProviderPackage(snapshotHash, manifest, signedFiles);
    }

    private static void ValidateManifest(ProviderPackageManifest manifest)
    {
        if (manifest.SchemaVersion != 1 || string.IsNullOrWhiteSpace(manifest.ProviderId) || manifest.ProviderId.Length > 64 ||
            string.IsNullOrWhiteSpace(manifest.PackageId) || manifest.PackageId.Length > 200 ||
            string.IsNullOrWhiteSpace(manifest.PackageVersion) || manifest.PackageVersion.Length > 64 ||
            manifest.TargetFramework != "net10.0" || string.IsNullOrWhiteSpace(manifest.EntryAssembly) ||
            manifest.EntryAssembly.Length > 260 || string.IsNullOrWhiteSpace(manifest.EntryType) ||
            manifest.EntryType.Length > 500 || string.IsNullOrWhiteSpace(manifest.SigningKeyId) ||
            manifest.SigningKeyId.Length > 200 || manifest.Files is null || manifest.Files.Count is < 1 or > 256)
        {
            throw new InvalidDataException("The provider manifest violates package policy.");
        }

        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
        foreach (ProviderPackageFile file in manifest.Files)
        {
            string canonical = NormaliseRelativePath(file.Path);
            if (!string.Equals(canonical, file.Path, StringComparison.Ordinal) || !paths.Add(file.Path) ||
                file.Sha256?.Length != 64 || !file.Sha256.All(Uri.IsHexDigit))
            {
                throw new InvalidDataException("The provider file manifest is invalid or has a case collision.");
            }
        }
        if (!string.Equals(NormaliseRelativePath(manifest.EntryAssembly), manifest.EntryAssembly, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The provider entry assembly path is not canonical.");
        }
    }

    private static string NormaliseRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Length > 260 || Path.IsPathRooted(relativePath) ||
            relativePath.Contains('\\') || relativePath.Contains('\0'))
        {
            throw new InvalidDataException("Provider package paths must be canonical and relative.");
        }
        string[] segments = relativePath.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".." || segment.Contains(':')))
        {
            throw new InvalidDataException("Provider package path traversal or aliases were rejected.");
        }
        return string.Join('/', segments);
    }

    private static string SafePath(string root, string relativePath)
    {
        string canonical = NormaliseRelativePath(relativePath);
        string candidate = Path.GetFullPath(Path.Combine(root, canonical.Replace('/', Path.DirectorySeparatorChar)));
        string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, PathComparison()))
        {
            throw new InvalidDataException("Provider package path traversal was rejected.");
        }
        EnsureNoLinkedParents(root, Path.GetDirectoryName(candidate)!);
        return candidate;
    }

    private static PackageTree EnumerateExactTree(string root)
    {
        HashSet<string> files = new(StringComparer.Ordinal);
        HashSet<string> directories = new(StringComparer.Ordinal);
        HashSet<string> portableNames = new(StringComparer.OrdinalIgnoreCase);
        Stack<DirectoryInfo> pending = new();
        pending.Push(new(root));
        int entries = 0;
        while (pending.Count > 0)
        {
            DirectoryInfo directory = pending.Pop();
            EnsureOrdinaryDirectory(directory.FullName);
            foreach (FileSystemInfo entry in directory.EnumerateFileSystemInfos())
            {
                if (++entries > MaximumTreeEntries || entry.LinkTarget is not null ||
                    entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new InvalidDataException("The provider package tree exceeds policy or contains a link.");
                }
                string relative = Path.GetRelativePath(root, entry.FullName).Replace('\\', '/');
                if (!portableNames.Add(relative))
                {
                    throw new InvalidDataException("The provider package tree contains a portable case collision.");
                }
                if (entry is DirectoryInfo child)
                {
                    directories.Add(relative);
                    pending.Push(child);
                }
                else if (entry is FileInfo)
                {
                    files.Add(relative);
                }
                else
                {
                    throw new InvalidDataException("The provider package tree contains an unsupported entry.");
                }
            }
        }
        return new(files, directories);
    }

    private static HashSet<string> RequiredDirectories(IEnumerable<string> files)
    {
        HashSet<string> result = new(StringComparer.Ordinal);
        foreach (string file in files)
        {
            string? parent = Path.GetDirectoryName(file)?.Replace('\\', '/');
            while (!string.IsNullOrEmpty(parent))
            {
                result.Add(parent);
                parent = Path.GetDirectoryName(parent)?.Replace('\\', '/');
            }
        }
        return result;
    }

    private static async ValueTask<PackageFileSnapshot> ReadSnapshotAsync(
        string path,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        FileInfo info = new(path);
        if (!info.Exists || info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException("Provider package links and reparse points are not allowed.");
        }
        await using FileStream stream = new(
            path, FileMode.Open, FileAccess.Read, FileShare.None, 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        info.Refresh();
        PackageFileIdentity identity = GetHandleIdentity(stream.SafeFileHandle, stream.Length, info.LastWriteTimeUtc);
        if (identity.LinkCount != 1 || stream.Length > maximumBytes)
        {
            throw new InvalidDataException("A provider package file violates link or size policy.");
        }
        byte[] content = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(content, cancellationToken).ConfigureAwait(false);
        return new(content, Convert.ToHexStringLower(SHA256.HashData(content)), identity);
    }

    private static string ComputeSnapshotHash(IReadOnlyDictionary<string, PackageFileSnapshot> snapshots)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach ((string path, PackageFileSnapshot snapshot) in snapshots.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            byte[] pathBytes = Encoding.UTF8.GetBytes(path);
            hash.AppendData(BitConverter.GetBytes(pathBytes.Length));
            hash.AppendData(pathBytes);
            hash.AppendData(Convert.FromHexString(snapshot.Sha256));
        }
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void EnsureOrdinaryDirectory(string path)
    {
        DirectoryInfo info = new(path);
        if (!info.Exists || info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException("The provider package directory is not ordinary.");
        }
    }

    private static void EnsureNoLinkedParents(string root, string directoryPath)
    {
        string current = directoryPath;
        while (!string.Equals(current, root, PathComparison()))
        {
            EnsureOrdinaryDirectory(current);
            current = Path.GetDirectoryName(current)
                ?? throw new InvalidDataException("A provider package path escaped its root.");
        }
        EnsureOrdinaryDirectory(root);
    }

    private static StringComparison PathComparison() =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static PackageFileIdentity GetHandleIdentity(
        SafeFileHandle handle,
        long length,
        DateTime lastWriteTimeUtc)
    {
        if (OperatingSystem.IsWindows())
        {
            if (!GetFileInformationByHandle(handle, out ByHandleFileInformation information))
            {
                throw new IOException("The provider package file identity could not be read.",
                    Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }
            ulong fileId = ((ulong)information.FileIndexHigh << 32) | information.FileIndexLow;
            return new(information.VolumeSerialNumber, fileId, information.NumberOfLinks, length, lastWriteTimeUtc);
        }
        if (OperatingSystem.IsLinux())
        {
            if (FStat(handle, out LinuxStat information) != 0)
            {
                throw new IOException("The provider package file identity could not be read.",
                    Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }
            return new(information.Device, information.Inode, information.LinkCount, length, lastWriteTimeUtc);
        }
        throw new PlatformNotSupportedException("Secure package file identity is supported only on Windows and Linux.");
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle fileHandle,
        out ByHandleFileInformation fileInformation);

    [DllImport("libc", EntryPoint = "fstat", SetLastError = true)]
    private static extern int FStat(SafeFileHandle fileDescriptor, out LinuxStat buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LinuxStat
    {
        public ulong Device;
        public ulong Inode;
        public ulong LinkCount;
        public uint Mode;
        public uint UserId;
        public uint GroupId;
        public int Padding;
        public ulong RawDevice;
        public long Size;
        public long BlockSize;
        public long BlockCount;
        public long AccessSeconds;
        public long AccessNanoseconds;
        public long ModificationSeconds;
        public long ModificationNanoseconds;
        public long ChangeSeconds;
        public long ChangeNanoseconds;
    }

    private sealed record PackageTree(HashSet<string> Files, HashSet<string> Directories)
    {
        public bool Equals(PackageTree? other) => other is not null &&
            Files.SetEquals(other.Files) && Directories.SetEquals(other.Directories);

        public override int GetHashCode() => Files.Count ^ Directories.Count;
    }

    private sealed record PackageFileSnapshot(
        byte[] Content,
        string Sha256,
        PackageFileIdentity Identity);

    private readonly record struct PackageFileIdentity(
        ulong Device,
        ulong FileId,
        ulong LinkCount,
        long Length,
        DateTime LastWriteTimeUtc)
    {
        public bool SameFile(PackageFileIdentity other) => Device == other.Device && FileId == other.FileId;
    }
}

/// <summary>Discovers bounded package candidates and returns snapshots without activating a loader.</summary>
public sealed class ProviderPackageDiscovery(ProviderPackageVerifier verifier)
{
    /// <summary>Verifies up to one hundred immediate package directories.</summary>
    /// <param name="packagesRoot">Root containing one directory per candidate package.</param>
    /// <param name="cancellationToken">Cancellation for package verification.</param>
    /// <returns>Verified immutable snapshots and sanitised failures.</returns>
    public async ValueTask<ProviderPackageDiscoveryResult> DiscoverAsync(
        string packagesRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagesRoot);
        string root = Path.GetFullPath(packagesRoot);
        EnsureDiscoveryRoot(root);
        DirectoryInfo[] candidates = new DirectoryInfo(root).EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
            .Take(101).ToArray();
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
            catch (Exception exception) when (exception is InvalidDataException or IOException or
                                              UnauthorizedAccessException or CryptographicException)
            {
                diagnostics.Add(new(candidate.FullName, "provider.package_verification_failed"));
            }
        }
        return new(verified, diagnostics);
    }

    private static void EnsureDiscoveryRoot(string root)
    {
        DirectoryInfo info = new(root);
        if (!info.Exists || info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidDataException("The provider discovery root is not an ordinary directory.");
        }
    }
}
