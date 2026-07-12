// Module purpose: Implements Composite Credential Vault as an outer adapter behind application or provider contracts.
using DBNotifier.Application.Security;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Infrastructure.Security;

public sealed class CompositeCredentialVault : ICredentialVault
{
    private readonly Dictionary<string, ICredentialVaultAdapter> adapters;

    public CompositeCredentialVault(IEnumerable<ICredentialVaultAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        Dictionary<string, ICredentialVaultAdapter> registered = new(StringComparer.OrdinalIgnoreCase);
        foreach (ICredentialVaultAdapter adapter in adapters)
        {
            ArgumentNullException.ThrowIfNull(adapter);
            ArgumentException.ThrowIfNullOrWhiteSpace(adapter.ProviderId);
            if (!registered.TryAdd(adapter.ProviderId, adapter))
            {
                throw new InvalidOperationException($"Credential vault adapter '{adapter.ProviderId}' is registered more than once.");
            }
        }

        this.adapters = registered;
    }

    public ValueTask<IProviderCredential> ResolveAsync(
        CredentialReference reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (!adapters.TryGetValue(reference.VaultProvider, out ICredentialVaultAdapter? adapter))
        {
            return ValueTask.FromException<IProviderCredential>(new CredentialUnavailableException(
                "The configured credential vault provider is not available on this Agent."));
        }

        return adapter.ResolveAsync(reference.Locator, cancellationToken);
    }
}
