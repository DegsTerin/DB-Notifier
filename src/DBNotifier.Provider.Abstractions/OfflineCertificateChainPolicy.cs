// Module purpose: Creates fail-closed TLS chain policies that never download certificates or revocation material.
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DBNotifier.Provider.Abstractions;

/// <summary>
/// Creates a fresh offline chain policy for each TLS boundary while preserving operating-system trust decisions.
/// </summary>
public static class OfflineCertificateChainPolicy
{
    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";
    private const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";

    /// <summary>Creates a no-download policy for validating a remote TLS server certificate.</summary>
    /// <returns>A fresh chain policy requiring server-authentication use and cached revocation evidence.</returns>
    public static X509ChainPolicy CreateServerAuthentication() =>
        Create(ServerAuthenticationOid);

    /// <summary>Creates a no-download policy for validating a remote TLS client certificate.</summary>
    /// <returns>A fresh chain policy requiring client-authentication use and cached revocation evidence.</returns>
    public static X509ChainPolicy CreateClientAuthentication() =>
        Create(ClientAuthenticationOid);

    /// <summary>Builds one no-download system-trust policy for the requested extended-key use.</summary>
    /// <param name="applicationPolicyOid">Server- or client-authentication extended-key-use identifier.</param>
    /// <returns>A fresh fail-closed chain policy.</returns>
    private static X509ChainPolicy Create(string applicationPolicyOid)
    {
        X509ChainPolicy policy = new()
        {
            DisableCertificateDownloads = true,
            RevocationMode = X509RevocationMode.Offline,
            RevocationFlag = X509RevocationFlag.EntireChain,
            VerificationFlags = X509VerificationFlags.NoFlag,
        };
        policy.ApplicationPolicy.Add(new Oid(applicationPolicyOid));
        return policy;
    }
}
