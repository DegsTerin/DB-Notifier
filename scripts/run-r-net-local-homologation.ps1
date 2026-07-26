# Module purpose: Runs the bounded local R-NET DNS, PKI, IdP and PostgreSQL TLS matrix with process-local trust and exact runtime cleanup.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$DotNetPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($DotNetPath)) {
    $DotNetPath = Join-Path $root '.dotnet\dotnet.exe'
}
if (-not (Test-Path -LiteralPath $DotNetPath -PathType Leaf)) {
    throw 'The requested .NET executable is unavailable.'
}

$resolvedDotNet = (Resolve-Path -LiteralPath $DotNetPath).Path
$integrationProject = Join-Path $root 'tests\DBNotifier.IntegrationTests\DBNotifier.IntegrationTests.csproj'
$dockerCommand = Get-Command docker.exe -CommandType Application -ErrorAction SilentlyContinue |
    Select-Object -First 1
if ($null -eq $dockerCommand) {
    throw 'The local Docker command is unavailable.'
}

$docker = $dockerCommand.Path
$imageReference =
    'postgres@sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb'
$runId = [guid]::NewGuid().ToString('N')
$ownershipLabel = 'com.db-notifier.r-net-local'
$runLabel = 'com.db-notifier.r-net-local.run'
$networkName = "db-notifier-r-net-$runId"
$systemTemporaryRoot = [System.IO.Path]::GetFullPath(
    [System.IO.Path]::GetTempPath()).TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar)
$temporaryLeaf = "DBNotifier-R-Net-$runId"
$temporaryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $systemTemporaryRoot $temporaryLeaf))
$expectedTemporaryRoot = Join-Path $systemTemporaryRoot $temporaryLeaf
$runnerLockPath = Join-Path $systemTemporaryRoot 'DBNotifier-R-Net.lock'
$secretFile = Join-Path $temporaryRoot 'postgres-secrets.json'
$administratorPasswordFile = Join-Path $temporaryRoot 'postgres-administrator-password.txt'
$monitorPasswordFile = Join-Path $temporaryRoot 'postgres-monitor-password.txt'
$initialisationFile = Join-Path $temporaryRoot 'postgres-monitor-role.sh'
$hostBasedAuthenticationFile = Join-Path $temporaryRoot 'postgres-hostssl.sh'
$resolverConfigurationFile = Join-Path $temporaryRoot 'resolv.conf'
$testResultsRoot = Join-Path $temporaryRoot 'test-results'
$fixtureHost = 'rnet-postgresql.localhost'
$networkProvisioningAttempted = $false
$currentContainer = $null
$testProcess = $null
$crlRegistration = $null
$runnerLock = $null
$preserveRecoveryRoot = $false
$cleanupFailures = [System.Collections.Generic.List[string]]::new()
$administratorPassword = $null
$monitorPassword = $null
$testInvocationCount = 0
$testCaseCount = 0
$successSummary = $null
$publishedPorts = [System.Collections.Generic.List[int]]::new()
$testProcessIdentities = [System.Collections.Generic.List[object]]::new()
$externalProcessIdentities = [System.Collections.Generic.List[object]]::new()

$fixtureSource = @'
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DBNotifier.RNetFixture;

// Generates a process-scoped public-key fixture without retaining private material outside the supplied directory.
public static class CertificateFixtureGenerator
{
    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";
    private const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";
    private const string IdpHost = "rnet-idp.localhost";
    private const string PostgreSqlHost = "rnet-postgresql.localhost";

    // Writes the exact certificate corpus consumed by the local DNS, PKI, IdP and PostgreSQL tests.
    public static void Generate(string fixtureRoot, string runId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Uri primaryCrl = new(Path.Combine(fixtureRoot, "root-ca.crl"));
        Uri missingCrl = new(Path.Combine(fixtureRoot, "missing-crl-root-ca.crl"));
        Uri untrustedCrl = new(Path.Combine(fixtureRoot, "untrusted-root-ca.crl"));

        using RSA rootKey = RSA.Create(3072);
        using X509Certificate2 root = CreateAuthority(
            rootKey,
            $"CN=DBNotifier RNET Local Root {runId}",
            now);
        using RSA missingRootKey = RSA.Create(3072);
        using X509Certificate2 missingRoot = CreateAuthority(
            missingRootKey,
            $"CN=DBNotifier RNET Missing CRL Root {runId}",
            now);
        using RSA untrustedRootKey = RSA.Create(3072);
        using X509Certificate2 untrustedRoot = CreateAuthority(
            untrustedRootKey,
            $"CN=DBNotifier RNET Untrusted Root {runId}",
            now);

        using X509Certificate2 idp = IssueServerCertificate(
            root,
            $"CN={IdpHost}",
            [IdpHost],
            primaryCrl,
            true,
            now);
        using X509Certificate2 postgres = IssueServerCertificate(
            root,
            $"CN={PostgreSqlHost}",
            [PostgreSqlHost],
            primaryCrl,
            true,
            now);
        using X509Certificate2 wrongName = IssueServerCertificate(
            root,
            "CN=wrong-rnet.localhost",
            ["wrong-rnet.localhost"],
            primaryCrl,
            true,
            now);
        using X509Certificate2 wrongUsage = IssueServerCertificate(
            root,
            $"CN={IdpHost}",
            [IdpHost],
            primaryCrl,
            false,
            now);
        using X509Certificate2 revoked = IssueServerCertificate(
            root,
            $"CN={IdpHost}",
            [IdpHost, PostgreSqlHost],
            primaryCrl,
            true,
            now);
        using X509Certificate2 untrusted = IssueServerCertificate(
            untrustedRoot,
            $"CN={IdpHost}",
            [IdpHost, PostgreSqlHost],
            untrustedCrl,
            true,
            now);
        using X509Certificate2 missing = IssueServerCertificate(
            missingRoot,
            $"CN={IdpHost}",
            [IdpHost, PostgreSqlHost],
            missingCrl,
            true,
            now);

        WritePublicCertificate(Path.Combine(fixtureRoot, "root-ca.crt"), root);
        WritePublicCertificate(
            Path.Combine(fixtureRoot, "missing-crl-root-ca.crt"),
            missingRoot);
        WriteServerMaterial(fixtureRoot, "idp-server", idp);
        WriteServerMaterial(fixtureRoot, "postgres-server", postgres);
        WriteServerMaterial(fixtureRoot, "wrong-san", wrongName);
        WriteServerMaterial(fixtureRoot, "wrong-eku", wrongUsage);
        WriteServerMaterial(fixtureRoot, "revoked", revoked);
        WriteServerMaterial(fixtureRoot, "untrusted", untrusted);
        WriteServerMaterial(fixtureRoot, "missing-crl", missing);

        CertificateRevocationListBuilder builder = new();
        builder.AddEntry(
            revoked,
            now.AddMinutes(-2),
            X509RevocationReason.KeyCompromise);
        byte[] encodedCrl = builder.Build(
            root,
            BigInteger.One,
            now.AddDays(1),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1,
            now.AddMinutes(-5));
        File.WriteAllBytes(Path.Combine(fixtureRoot, "root-ca.crl"), encodedCrl);
        CryptographicOperations.ZeroMemory(encodedCrl);
    }

    // Creates one short-lived self-signed CA whose private key remains only in the current process.
    private static X509Certificate2 CreateAuthority(
        RSA key,
        string subject,
        DateTimeOffset now)
    {
        CertificateRequest request = new(
            subject,
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign |
                X509KeyUsageFlags.CrlSign |
                X509KeyUsageFlags.DigitalSignature,
                true));
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        return request.CreateSelfSigned(now.AddDays(-1), now.AddDays(2));
    }

    // Issues one RSA leaf with a bounded SAN, explicit EKU and local CRL distribution point.
    private static X509Certificate2 IssueServerCertificate(
        X509Certificate2 issuer,
        string subject,
        IEnumerable<string> dnsNames,
        Uri crlUri,
        bool serverAuthentication,
        DateTimeOffset now)
    {
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new(
            subject,
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature |
                X509KeyUsageFlags.KeyEncipherment,
                true));
        OidCollection usages = new();
        usages.Add(new Oid(
            serverAuthentication
                ? ServerAuthenticationOid
                : ClientAuthenticationOid));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(usages, true));
        SubjectAlternativeNameBuilder alternativeNames = new();
        foreach (string dnsName in dnsNames)
        {
            alternativeNames.AddDnsName(dnsName);
        }
        request.CertificateExtensions.Add(alternativeNames.Build(true));
        request.CertificateExtensions.Add(
            CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(
                [crlUri.AbsoluteUri],
                false));
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        byte[] serial = RandomNumberGenerator.GetBytes(16);
        serial[0] |= 0x01;
        X509Certificate2 issued = request.Create(
            issuer,
            now.AddMinutes(-10),
            now.AddHours(12),
            serial);
        CryptographicOperations.ZeroMemory(serial);
        X509Certificate2 withKey = issued.CopyWithPrivateKey(key);
        issued.Dispose();
        return withKey;
    }

    // Writes only the public PEM representation of one authority.
    private static void WritePublicCertificate(
        string path,
        X509Certificate2 certificate) =>
        File.WriteAllText(
            path,
            certificate.ExportCertificatePem(),
            new UTF8Encoding(false));

    // Writes one leaf and its unencrypted PKCS#8 key to the runner-owned temporary directory.
    private static void WriteServerMaterial(
        string fixtureRoot,
        string stem,
        X509Certificate2 certificate)
    {
        WritePublicCertificate(
            Path.Combine(fixtureRoot, stem + ".crt"),
            certificate);
        using RSA key = certificate.GetRSAPrivateKey()
            ?? throw new CryptographicException("rnet.fixture_private_key_missing");
        File.WriteAllText(
            Path.Combine(fixtureRoot, stem + ".key"),
            key.ExportPkcs8PrivateKeyPem(),
            new UTF8Encoding(false));
    }
}

