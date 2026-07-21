// Module purpose: Keeps the unhomologated Linux Secret Service boundary explicitly unavailable.
using DBNotifier.Application.Security;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Infrastructure.Security;

/// <summary>
/// Represents the reserved Linux Secret Service adapter without invoking a PATH-resolved executable.
/// </summary>
/// <remarks>
/// Normal composition retains this adapter so existing provider identifiers fail closed until a typed
/// client or a separately homologated absolute executable path is authorised.
/// </remarks>
public sealed class LinuxSecretServiceVaultAdapter : ICredentialVaultAdapter
{
    /// <inheritdoc />
    public string ProviderId => "linux-secret-service";

    /// <inheritdoc />
    public ValueTask<IProviderCredential> ResolveAsync(
        string locator,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locator);
        if (locator.Contains('\n') || locator.Contains('\r'))
        {
            throw new CredentialUnavailableException("The Linux Secret Service locator is invalid.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new CredentialUnavailableException(
            "Linux Secret Service is unavailable until a typed or absolute-path adapter is homologated.");
    }
}
