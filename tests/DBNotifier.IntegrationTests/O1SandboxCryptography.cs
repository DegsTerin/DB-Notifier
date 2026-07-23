// Module purpose: Provides canonical hashing and in-memory synthetic signatures for O1 trust and corpus fixtures without persisting private material.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DBNotifier.IntegrationTests;

/// <summary>Provides deterministic canonical encoding, hashing and signature verification for O1 sandbox artefacts.</summary>
internal static class O1CanonicalCryptography
{
    private static readonly JsonSerializerOptions CanonicalOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>Encodes one immutable contract using its declared property and sorted collection order.</summary>
    /// <typeparam name="T">Contract type to encode.</typeparam>
    /// <param name="value">Validated immutable value.</param>
    /// <returns>Canonical UTF-8 JSON bytes.</returns>
    internal static byte[] Encode<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, CanonicalOptions);

    /// <summary>Hashes one immutable contract with SHA-256.</summary>
    /// <typeparam name="T">Contract type to hash.</typeparam>
    /// <param name="value">Validated immutable value.</param>
    /// <returns>Upper-case hexadecimal SHA-256 digest.</returns>
    internal static string Digest<T>(T value) =>
        Convert.ToHexString(SHA256.HashData(Encode(value)));

    /// <summary>Verifies one base64 ECDSA P-256 signature against independently supplied public material.</summary>
    /// <typeparam name="T">Signed contract type.</typeparam>
    /// <param name="value">Exact signed value.</param>
    /// <param name="signature">Base64 signature.</param>
    /// <param name="publicKey">Base64 SubjectPublicKeyInfo.</param>
    /// <returns><see langword="true"/> only for a valid exact signature.</returns>
    internal static bool Verify<T>(T value, string signature, string publicKey)
    {
        try
        {
            using ECDsa verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out int consumed);
            byte[] keyBytes = Convert.FromBase64String(publicKey);
            return consumed == keyBytes.Length &&
                verifier.VerifyData(
                    Encode(value),
                    Convert.FromBase64String(signature),
                    HashAlgorithmName.SHA256);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            FormatException or
            CryptographicException)
        {
            return false;
        }
    }

    /// <summary>Creates a stable digest for a public key without exposing its encoded material in diagnostics.</summary>
    /// <param name="publicKey">Base64 SubjectPublicKeyInfo.</param>
    /// <returns>Upper-case SHA-256 key-material digest.</returns>
    internal static string PublicKeyDigest(string publicKey) =>
        Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(publicKey)));

    /// <summary>Returns a stable non-secret identifier digest for one bounded string.</summary>
    /// <param name="value">Bounded value to hash.</param>
    /// <returns>Upper-case SHA-256 digest.</returns>
    internal static string TextDigest(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

/// <summary>Owns ephemeral ECDSA keys used only to construct synthetic O1 fixtures in memory.</summary>
internal sealed class O1SyntheticKeyRing : IDisposable
{
    private readonly Dictionary<O1TrustRole, ECDsa> privateKeys = [];
    private readonly string ringId;

    /// <summary>Creates materially distinct P-256 keys for every declared synthetic role.</summary>
    /// <param name="ringId">Stable non-secret fixture namespace.</param>
    internal O1SyntheticKeyRing(string ringId = "primary")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ringId);
        this.ringId = ringId;
        foreach (O1TrustRole role in Enum.GetValues<O1TrustRole>())
        {
            privateKeys.Add(role, ECDsa.Create(ECCurve.NamedCurves.nistP256));
        }
    }

    /// <summary>Gets a non-secret stable fixture key identifier for one role.</summary>
    /// <param name="role">Exact role.</param>
    /// <returns>Role-scoped key identifier.</returns>
    internal string KeyId(O1TrustRole role) =>
        $"o1-synthetic-{ringId}-{role.ToString().ToLowerInvariant()}";

    /// <summary>Exports only the public SubjectPublicKeyInfo for one synthetic role.</summary>
    /// <param name="role">Exact role.</param>
    /// <returns>Base64 public key material.</returns>
    internal string PublicKey(O1TrustRole role) =>
        Convert.ToBase64String(privateKeys[role].ExportSubjectPublicKeyInfo());

    /// <summary>Signs one exact immutable contract with the selected synthetic role.</summary>
    /// <typeparam name="T">Contract type.</typeparam>
    /// <param name="role">Signing role.</param>
    /// <param name="value">Exact contract.</param>
    /// <returns>Base64 ECDSA signature.</returns>
    internal string Sign<T>(O1TrustRole role, T value) =>
        Convert.ToBase64String(
            privateKeys[role].SignData(
                O1CanonicalCryptography.Encode(value),
                HashAlgorithmName.SHA256));

    /// <summary>Disposes every ephemeral private key without writing it to disk.</summary>
    public void Dispose()
    {
        foreach (ECDsa key in privateKeys.Values)
        {
            key.Dispose();
        }
        privateKeys.Clear();
    }
}