// Owns exactly one DER CRL added to the current-user CA store and removes only that exact context.
public sealed class ExactCurrentUserCrlRegistration : IDisposable
{
    private const uint X509AsnEncoding = 0x00000001;
    private const uint Pkcs7AsnEncoding = 0x00010000;
    private const uint AddNew = 1;
    private const int MaximumEnumeratedCrls = 1024;
    private const int CryptENotFound = unchecked((int)0x80092004);
    private readonly byte[] encoded;
    private IntPtr store;
    private IntPtr context;
    private IntPtr lookupContext;
    private bool additionSucceeded;
    private bool removalAttempted;
    private bool removalResult;

    private ExactCurrentUserCrlRegistration(
        byte[] encoded,
        IntPtr store,
        IntPtr context,
        IntPtr lookupContext)
    {
        this.encoded = encoded;
        this.store = store;
        this.context = context;
        this.lookupContext = lookupContext;
    }

    // Adds a previously absent CRL and proves its exact bytes are visible in the current-user CA store.
    public static ExactCurrentUserCrlRegistration Add(byte[] encoded)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        IntPtr lookupContext = CertCreateCRLContext(
            X509AsnEncoding | Pkcs7AsnEncoding,
            encoded,
            encoded.Length);
        if (lookupContext == IntPtr.Zero)
        {
            throw new CryptographicException("rnet.crl_context_invalid");
        }

        IntPtr store = CertOpenSystemStore(IntPtr.Zero, "CA");
        if (store == IntPtr.Zero)
        {
            CertFreeCRLContext(lookupContext);
            throw new CryptographicException("rnet.crl_store_open_failed");
        }

