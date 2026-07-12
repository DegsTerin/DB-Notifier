using DBNotifier.Application.Security;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Infrastructure.Security;

public sealed class UnavailableCredentialVault : ICredentialVault
{
    public ValueTask<IProviderCredential> ResolveAsync(
        CredentialReference reference,
        CancellationToken cancellationToken) =>
        ValueTask.FromException<IProviderCredential>(new CredentialUnavailableException(
            "No credential vault adapter is configured for this Agent."));
}
