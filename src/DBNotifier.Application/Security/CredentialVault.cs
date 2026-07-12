// Module purpose: Defines Credential Vault application behaviour without depending on concrete providers or user interfaces.
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Application.Security;

public interface ICredentialVault
{
    ValueTask<IProviderCredential> ResolveAsync(
        CredentialReference reference,
        CancellationToken cancellationToken);
}

public interface ICredentialVaultAdapter
{
    string ProviderId { get; }

    ValueTask<IProviderCredential> ResolveAsync(string locator, CancellationToken cancellationToken);
}

public sealed class CredentialUnavailableException(string safeMessage) : Exception(safeMessage);

public sealed class ProviderCredentialLease : IProviderCredential
{
    private char[]? secret;

    public ProviderCredentialLease(
        string? userName,
        ReadOnlySpan<char> secret,
        DateTimeOffset? expiresAt = null)
    {
        UserName = userName;
        this.secret = secret.ToArray();
        ExpiresAt = expiresAt;
    }

    public string? UserName { get; }

    public DateTimeOffset? ExpiresAt { get; }

    public ReadOnlyMemory<char> Secret => secret ?? throw new ObjectDisposedException(nameof(ProviderCredentialLease));

    public void Dispose()
    {
        char[]? value = Interlocked.Exchange(ref secret, null);
        if (value is not null)
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(value.AsSpan()));
        }
    }
}