        try
        {
            if (ContainsExactEncodedCrl(store, encoded))
            {
                throw new CryptographicException("rnet.crl_preexisting");
            }

            ExactCurrentUserCrlRegistration registration =
                new((byte[])encoded.Clone(), store, IntPtr.Zero, lookupContext);
            store = IntPtr.Zero;
            lookupContext = IntPtr.Zero;
            try
            {
                bool added = CertAddCRLContextToStore(
                    registration.store,
                    registration.lookupContext,
                    AddNew,
                    out IntPtr ownedContext);
                registration.additionSucceeded =
                    added && ownedContext != IntPtr.Zero;
                if (registration.additionSucceeded)
                {
                    registration.context = ownedContext;
                }
                return registration;
            }
            catch
            {
                registration.RemoveAndVerify();
                throw;
            }
        }
        finally
        {
            if (store != IntPtr.Zero)
            {
                CertCloseStore(store, 0);
            }
            if (lookupContext != IntPtr.Zero)
            {
                CertFreeCRLContext(lookupContext);
            }
        }
    }

    // Removes an exact CRL recovered from a cryptographically self-identifying runner fixture.
    public static bool TryRemoveOwnedRecovery(
        string rootCertificatePath,
        string crlPath,
        string expectedRunId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootCertificatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(crlPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedRunId);
        FileInfo rootFile = new(rootCertificatePath);
        FileInfo crlFile = new(crlPath);
        if (!rootFile.Exists ||
            rootFile.Length is < 256 or > 16384 ||
            !crlFile.Exists ||
            crlFile.Length is < 128 or > 1048576)
        {
            return false;
        }

        using X509Certificate2 root =
            X509CertificateLoader.LoadCertificateFromFile(rootFile.FullName);
        if (root.HasPrivateKey ||
            !string.Equals(
                root.SubjectName.Name,
                $"CN=DBNotifier RNET Local Root {expectedRunId}",
                StringComparison.Ordinal))
        {
            return false;
        }

        bool isAuthority = false;
        bool canSignCrl = false;
        foreach (X509Extension extension in root.Extensions)
        {
            if (extension is X509BasicConstraintsExtension constraints)
            {
                isAuthority = constraints.CertificateAuthority;
            }
            else if (extension is X509KeyUsageExtension usage)
            {
                canSignCrl =
                    (usage.KeyUsages & X509KeyUsageFlags.CrlSign) != 0;
            }
        }
        if (!isAuthority || !canSignCrl)
        {
            return false;
        }

        byte[] encoded = File.ReadAllBytes(crlFile.FullName);
        try
        {
            return VerifyCrlSignature(root, encoded) && RemoveExact(encoded);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encoded);
        }
    }

    // Gets whether the exact context added by this registration is currently discoverable.
    public bool IsInstalledAndProved =>
        additionSucceeded &&
        store != IntPtr.Zero &&
        ContainsExactEncodedCrl(store, encoded);

    // Deletes only the context returned by the exact add operation and proves no equal DER CRL remains.
    public bool RemoveAndVerify()
    {
        if (removalAttempted)
        {
            return removalResult;
        }

        removalAttempted = true;
        IntPtr ownedContext = context;
        context = IntPtr.Zero;
        if (ownedContext != IntPtr.Zero)
        {
            CertDeleteCRLFromStore(ownedContext);
        }
        bool absent = false;
        bool storeClosed = true;
        bool lookupFreed = true;
        try
        {
            absent = store == IntPtr.Zero ||
                !ContainsExactEncodedCrl(store, encoded);
            if (!absent)
            {
                absent = RemoveExact(encoded);
            }
        }
        finally
        {
            IntPtr ownedStore = store;
            store = IntPtr.Zero;
            if (ownedStore != IntPtr.Zero)
            {
                storeClosed = CertCloseStore(ownedStore, 0);
            }
            IntPtr ownedLookup = lookupContext;
            lookupContext = IntPtr.Zero;
            if (ownedLookup != IntPtr.Zero)
            {
                lookupFreed = CertFreeCRLContext(ownedLookup);
            }
            CryptographicOperations.ZeroMemory(encoded);
        }
        removalResult = absent && storeClosed && lookupFreed;
        return removalResult;
    }

    public void Dispose() => RemoveAndVerify();

    // Performs a bounded byte-for-byte proof without treating issuer equivalence as exact ownership.
    private static bool ContainsExactEncodedCrl(
        IntPtr store,
        byte[] expected)
    {
        IntPtr current = IntPtr.Zero;
        int count = 0;
        try
        {
            while (true)
            {
                current = CertEnumCRLsInStore(store, current);
                if (current == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error != CryptENotFound)
                    {
                        throw new CryptographicException(
                            "rnet.crl_store_enumeration_failed");
                    }
                    return false;
                }

                count++;
                if (count > MaximumEnumeratedCrls)
                {
                    throw new CryptographicException(
                        "rnet.crl_store_enumeration_limit");
                }

                CrlContext native = Marshal.PtrToStructure<CrlContext>(current);
                if (native.EncodedLength != expected.Length)
                {
                    continue;
                }

                byte[] candidate = new byte[native.EncodedLength];
                Marshal.Copy(
                    native.EncodedPointer,
                    candidate,
                    0,
                    candidate.Length);
                bool equal =
                    CryptographicOperations.FixedTimeEquals(candidate, expected);
                CryptographicOperations.ZeroMemory(candidate);
                if (equal)
                {
                    bool freed = CertFreeCRLContext(current);
                    current = IntPtr.Zero;
                    if (!freed)
                    {
                        throw new CryptographicException(
                            "rnet.crl_context_release_failed");
                    }
                    return true;
                }
            }
        }
        finally
        {
            if (current != IntPtr.Zero)
            {
                CertFreeCRLContext(current);
            }
        }
    }

    // Reopens the current-user CA store and retries exact removal without relying on a retained native context.
    private static bool RemoveExact(byte[] expected)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            IntPtr store = CertOpenSystemStore(IntPtr.Zero, "CA");
            if (store == IntPtr.Zero)
            {
                continue;
            }

            bool absent = false;
            try
            {
                if (!ContainsExactEncodedCrl(store, expected))
                {
                    absent = true;
                }
                else
                {
                    bool deleted = DeleteExactEncodedCrl(store, expected);
                    absent =
                        deleted && !ContainsExactEncodedCrl(store, expected);
                }
            }
            finally
            {
                if (!CertCloseStore(store, 0))
                {
                    absent = false;
                }
            }
            if (absent)
            {
                return true;
            }
        }

        return false;
    }

    // Deletes only the first byte-for-byte CRL match; AddNew prevents an owned duplicate.
    private static bool DeleteExactEncodedCrl(
        IntPtr store,
        byte[] expected)
    {
        IntPtr current = IntPtr.Zero;
        int count = 0;
        try
        {
            while (true)
            {
                current = CertEnumCRLsInStore(store, current);
                if (current == IntPtr.Zero)
                {
                    return false;
                }

                count++;
                if (count > MaximumEnumeratedCrls)
                {
                    throw new CryptographicException(
                        "rnet.crl_store_enumeration_limit");
                }

                CrlContext native = Marshal.PtrToStructure<CrlContext>(current);
                if (native.EncodedLength != expected.Length)
                {
                    continue;
                }

                byte[] candidate = new byte[native.EncodedLength];
                Marshal.Copy(
                    native.EncodedPointer,
                    candidate,
                    0,
                    candidate.Length);
                bool equal =
                    CryptographicOperations.FixedTimeEquals(candidate, expected);
                CryptographicOperations.ZeroMemory(candidate);
                if (!equal)
                {
                    continue;
                }

                IntPtr owned = current;
                current = IntPtr.Zero;
                return CertDeleteCRLFromStore(owned);
            }
        }
        finally
        {
            if (current != IntPtr.Zero)
            {
                CertFreeCRLContext(current);
            }
        }
    }

    // Verifies that the recovery CRL was signed by the exact synthetic root retained beside it.
    private static bool VerifyCrlSignature(
        X509Certificate2 root,
        byte[] encoded)
    {
        try
        {
            AsnReader document = new(encoded, AsnEncodingRules.DER);
            AsnReader sequence = document.ReadSequence();
            ReadOnlyMemory<byte> toBeSigned = sequence.ReadEncodedValue();
            AsnReader algorithm = sequence.ReadSequence();
            string algorithmOid = algorithm.ReadObjectIdentifier();
            if (algorithm.HasData)
            {
                algorithm.ReadNull();
            }
            int unusedBits;
            byte[] signature = sequence.ReadBitString(out unusedBits);
            try
            {
                if (algorithm.HasData ||
                    sequence.HasData ||
                    document.HasData ||
                    unusedBits != 0 ||
                    !string.Equals(
                        algorithmOid,
                        "1.2.840.113549.1.1.11",
                        StringComparison.Ordinal))
                {
                    return false;
                }

                RSA publicKey = root.GetRSAPublicKey();
                if (publicKey is null)
                {
                    return false;
                }
                using (publicKey)
                {
                    return publicKey.VerifyData(
                        toBeSigned.Span,
                        signature,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(signature);
            }
        }
        catch (AsnContentException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CrlContext
    {
        internal uint EncodingType;
        internal IntPtr EncodedPointer;
        internal int EncodedLength;
        internal IntPtr CrlInfo;
        internal IntPtr Store;
    }

    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CertOpenSystemStore(
        IntPtr provider,
        string subsystemProtocol);

    [DllImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CertCloseStore(IntPtr store, uint flags);

    [DllImport("crypt32.dll", SetLastError = true)]
    private static extern IntPtr CertCreateCRLContext(
        uint encodingType,
        byte[] encoded,
        int encodedLength);

    [DllImport("crypt32.dll", SetLastError = true)]
    private static extern IntPtr CertEnumCRLsInStore(
        IntPtr store,
        IntPtr previousContext);

    [DllImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CertAddCRLContextToStore(
        IntPtr store,
        IntPtr sourceContext,
        uint disposition,
        out IntPtr context);

    [DllImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CertDeleteCRLFromStore(IntPtr context);

    [DllImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CertFreeCRLContext(IntPtr context);
}
'@

# Runs one argument-vector process under a total watchdog without invoking a command shell.
function Invoke-BoundedExternalProcess {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [ValidateRange(5, 300)]
        [int]$TimeoutSeconds = 60
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.WorkingDirectory = $root
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }
    $startInfo.Environment['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    $startInfo.Environment['DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE'] = '1'

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $started = $false
    $exitProved = $false
    try {
        if (-not $process.Start()) {
            throw 'A bounded local process could not be started.'
        }
        $started = $true
        $externalProcessIdentities.Add(
            [pscustomobject]@{
                ProcessId = $process.Id
                StartTimeUtc = $process.StartTime.ToUniversalTime()
            })
        $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
        $standardErrorTask = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            $process.Kill($true)
            if (-not $process.WaitForExit(10000)) {
                throw 'A bounded local process did not exit after its watchdog.'
            }
            $exitProved = $true
            throw 'A bounded local process exceeded its total watchdog.'
        }
        $exitProved = $true
        $standardOutput = $standardOutputTask.GetAwaiter().GetResult()
        $standardError = $standardErrorTask.GetAwaiter().GetResult()
        if ($standardOutput.Length -gt 2097152 -or
            $standardError.Length -gt 2097152) {
            throw 'A bounded local process exceeded its output limit.'
        }
        return [pscustomobject]@{
            ExitCode = $process.ExitCode
            StandardOutput = $standardOutput
            StandardError = $standardError
            OutputLines = @(
                $standardOutput -split '\r?\n' |
                    Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
            )
            ErrorLines = @(
                $standardError -split '\r?\n' |
                    Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
            )
        }
    }
    catch {
        throw 'A bounded local process failed or exceeded its watchdog.'
    }
    finally {
        if ($started) {
            try {
                if (-not $process.HasExited) {
                    $process.Kill($true)
                    $exitProved = $process.WaitForExit(10000)
                }
                else {
                    $exitProved = $true
                }
            }
            catch {
                $cleanupFailures.Add(
                    'A bounded local process could not be stopped by its watchdog.')
            }
        }
        $process.Dispose()
        if ($started -and -not $exitProved) {
            throw 'A bounded local process exit could not be proved.'
        }
    }
}

# Runs one Docker argument vector containing no credential value and requires a successful exit.
function Invoke-DockerChecked {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [ValidateRange(5, 120)]
        [int]$TimeoutSeconds = 60
    )

    $result = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments $Arguments `
        -TimeoutSeconds $TimeoutSeconds
    if ($result.ExitCode -ne 0) {
        throw 'A bounded Docker operation failed.'
    }
    return @($result.OutputLines)
}

# Returns one random URL-safe synthetic credential without emitting or retaining the source bytes.
function Get-SyntheticCredential {
    $bytes = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
    try {
        return [Convert]::ToBase64String($bytes).
            TrimEnd('=').
            Replace('+', 'A').
            Replace('/', 'B')
    }
    finally {
        [System.Security.Cryptography.CryptographicOperations]::ZeroMemory($bytes)
    }
}

# Restricts inherited access on the runner-owned temporary directory to the current user and LocalSystem.
function Set-PrivateTemporaryDirectoryAccess {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $identity = [Security.Principal.WindowsIdentity]::GetCurrent().User
    if ($null -eq $identity) {
        throw 'The current Windows identity SID is unavailable.'
    }
    $systemIdentity = [Security.Principal.SecurityIdentifier]::new(
        [Security.Principal.WellKnownSidType]::LocalSystemSid,
        $null)
    $security = [Security.AccessControl.DirectorySecurity]::new()
    $security.SetAccessRuleProtection($true, $false)
    $inheritance = [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor
        [Security.AccessControl.InheritanceFlags]::ObjectInherit
    foreach ($sid in @($identity, $systemIdentity)) {
        $rule = [Security.AccessControl.FileSystemAccessRule]::new(
            $sid,
            [Security.AccessControl.FileSystemRights]::FullControl,
            $inheritance,
            [Security.AccessControl.PropagationFlags]::None,
            [Security.AccessControl.AccessControlType]::Allow)
        [void]$security.AddAccessRule($rule)
    }
    if ($PSCmdlet.ShouldProcess(
            $Path,
            'Restrict runner-owned temporary directory access')) {
        [IO.FileSystemAclExtensions]::SetAccessControl(
            [IO.DirectoryInfo]::new($Path),
            $security)
    }
}

# Checks whether one exact generated Docker resource still exists.
function Test-DockerResourceExistence {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('container', 'network')]
        [string]$ResourceType,

        [Parameter(Mandatory)]
        [string]$ResourceName
    )

    $inspect = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            $ResourceType,
            'inspect',
            '--format',
            '{{.Id}}',
            $ResourceName
        ) `
        -TimeoutSeconds 30
    if ($inspect.ExitCode -eq 0) {
        if ($inspect.OutputLines.Count -ne 1 -or
            [string]::IsNullOrWhiteSpace($inspect.OutputLines[0])) {
            throw 'Docker returned an invalid resource identity.'
        }
        return $true
    }

    $listArguments = if ($ResourceType -ceq 'container') {
        @('container', 'ls', '--all', '--format', '{{.Names}}')
    }
    else {
        @('network', 'ls', '--format', '{{.Name}}')
    }
    $listing = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments $listArguments `
        -TimeoutSeconds 30
    if ($listing.ExitCode -ne 0) {
        throw 'Docker resource absence could not be corroborated.'
    }
    $exactMatches = @(
        $listing.OutputLines |
            Where-Object { $_ -ceq $ResourceName }
    )
    if ($exactMatches.Count -ne 0) {
        throw 'Docker inspect failed for a resource still present in the bounded listing.'
    }
    return $false
}

# Proves both labels before any generated Docker object is removed.
function Test-ExactDockerOwnership {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('container', 'network')]
        [string]$ResourceType,

        [Parameter(Mandatory)]
        [string]$ResourceName,

        [Parameter(Mandatory)]
        [ValidatePattern('^[0-9a-f]{32}$')]
        [string]$ExpectedRunId
    )

    $template = if ($ResourceType -eq 'container') {
        '{{index .Config.Labels "com.db-notifier.r-net-local"}}|{{index .Config.Labels "com.db-notifier.r-net-local.run"}}'
    }
    else {
        '{{index .Labels "com.db-notifier.r-net-local"}}|{{index .Labels "com.db-notifier.r-net-local.run"}}'
    }
    $inspection = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            $ResourceType,
            'inspect',
            '--format',
            $template,
            $ResourceName
        ) `
        -TimeoutSeconds 30
    return $inspection.ExitCode -eq 0 -and
        $inspection.OutputLines.Count -eq 1 -and
        $inspection.OutputLines[0] -ceq "true|$ExpectedRunId"
}

# Removes one exact generated Docker resource and records fail-closed residue.
function Remove-OwnedDockerResource {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('container', 'network')]
        [string]$ResourceType,

        [Parameter(Mandatory)]
        [string]$ResourceName,

        [ValidatePattern('^[0-9a-f]{32}$')]
        [string]$ExpectedRunId = $runId
    )

    try {
        if (-not (Test-DockerResourceExistence $ResourceType $ResourceName)) {
            return $true
        }
        if (-not (Test-ExactDockerOwnership `
                -ResourceType $ResourceType `
                -ResourceName $ResourceName `
                -ExpectedRunId $ExpectedRunId)) {
            return $false
        }

        if ($PSCmdlet.ShouldProcess($ResourceName, "Remove owned $ResourceType")) {
            $arguments = if ($ResourceType -ceq 'container') {
                @('container', 'rm', '--force', '--volumes', $ResourceName)
            }
            else {
                @('network', 'rm', $ResourceName)
            }
            $removal = Invoke-BoundedExternalProcess `
                -FilePath $docker `
                -Arguments $arguments `
                -TimeoutSeconds 90
            if ($removal.ExitCode -ne 0) {
                return $false
            }
        }
        return -not (Test-DockerResourceExistence $ResourceType $ResourceName)
    }
    catch {
        return $false
    }
}

# Removes only stale runner-owned Docker resources and CRL recovery fixtures before a new bounded campaign.
function Repair-StaleRNetResidue {
    $containerListing = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            'container',
            'ls',
            '--all',
            '--filter',
            "label=$ownershipLabel=true",
            '--format',
            '{{.Names}}|{{.Label "com.db-notifier.r-net-local.run"}}'
        ) `
        -TimeoutSeconds 30
    if ($containerListing.ExitCode -ne 0) {
        throw 'Stale R-NET container discovery failed closed.'
    }
    foreach ($line in $containerListing.OutputLines) {
        if ($line -notmatch
            '^db-notifier-r-net-(?:healthy|untrusted|missing-crl|revoked)-(?<run>[0-9a-f]{32})\|(?<label>[0-9a-f]{32})$' -or
            $Matches.run -cne $Matches.label) {
            throw 'A labelled R-NET container did not match the recoverable ownership shape.'
        }
        if (-not (Remove-OwnedDockerResource `
                -ResourceType container `
                -ResourceName ($line.Split('|', 2)[0]) `
                -ExpectedRunId $Matches.run)) {
            throw 'A stale runner-owned R-NET container could not be removed.'
        }
    }

    $networkListing = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            'network',
            'ls',
            '--filter',
            "label=$ownershipLabel=true",
            '--format',
            '{{.Name}}|{{.Label "com.db-notifier.r-net-local.run"}}'
        ) `
        -TimeoutSeconds 30
    if ($networkListing.ExitCode -ne 0) {
        throw 'Stale R-NET network discovery failed closed.'
    }
    foreach ($line in $networkListing.OutputLines) {
        if ($line -notmatch
            '^db-notifier-r-net-(?<run>[0-9a-f]{32})\|(?<label>[0-9a-f]{32})$' -or
            $Matches.run -cne $Matches.label) {
            throw 'A labelled R-NET network did not match the recoverable ownership shape.'
        }
        if (-not (Remove-OwnedDockerResource `
                -ResourceType network `
                -ResourceName ($line.Split('|', 2)[0]) `
                -ExpectedRunId $Matches.run)) {
            throw 'A stale runner-owned R-NET network could not be removed.'
        }
    }

    $volumeListing = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            'volume',
            'ls',
            '--filter',
            "label=$ownershipLabel=true",
            '--format',
            '{{.Name}}'
        ) `
        -TimeoutSeconds 30
    if ($volumeListing.ExitCode -ne 0 -or
        $volumeListing.OutputLines.Count -ne 0) {
        throw 'Unexpected labelled R-NET volume residue requires manual review.'
    }

    $staleRoots = @(
        Get-ChildItem `
            -LiteralPath $systemTemporaryRoot `
            -Directory `
            -Filter 'DBNotifier-R-Net-*' `
            -Force
    )
    foreach ($staleRoot in $staleRoots) {
        if ($staleRoot.Name -notmatch
            '^DBNotifier-R-Net-(?<run>[0-9a-f]{32})$' -or
            $staleRoot.Parent.FullName -cne $systemTemporaryRoot) {
            throw 'A stale R-NET temporary root did not match the recoverable ownership shape.'
        }

        $staleCertificate = Join-Path $staleRoot.FullName 'root-ca.crt'
        $staleCrl = Join-Path $staleRoot.FullName 'root-ca.crl'
        if ((Test-Path -LiteralPath $staleCertificate -PathType Leaf) -xor
            (Test-Path -LiteralPath $staleCrl -PathType Leaf)) {
            throw 'A stale R-NET PKI recovery pair was incomplete.'
        }
        if (Test-Path -LiteralPath $staleCertificate -PathType Leaf) {
            if (-not [DBNotifier.RNetFixture.ExactCurrentUserCrlRegistration]::TryRemoveOwnedRecovery(
                    $staleCertificate,
                    $staleCrl,
                    $Matches.run)) {
                throw 'The exact stale R-NET CRL recovery could not be proved.'
            }
        }

        Remove-Item -LiteralPath $staleRoot.FullName -Recurse -Force
        if (Test-Path -LiteralPath $staleRoot.FullName) {
            throw 'A recovered stale R-NET temporary root remained.'
        }
    }
}

# Creates one certificate-selected PostgreSQL cell on the restricted local bridge and returns its loopback port.
function Start-PostgreSqlCell {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('healthy', 'untrusted', 'missing-crl', 'revoked')]
        [string]$Scenario
    )

    $stem = switch ($Scenario) {
        'healthy' { 'postgres-server' }
        'untrusted' { 'untrusted' }
        'missing-crl' { 'missing-crl' }
        'revoked' { 'revoked' }
    }
    $containerName = "db-notifier-r-net-$Scenario-$runId"
    $certificatePath = Join-Path $temporaryRoot "$stem.crt"
    $privateKeyPath = Join-Path $temporaryRoot "$stem.key"
    $shellCommand = @(
        'set -eu'
        'while ip -4 route show default | grep -q .; do ip -4 route del default; done'
        'while ip -6 route show default | grep -q .; do ip -6 route del default; done'
        'mkdir -p /run/dbn-tls'
        'cp /dbn-fixture/server.crt /run/dbn-tls/server.crt'
        'cp /dbn-fixture/server.key /run/dbn-tls/server.key'
        'chown postgres:postgres /run/dbn-tls/server.crt /run/dbn-tls/server.key'
        'chmod 0644 /run/dbn-tls/server.crt'
        'chmod 0600 /run/dbn-tls/server.key'
        "exec /usr/local/bin/docker-entrypoint.sh postgres -c ssl=on -c ssl_min_protocol_version=TLSv1.2 -c ssl_cert_file=/run/dbn-tls/server.crt -c ssl_key_file=/run/dbn-tls/server.key -c listen_addresses='*' -c log_connections=off -c log_disconnections=off -c log_statement=none -c log_min_error_statement=panic -c log_parameter_max_length=0 -c log_parameter_max_length_on_error=0"
    ) -join '; '

    $arguments = @(
        'container', 'create',
        '--pull', 'never',
        '--name', $containerName,
        '--label', "$ownershipLabel=true",
        '--label', "$runLabel=$runId",
        '--network', $networkName,
        '--publish', '127.0.0.1::5432',
        '--dns', '127.0.0.1',
        '--cap-add', 'NET_ADMIN',
        '--security-opt', 'no-new-privileges=true',
        '--memory', '384m',
        '--memory-swap', '384m',
        '--cpus', '1.0',
        '--pids-limit', '128',
        '--restart', 'no',
        '--stop-timeout', '10',
        '--read-only',
        '--tmpfs', '/var/lib/postgresql/data:rw,noexec,nosuid,nodev,size=268435456',
        '--tmpfs', '/var/run/postgresql:rw,noexec,nosuid,nodev,size=16777216',
        '--tmpfs', '/run/dbn-tls:rw,noexec,nosuid,nodev,size=2097152',
        '--tmpfs', '/tmp:rw,noexec,nosuid,nodev,size=16777216',
        '--mount', "type=bind,src=$certificatePath,dst=/dbn-fixture/server.crt,readonly",
        '--mount', "type=bind,src=$privateKeyPath,dst=/dbn-fixture/server.key,readonly",
        '--mount', "type=bind,src=$administratorPasswordFile,dst=/run/secrets/dbn-administrator,readonly",
        '--mount', "type=bind,src=$monitorPasswordFile,dst=/run/secrets/dbn-monitor,readonly",
        '--mount', "type=bind,src=$initialisationFile,dst=/docker-entrypoint-initdb.d/001-rnet-monitor.sh,readonly",
        '--mount', "type=bind,src=$hostBasedAuthenticationFile,dst=/docker-entrypoint-initdb.d/002-rnet-hba.sh,readonly",
        '--mount', "type=bind,src=$resolverConfigurationFile,dst=/etc/resolv.conf,readonly",
        '--env', 'POSTGRES_USER=dbn_fixture_admin',
        '--env', 'POSTGRES_DB=dbn_fixture',
        '--env', 'POSTGRES_PASSWORD_FILE=/run/secrets/dbn-administrator',
        '--env', 'POSTGRES_INITDB_ARGS=--auth-host=scram-sha-256 --auth-local=trust',
        '--health-cmd', 'pg_isready -U dbn_fixture_admin -d dbn_fixture',
        '--health-interval', '1s',
        '--health-timeout', '3s',
        '--health-retries', '60',
        '--entrypoint', '/bin/sh',
        $imageReference,
        '-c', $shellCommand
    )
    if (-not $PSCmdlet.ShouldProcess(
            $containerName,
            "Create and start bounded $Scenario PostgreSQL cell")) {
        throw 'The PostgreSQL fixture start was declined.'
    }

    $script:currentContainer = $containerName
    [void](Invoke-DockerChecked $arguments)
    [void](Invoke-DockerChecked @('container', 'start', $containerName))

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(90)
    $health = $null
    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        $healthInspection = Invoke-BoundedExternalProcess `
            -FilePath $docker `
            -Arguments @(
                'container',
                'inspect',
                '--format',
                '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}',
                $containerName
            ) `
            -TimeoutSeconds 30
        if ($healthInspection.ExitCode -ne 0 -or
            $healthInspection.OutputLines.Count -ne 1) {
            throw 'The PostgreSQL fixture health could not be inspected.'
        }
        $health = $healthInspection.OutputLines[0]
        if ($health -ceq 'healthy') {
            break
        }
        if ($health -in @('unhealthy', 'exited', 'dead')) {
            throw 'The PostgreSQL fixture did not become healthy.'
        }
        Start-Sleep -Milliseconds 500
    }
    if ($health -cne 'healthy') {
        throw 'The PostgreSQL fixture exceeded its bounded readiness deadline.'
    }

    $publishedInspection = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            'container',
            'inspect',
            '--format',
            '{{json (index .NetworkSettings.Ports "5432/tcp")}}',
            $containerName
        ) `
        -TimeoutSeconds 30
    $publishedBindings = @()
    $publishedSchema = ''
    if ($publishedInspection.ExitCode -eq 0 -and
        $publishedInspection.OutputLines.Count -eq 1) {
        try {
            $publishedBindings = @(
                $publishedInspection.OutputLines[0] |
                    ConvertFrom-Json -Depth 3
            )
            if ($publishedBindings.Count -eq 1) {
                $publishedSchema = [string]::Join(
                    '|',
                    [string[]]@(
                        $publishedBindings[0].PSObject.Properties.Name |
                            Sort-Object
                    ))
            }
        }
        catch {
            $publishedBindings = @()
            $publishedSchema = ''
        }
    }
    if ($publishedBindings.Count -ne 1 -or
        $publishedSchema -cne 'HostIp|HostPort' -or
        $publishedBindings[0].HostIp -cne '127.0.0.1' -or
        $publishedBindings[0].HostPort -notmatch '^[0-9]{1,5}$') {
        throw 'The PostgreSQL fixture was not published exclusively on IPv4 loopback.'
    }
    $port = [int]$publishedBindings[0].HostPort
    if ($port -lt 1 -or $port -gt 65535) {
        throw 'The PostgreSQL fixture returned an invalid loopback port.'
    }
    $publishedPorts.Add($port)

    $listenerDeadline = [DateTimeOffset]::UtcNow.AddSeconds(10)
    $listeners = @()
    while ([DateTimeOffset]::UtcNow -lt $listenerDeadline) {
        $listeners = @(
            Get-NetTCPConnection `
                -State Listen `
                -LocalPort $port `
                -ErrorAction SilentlyContinue
        )
        if ($listeners.Count -ne 0) {
            break
        }
        Start-Sleep -Milliseconds 200
    }
    if ($listeners.Count -eq 0 -or
        @(
            $listeners |
                Where-Object { $_.LocalAddress -cne '127.0.0.1' }
        ).Count -ne 0) {
        throw 'The effective PostgreSQL listener was not exclusively bound to IPv4 loopback.'
    }

    $networkIsolation = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            'container',
            'exec',
            $containerName,
            '/bin/sh',
            '-c',
            'set -eu; test -z "$(ip -4 route show default)"; test -z "$(ip -6 route show default)"; ! ip -4 route get 192.0.2.1 >/dev/null 2>&1; ! ip -6 route get 2001:db8::1 >/dev/null 2>&1; test "$(sed -n ''1p'' /etc/resolv.conf)" = "nameserver 127.0.0.1"; test "$(sed -n ''2p'' /etc/resolv.conf)" = "options timeout:1 attempts:1"; test "$(wc -l < /etc/resolv.conf)" -eq 2; ! getent ahosts dbnotifier-rnet.invalid >/dev/null 2>&1; awk ''$1 == "Uid:" { if ($2 == "0") exit 1; uid = 1 } $1 ~ /^Cap(Eff|Prm|Inh|Amb):$/ { if ($2 !~ /^0+$/) exit 2; caps++ } $1 == "NoNewPrivs:" { if ($2 != "1") exit 3; nnp = 1 } END { if (!uid || caps != 4 || !nnp) exit 4 }'' /proc/1/status'
        ) `
        -TimeoutSeconds 30
    if ($networkIsolation.ExitCode -ne 0 -or
        $networkIsolation.OutputLines.Count -ne 0) {
        throw 'The PostgreSQL cell did not prove its route, DNS and effective-capability boundary.'
    }
    $dnsInspection = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            'container',
            'inspect',
            '--format',
            '{{json .HostConfig.Dns}}',
            $containerName
        ) `
        -TimeoutSeconds 30
    if ($dnsInspection.ExitCode -ne 0 -or
        $dnsInspection.OutputLines.Count -ne 1 -or
        $dnsInspection.OutputLines[0] -cne '["127.0.0.1"]') {
        throw 'The PostgreSQL cell did not retain the loopback-only DNS setting.'
    }

    $mountInspection = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            'container',
            'inspect',
            '--format',
            '{{range .Mounts}}{{if eq .Type "volume"}}{{.Name}}{{end}}{{end}}',
            $containerName
        ) `
        -TimeoutSeconds 30
    if ($mountInspection.ExitCode -ne 0 -or
        $mountInspection.OutputLines.Count -ne 0) {
        throw 'The PostgreSQL fixture unexpectedly allocated a Docker volume.'
    }
    $expectedCertificateHash = (
        Get-FileHash -LiteralPath $certificatePath -Algorithm SHA256
    ).Hash.ToLowerInvariant()
    $certificateInspection = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            'container',
            'exec',
            $containerName,
            'sha256sum',
            '/run/dbn-tls/server.crt'
        ) `
        -TimeoutSeconds 30
    if ($certificateInspection.ExitCode -ne 0 -or
        $certificateInspection.OutputLines.Count -ne 1 -or
        $certificateInspection.OutputLines[0] -notmatch
            '^(?<digest>[0-9a-f]{64})  /run/dbn-tls/server\.crt$' -or
        $Matches.digest -cne $expectedCertificateHash) {
        throw 'The PostgreSQL cell did not present the exact scenario certificate bytes.'
    }

    return [pscustomobject]@{
        ContainerName = $containerName
        Port = $port
    }
}

# Validates one closed TRX document against the exact filter-owned names and all-pass counters.
function Test-ExactTrxEvidence {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [ValidateRange(1, 20)]
        [int]$ExpectedCount,

        [Parameter(Mandatory)]
        [string[]]$ExpectedTestNames,

        [Parameter(Mandatory)]
        [string[]]$SecretValues
    )

    try {
        $canonicalPath = [IO.Path]::GetFullPath($Path)
        if (-not [IO.Path]::IsPathFullyQualified($Path) -or
            (Split-Path -Parent $canonicalPath) -cne $testResultsRoot -or
            $ExpectedTestNames.Count -ne $ExpectedCount) {
            return $false
        }
        $information = [IO.FileInfo]::new($canonicalPath)
        if (-not $information.Exists -or
            $information.Length -lt 1 -or
            $information.Length -gt 2097152) {
            return $false
        }

        $settings = [Xml.XmlReaderSettings]::new()
        $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
        $settings.XmlResolver = $null
        $settings.MaxCharactersInDocument = 2097152
        $document = [Xml.XmlDocument]::new()
        $document.XmlResolver = $null
        $stream = [IO.File]::Open(
            $canonicalPath,
            [IO.FileMode]::Open,
            [IO.FileAccess]::Read,
            [IO.FileShare]::Read)
        try {
            $reader = [Xml.XmlReader]::Create($stream, $settings)
            try {
                $document.Load($reader)
            }
            finally {
                $reader.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }

        $serialisedEvidence = $document.OuterXml
        foreach ($secretValue in $SecretValues) {
            if (-not [string]::IsNullOrEmpty($secretValue) -and
                $serialisedEvidence.Contains(
                    $secretValue,
                    [StringComparison]::Ordinal)) {
                return $false
            }
        }
        if ($serialisedEvidence -match ('Pass' + 'word\s*=[^;\s]+') -or
            $serialisedEvidence -match '(?i)BEGIN [A-Z0-9 ]*PRIVATE KEY' -or
            $serialisedEvidence -match '(?i)Authorization\s*:\s*Bearer' -or
            $serialisedEvidence -match 'eyJ[A-Za-z0-9_-]{7,}\.eyJ[A-Za-z0-9_-]{7,}\.[A-Za-z0-9_-]{10,}') {
            return $false
        }

        $counters = $document.SelectSingleNode(
            "/*[local-name()='TestRun']/*[local-name()='ResultSummary']/*[local-name()='Counters']")
        if ($null -eq $counters) {
            return $false
        }
        $expectedCounters = [ordered]@{
            total = $ExpectedCount
            executed = $ExpectedCount
            passed = $ExpectedCount
            failed = 0
            notExecuted = 0
        }
        foreach ($counter in $expectedCounters.GetEnumerator()) {
            $value = 0
            if (-not [int]::TryParse(
                    $counters.GetAttribute($counter.Key),
                    [Globalization.NumberStyles]::None,
                    [Globalization.CultureInfo]::InvariantCulture,
                    [ref]$value) -or
                $value -ne $counter.Value) {
                return $false
            }
        }

        $results = @(
            $document.SelectNodes(
                "/*[local-name()='TestRun']/*[local-name()='Results']/*[local-name()='UnitTestResult']")
        )
        if ($results.Count -ne $ExpectedCount -or
            @($results | Where-Object { $_.GetAttribute('outcome') -cne 'Passed' }).Count -ne 0) {
            return $false
        }
        $actualNames = @(
            $results |
                ForEach-Object { $_.GetAttribute('testName') } |
                Sort-Object
        )
        $expectedNames = @($ExpectedTestNames | Sort-Object)
        return @(
            Compare-Object `
                -ReferenceObject $expectedNames `
                -DifferenceObject $actualNames `
                -CaseSensitive
        ).Count -eq 0
    }
    catch {
        return $false
    }
}

# Executes one marker-gated integration-test filter under a bounded child process and suppresses credential-bearing output.
function Invoke-BoundedIntegrationTest {
    param(
        [Parameter(Mandatory)]
        [string]$Filter,

        [Parameter(Mandatory)]
        [ValidateSet(
            'contracts',
            'postgres-healthy',
            'postgres-untrusted',
            'postgres-missing-crl',
            'postgres-revoked')]
        [string]$InvocationName,

        [Parameter(Mandatory)]
        [ValidateRange(1, 20)]
        [int]$ExpectedCount,

        [Parameter(Mandatory)]
        [string[]]$ExpectedTestNames,

        [Parameter(Mandatory)]
        [int]$PostgreSqlPort,

        [Parameter(Mandatory)]
        [string[]]$SecretValues,

        [ValidateRange(30, 300)]
        [int]$TimeoutSeconds = 180
    )

    $expectedFilter = if ($InvocationName -ceq 'contracts') {
        'Category=RNetLocalHomologation'
    }
    else {
        ($ExpectedTestNames |
            ForEach-Object { "FullyQualifiedName=$_" }) -join '|'
    }
    if ($ExpectedCount -ne $ExpectedTestNames.Count -or
        $Filter -cne $expectedFilter) {
        throw 'The bounded R-NET test filter did not match its closed manifest.'
    }
    $trxPath = Join-Path $testResultsRoot "$InvocationName.trx"
    if (Test-Path -LiteralPath $trxPath) {
        throw 'The bounded R-NET TRX destination unexpectedly existed.'
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $resolvedDotNet
    $startInfo.WorkingDirectory = $root
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in @(
            'test',
            $integrationProject,
            '--configuration',
            'Release',
            '--no-build',
            '--no-restore',
            '--disable-build-servers',
            '--filter',
            $Filter,
            '--logger',
            'console;verbosity=minimal',
            '--logger',
            "trx;LogFileName=$InvocationName.trx",
            '--results-directory',
            $testResultsRoot
        )) {
        $startInfo.ArgumentList.Add($argument)
    }
    $startInfo.Environment['DBNOTIFIER_RNET_LOCAL_HOMOLOGATION'] = 'local-test'
    $startInfo.Environment['DBNOTIFIER_RNET_FIXTURE_ROOT'] = $temporaryRoot
    $startInfo.Environment['DBNOTIFIER_RNET_POSTGRESQL_FIXTURE_ROOT'] = $temporaryRoot
    $startInfo.Environment['DBNOTIFIER_RNET_POSTGRESQL_SECRET_FILE'] = $secretFile
    $startInfo.Environment['DBNOTIFIER_RNET_POSTGRESQL_HOST'] = $fixtureHost
    $startInfo.Environment['DBNOTIFIER_RNET_POSTGRESQL_PORT'] =
        $PostgreSqlPort.ToString([Globalization.CultureInfo]::InvariantCulture)
    $startInfo.Environment['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    $startInfo.Environment['DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE'] = '1'

    $script:testProcess = [System.Diagnostics.Process]::new()
    $script:testProcess.StartInfo = $startInfo
    $started = $false
    try {
        if (-not $script:testProcess.Start()) {
            throw 'The bounded R-NET integration test could not be started.'
        }
        $started = $true
        $testProcessIdentities.Add(
            [pscustomobject]@{
                ProcessId = $script:testProcess.Id
                StartTimeUtc = $script:testProcess.StartTime.ToUniversalTime()
            })
        $standardOutputTask = $script:testProcess.StandardOutput.ReadToEndAsync()
        $standardErrorTask = $script:testProcess.StandardError.ReadToEndAsync()
        $timedOut = -not $script:testProcess.WaitForExit($TimeoutSeconds * 1000)
        if ($timedOut) {
            $script:testProcess.Kill($true)
            if (-not $script:testProcess.WaitForExit(10000)) {
                throw 'The bounded R-NET test process exit could not be proved.'
            }
        }

        $standardOutput = $standardOutputTask.GetAwaiter().GetResult()
        $standardError = $standardErrorTask.GetAwaiter().GetResult()
        if ($standardOutput.Length -gt 2097152 -or
            $standardError.Length -gt 2097152) {
            throw 'The bounded R-NET test output exceeded its retained limit.'
        }
        $rawCombined = $standardOutput + [Environment]::NewLine + $standardError
        foreach ($secretValue in $SecretValues) {
            if (-not [string]::IsNullOrEmpty($secretValue) -and
                $rawCombined.Contains($secretValue, [StringComparison]::Ordinal)) {
                throw 'The bounded R-NET test output contained synthetic credential material.'
            }
        }
        $sanitisedOutput = $standardOutput.Replace(
            $testResultsRoot,
            '[r-net-results]',
            [StringComparison]::OrdinalIgnoreCase)
        $sanitisedError = $standardError.Replace(
            $testResultsRoot,
            '[r-net-results]',
            [StringComparison]::OrdinalIgnoreCase)
        $combined = $sanitisedOutput + [Environment]::NewLine + $sanitisedError
        $passwordPattern = ('Pass' + 'word\s*=[^;\s]+')
        if ($combined -match $passwordPattern -or
            $combined -match '(?i)BEGIN [A-Z0-9 ]*PRIVATE KEY' -or
            $combined -match '(?i)Authorization\s*:\s*Bearer' -or
            $combined -match 'eyJ[A-Za-z0-9_-]{7,}\.eyJ[A-Za-z0-9_-]{7,}\.[A-Za-z0-9_-]{10,}' -or
            $combined.Contains('.key', [StringComparison]::OrdinalIgnoreCase) -or
            $combined.Contains($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'The bounded R-NET test output contained prohibited private material.'
        }
        if ($timedOut) {
            throw 'The bounded R-NET integration test exceeded its total watchdog.'
        }
        if ($script:testProcess.ExitCode -ne 0) {
            Write-Output $sanitisedOutput
            Write-Output $sanitisedError
            throw 'A bounded R-NET integration-test filter failed.'
        }
        if (-not (Test-ExactTrxEvidence `
                -Path $trxPath `
                -ExpectedCount $ExpectedCount `
                -ExpectedTestNames $ExpectedTestNames `
                -SecretValues $SecretValues)) {
            Write-Output $sanitisedOutput
            Write-Output $sanitisedError
            throw 'The bounded R-NET TRX evidence did not match its closed manifest.'
        }

        Write-Output $sanitisedOutput
        if (-not [string]::IsNullOrWhiteSpace($sanitisedError)) {
            Write-Output $sanitisedError
        }
        $script:testInvocationCount++
        $script:testCaseCount += $ExpectedCount
    }
    finally {
        if ($null -ne $script:testProcess -and
            (-not $started -or $script:testProcess.HasExited)) {
            $script:testProcess.Dispose()
            $script:testProcess = $null
        }
    }
}

try {
    try {
        $runnerLock = [IO.FileStream]::new(
            $runnerLockPath,
            [IO.FileMode]::OpenOrCreate,
            [IO.FileAccess]::ReadWrite,
            [IO.FileShare]::None,
            1,
            [IO.FileOptions]::DeleteOnClose)
    }
    catch {
        throw 'Another R-NET campaign or residue recovery currently owns the runner lock.'
    }

    Add-Type -TypeDefinition $fixtureSource -Language CSharp
    Repair-StaleRNetResidue

    $imageInspection = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            'image',
            'inspect',
            '--format',
            '{{json .RepoDigests}}',
            $imageReference
        ) `
        -TimeoutSeconds 30
    $repoDigests = @()
    if ($imageInspection.ExitCode -eq 0 -and
        $imageInspection.OutputLines.Count -eq 1) {
        try {
            $repoDigests = @(
                $imageInspection.OutputLines[0] |
                    ConvertFrom-Json -Depth 2
            )
        }
        catch {
            $repoDigests = @()
        }
    }
    if ($repoDigests.Count -lt 1 -or
        @($repoDigests | Where-Object { $_ -ceq $imageReference }).Count -ne 1) {
        throw 'The exact pinned PostgreSQL image is unavailable locally.'
    }

    $build = Invoke-BoundedExternalProcess `
        -FilePath $resolvedDotNet `
        -Arguments @(
            'build',
            $integrationProject,
            '--configuration',
            'Release',
            '--no-restore',
            '--disable-build-servers',
            '-nodeReuse:false',
            '-p:UseSharedCompilation=false',
            '--nologo'
        ) `
        -TimeoutSeconds 180
    if ($build.ExitCode -ne 0) {
        throw 'The R-NET integration test project did not build.'
    }
    Write-Output 'R-NET integration test project build passed.'

    if (Test-Path -LiteralPath $temporaryRoot) {
        throw 'The generated R-NET temporary root unexpectedly exists.'
    }
    [void](New-Item -ItemType Directory -Path $temporaryRoot)
    if ((Resolve-Path -LiteralPath $temporaryRoot).Path -cne $expectedTemporaryRoot) {
        throw 'The generated R-NET temporary root was not exact.'
    }
    Set-PrivateTemporaryDirectoryAccess -Path $temporaryRoot
    [void](New-Item -ItemType Directory -Path $testResultsRoot)
    if ((Resolve-Path -LiteralPath $testResultsRoot).Path -cne $testResultsRoot) {
        throw 'The generated R-NET test-results root was not exact.'
    }

    [DBNotifier.RNetFixture.CertificateFixtureGenerator]::Generate(
        $temporaryRoot,
        $runId)
    [IO.File]::WriteAllText(
        $resolverConfigurationFile,
        "nameserver 127.0.0.1`noptions timeout:1 attempts:1`n",
        [Text.UTF8Encoding]::new($false))

    $administratorPassword = Get-SyntheticCredential
    $monitorPassword = Get-SyntheticCredential
    if ($administratorPassword -ceq $monitorPassword) {
        throw 'Synthetic fixture credentials unexpectedly matched.'
    }
    $secretDocument = [ordered]@{
        administratorPassword = $administratorPassword
        administratorUser = 'dbn_fixture_admin'
        database = 'dbn_fixture'
        monitorPassword = $monitorPassword
        monitorUser = 'dbn_fixture_monitor'
    } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText(
        $secretFile,
        $secretDocument,
        [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText(
        $administratorPasswordFile,
        $administratorPassword,
        [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText(
        $monitorPasswordFile,
        $monitorPassword,
        [System.Text.UTF8Encoding]::new($false))
    $monitorRoleInitialisationScript = @'
#!/bin/sh
# Module purpose: Creates the synthetic monitoring role without exposing its temporary credential through arguments, environment or diagnostics.
set -eu
set +x
if ! monitor_credential="$(cat /run/secrets/dbn-monitor 2>/dev/null)"; then
    echo 'Synthetic monitoring credential input was unavailable.' >&2
    exit 1
fi
case "$monitor_credential" in
    ''|*[!A-Za-z0-9]*)
        unset monitor_credential
        echo 'Synthetic monitoring credential input was invalid.' >&2
        exit 1
        ;;
esac
if [ "${#monitor_credential}" -lt 32 ] || [ "${#monitor_credential}" -gt 128 ]; then
    unset monitor_credential
    echo 'Synthetic monitoring credential input was invalid.' >&2
    exit 1
fi
if ! psql \
    --username "$POSTGRES_USER" \
    --dbname "$POSTGRES_DB" \
    --set ON_ERROR_STOP=1 \
    >/dev/null 2>&1 <<DBNOTIFIER_SQL
CREATE ROLE dbn_fixture_monitor LOGIN PASSWORD '$monitor_credential';
GRANT CONNECT ON DATABASE dbn_fixture TO dbn_fixture_monitor;
DBNOTIFIER_SQL
then
    unset monitor_credential
    echo 'Synthetic monitoring role initialisation failed.' >&2
    exit 1
fi
unset monitor_credential
'@
    [System.IO.File]::WriteAllText(
        $initialisationFile,
        $monitorRoleInitialisationScript.Replace("`r`n", "`n"),
        [System.Text.UTF8Encoding]::new($false))
    $hostBasedAuthenticationScript = @'
#!/bin/sh
# Module purpose: Restricts the disposable PostgreSQL fixture to local-socket init and TLS-only host authentication.
set -eu
cat > "$PGDATA/pg_hba.conf" <<'DBNOTIFIER_HBA'
local all all trust
hostssl all all 0.0.0.0/0 scram-sha-256
hostssl all all ::/0 scram-sha-256
hostnossl all all 0.0.0.0/0 reject
hostnossl all all ::/0 reject
DBNOTIFIER_HBA
chmod 0600 "$PGDATA/pg_hba.conf"
'@
    [System.IO.File]::WriteAllText(
        $hostBasedAuthenticationFile,
        $hostBasedAuthenticationScript.Replace("`r`n", "`n"),
        [System.Text.UTF8Encoding]::new($false))
    $secretDocument = $null
    $monitorRoleInitialisationScript = $null
    $hostBasedAuthenticationScript = $null

    $encodedCrl = [System.IO.File]::ReadAllBytes(
        (Join-Path $temporaryRoot 'root-ca.crl'))
    try {
        $crlRegistration =
            [DBNotifier.RNetFixture.ExactCurrentUserCrlRegistration]::Add(
                $encodedCrl)
        if (-not $crlRegistration.IsInstalledAndProved) {
            throw 'The exact current-user CRL installation could not be proved.'
        }
    }
    finally {
        [System.Security.Cryptography.CryptographicOperations]::ZeroMemory(
            $encodedCrl)
    }

    $networkProvisioningAttempted = $true
    [void](Invoke-DockerChecked @(
            'network', 'create',
            '--driver', 'bridge',
            '--opt', 'com.docker.network.bridge.enable_ip_masquerade=false',
            '--opt', 'com.docker.network.bridge.enable_icc=false',
            '--opt', 'com.docker.network.bridge.host_binding_ipv4=127.0.0.1',
            '--label', "$ownershipLabel=true",
            '--label', "$runLabel=$runId",
            $networkName
        ))
    $networkSecurityInspection = Invoke-BoundedExternalProcess `
        -FilePath $docker `
        -Arguments @(
            'network',
            'inspect',
            '--format',
            '{{.Internal}}|{{index .Options "com.docker.network.bridge.enable_ip_masquerade"}}|{{index .Options "com.docker.network.bridge.enable_icc"}}|{{index .Options "com.docker.network.bridge.host_binding_ipv4"}}',
            $networkName
        ) `
        -TimeoutSeconds 30
    if ($networkSecurityInspection.ExitCode -ne 0 -or
        $networkSecurityInspection.OutputLines.Count -ne 1 -or
        $networkSecurityInspection.OutputLines[0] -cne
            'false|false|false|127.0.0.1') {
        throw 'The R-NET bridge did not preserve the closed local network options.'
    }

    $contractTestNames = @(
        'DBNotifier.IntegrationTests.RNetLocalHomologationTests.LocalDnsProvesResolverAdmissionMixedAnswersAndRebindingResistance'
        'DBNotifier.IntegrationTests.RNetLocalHomologationTests.LocalPkiProvesOfflineTrustRevocationNameAndUsageBoundaries'
        'DBNotifier.IntegrationTests.RNetLocalHomologationTests.LocalHttpsIdentityProviderProvesDiscoveryJwksAndJwtBoundaries'
    )
    Invoke-BoundedIntegrationTest `
        -Filter 'Category=RNetLocalHomologation' `
        -InvocationName 'contracts' `
        -ExpectedCount 3 `
        -ExpectedTestNames $contractTestNames `
        -PostgreSqlPort 1 `
        -SecretValues @($administratorPassword, $monitorPassword)

    $healthy = Start-PostgreSqlCell -Scenario 'healthy'
    $healthyTestNames = @(
        'DBNotifier.IntegrationTests.RNetPostgreSqlTlsHomologationTests.CentralFactoryUsesVerifyFullAndPgStatSsl'
        'DBNotifier.IntegrationTests.RNetPostgreSqlTlsHomologationTests.ProviderCredentialIsHealthyOverVerifiedTls'
        'DBNotifier.IntegrationTests.RNetPostgreSqlTlsHomologationTests.WrongPasswordIsAuthenticationFailed'
        'DBNotifier.IntegrationTests.RNetPostgreSqlTlsHomologationTests.WrongHostnameIsRefusedByVerifyFull'
        'DBNotifier.IntegrationTests.RNetPostgreSqlTlsHomologationTests.PolicyDenialAvoidsSecretMaterialisationAndLeavesSampledSessionCountZero'
    )
    Invoke-BoundedIntegrationTest `
        -Filter (($healthyTestNames |
                ForEach-Object { "FullyQualifiedName=$_" }) -join '|') `
        -InvocationName 'postgres-healthy' `
        -ExpectedCount 5 `
        -ExpectedTestNames $healthyTestNames `
        -PostgreSqlPort $healthy.Port `
        -SecretValues @(
            $administratorPassword,
            $monitorPassword,
            "$monitorPassword-mismatch"
        )
    if (Remove-OwnedDockerResource `
            -ResourceType container `
            -ResourceName $healthy.ContainerName) {
        $currentContainer = $null
    }
    else {
        throw 'The healthy PostgreSQL cell could not be removed before the next cell.'
    }

    foreach ($scenario in @('untrusted', 'missing-crl', 'revoked')) {
        $cell = Start-PostgreSqlCell -Scenario $scenario
        $method = switch ($scenario) {
            'untrusted' { 'UntrustedCertificateIsRefusedByOfflineTls' }
            'missing-crl' { 'MissingCrlCertificateIsRefusedByOfflineTls' }
            'revoked' { 'RevokedCertificateIsRefusedByOfflineTls' }
        }
        $invocationName = switch ($scenario) {
            'untrusted' { 'postgres-untrusted' }
            'missing-crl' { 'postgres-missing-crl' }
            'revoked' { 'postgres-revoked' }
        }
        $testName =
            "DBNotifier.IntegrationTests.RNetPostgreSqlTlsHomologationTests.$method"
        Invoke-BoundedIntegrationTest `
            -Filter "FullyQualifiedName=$testName" `
            -InvocationName $invocationName `
            -ExpectedCount 1 `
            -ExpectedTestNames @($testName) `
            -PostgreSqlPort $cell.Port `
            -SecretValues @($administratorPassword, $monitorPassword)
        if (Remove-OwnedDockerResource `
                -ResourceType container `
                -ResourceName $cell.ContainerName) {
            $currentContainer = $null
        }
        else {
            throw 'A negative PostgreSQL cell could not be removed before the next cell.'
        }
    }

    $successSummary = (
        "R-NET local homologation passed $testCaseCount test cases across " +
        "$testInvocationCount bounded invocations; PostgreSQL cells were sequential.")
}
finally {
    if ($null -ne $testProcess) {
        try {
            if (-not $testProcess.HasExited) {
                $testProcess.Kill($true)
                if (-not $testProcess.WaitForExit(10000)) {
                    $cleanupFailures.Add(
                        'The exact owned integration-test process did not exit.')
                }
            }
        }
        catch {
            $cleanupFailures.Add(
                'The exact owned integration-test process could not be stopped.')
        }
        finally {
            $testProcess.Dispose()
            $testProcess = $null
        }
    }

    if ($null -ne $currentContainer) {
        for ($attempt = 1; $attempt -le 3 -and $null -ne $currentContainer; $attempt++) {
            if (Remove-OwnedDockerResource `
                    -ResourceType container `
                    -ResourceName $currentContainer) {
                $currentContainer = $null
            }
        }
        if ($null -ne $currentContainer) {
            $cleanupFailures.Add(
                'The exact owned R-NET container could not be removed.')
        }
    }
    if ($networkProvisioningAttempted) {
        $networkRemoved = $false
        for ($attempt = 1; $attempt -le 3 -and -not $networkRemoved; $attempt++) {
            $networkRemoved = Remove-OwnedDockerResource `
                -ResourceType network `
                -ResourceName $networkName
        }
        if (-not $networkRemoved) {
            $cleanupFailures.Add(
                'The exact owned R-NET network could not be removed.')
        }
    }

    if ($null -ne $crlRegistration) {
        $crlRemoved = $false
        try {
            $crlRemoved = $crlRegistration.RemoveAndVerify()
        }
        catch {
            $crlRemoved = $false
        }
        if ($crlRemoved) {
            $crlRegistration = $null
        }
        else {
            $preserveRecoveryRoot = $true
            $cleanupFailures.Add(
                'The exact current-user CRL removal could not be proved.')
        }
    }

    try {
        $containerAudit = Invoke-BoundedExternalProcess `
            -FilePath $docker `
            -Arguments @(
                'container',
                'ls',
                '--all',
                '--filter',
                "label=$ownershipLabel=true",
                '--filter',
                "label=$runLabel=$runId",
                '--format',
                '{{.ID}}'
            ) `
            -TimeoutSeconds 30
        $containerAuditFailed =
            $containerAudit.ExitCode -ne 0 -or
            $containerAudit.OutputLines.Count -ne 0
    }
    catch {
        $containerAuditFailed = $true
    }
    if ($containerAuditFailed) {
        $cleanupFailures.Add(
            'A labelled R-NET container remained after final cleanup.')
    }
    try {
        $networkAudit = Invoke-BoundedExternalProcess `
            -FilePath $docker `
            -Arguments @(
                'network',
                'ls',
                '--filter',
                "label=$ownershipLabel=true",
                '--filter',
                "label=$runLabel=$runId",
                '--format',
                '{{.ID}}'
            ) `
            -TimeoutSeconds 30
        $networkAuditFailed =
            $networkAudit.ExitCode -ne 0 -or
            $networkAudit.OutputLines.Count -ne 0
    }
    catch {
        $networkAuditFailed = $true
    }
    if ($networkAuditFailed) {
        $cleanupFailures.Add(
            'A labelled R-NET network remained after final cleanup.')
    }
    try {
        $volumeAudit = Invoke-BoundedExternalProcess `
            -FilePath $docker `
            -Arguments @(
                'volume',
                'ls',
                '--filter',
                "label=$ownershipLabel=true",
                '--filter',
                "label=$runLabel=$runId",
                '--format',
                '{{.Name}}'
            ) `
            -TimeoutSeconds 30
        $volumeAuditFailed =
            $volumeAudit.ExitCode -ne 0 -or
            $volumeAudit.OutputLines.Count -ne 0
    }
    catch {
        $volumeAuditFailed = $true
    }
    if ($volumeAuditFailed) {
        $cleanupFailures.Add(
            'A labelled R-NET volume remained after final cleanup.')
    }
    foreach ($publishedPort in $publishedPorts) {
        if (@(
                Get-NetTCPConnection `
                    -State Listen `
                    -LocalPort $publishedPort `
                    -ErrorAction SilentlyContinue
            ).Count -ne 0) {
            $cleanupFailures.Add(
                'A previously published R-NET loopback listener remained.')
        }
    }
    foreach ($identity in $testProcessIdentities) {
        $residualProcess = Get-Process `
            -Id $identity.ProcessId `
            -ErrorAction SilentlyContinue
        if ($null -ne $residualProcess) {
            try {
                if ($residualProcess.StartTime.ToUniversalTime() -eq
                    $identity.StartTimeUtc) {
                    $cleanupFailures.Add(
                        'An exact owned R-NET test process remained.')
                }
            }
            finally {
                $residualProcess.Dispose()
            }
        }
    }
    foreach ($identity in $externalProcessIdentities) {
        $residualProcess = Get-Process `
            -Id $identity.ProcessId `
            -ErrorAction SilentlyContinue
        if ($null -ne $residualProcess) {
            try {
                if ($residualProcess.StartTime.ToUniversalTime() -eq
                    $identity.StartTimeUtc) {
                    $cleanupFailures.Add(
                        'An exact owned bounded external process remained.')
                }
            }
            finally {
                $residualProcess.Dispose()
            }
        }
    }

    $administratorPassword = $null
    $monitorPassword = $null
    if ((Test-Path -LiteralPath $temporaryRoot) -and
        -not $preserveRecoveryRoot) {
        $resolvedTemporaryRoot = (Resolve-Path -LiteralPath $temporaryRoot).Path
        if ($resolvedTemporaryRoot -cne $expectedTemporaryRoot -or
            -not $resolvedTemporaryRoot.StartsWith(
                "$systemTemporaryRoot$([IO.Path]::DirectorySeparatorChar)",
                [StringComparison]::OrdinalIgnoreCase)) {
            $cleanupFailures.Add(
                'The exact R-NET temporary root could not be proved for cleanup.')
        }
        else {
            try {
                Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
            }
            catch {
                $cleanupFailures.Add(
                    'The exact R-NET temporary root could not be removed.')
            }
        }
    }
    if ((Test-Path -LiteralPath $temporaryRoot) -and
        -not $preserveRecoveryRoot) {
        $cleanupFailures.Add(
            'The exact R-NET temporary root remained after cleanup.')
    }

    if ($null -ne $runnerLock) {
        try {
            $runnerLock.Dispose()
            $runnerLock = $null
        }
        catch {
            $cleanupFailures.Add(
                'The exclusive R-NET runner lock could not be released.')
        }
    }

    if ($cleanupFailures.Count -ne 0) {
        throw (
            'R-NET cleanup failed with ' +
            $cleanupFailures.Count.ToString(
                [Globalization.CultureInfo]::InvariantCulture) +
            ' bounded finding(s).')
    }
}

if (-not [string]::IsNullOrWhiteSpace($successSummary)) {
    Write-Output $successSummary
}
