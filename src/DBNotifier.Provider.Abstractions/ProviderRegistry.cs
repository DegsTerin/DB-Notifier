// Module purpose: Defines Provider Registry as an engine-neutral provider contract shared by Application and adapters.
using System.Collections.ObjectModel;
using DBNotifier.Domain;

namespace DBNotifier.Provider.Abstractions;

public sealed class ProviderRegistry : IProviderRegistry
{
    private readonly ReadOnlyDictionary<ProviderType, IDatabaseProvider> providers;

    public ProviderRegistry(IEnumerable<IDatabaseProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        Dictionary<ProviderType, IDatabaseProvider> registered = [];
        foreach (IDatabaseProvider provider in providers)
        {
            ArgumentNullException.ThrowIfNull(provider);
            if (!ProviderType.TryParse(provider.ProviderType.Value, out ProviderType canonicalProviderType) ||
                canonicalProviderType != provider.ProviderType)
            {
                throw new InvalidOperationException("A provider exposed a non-canonical provider type.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(provider.Version);
            if (!registered.TryAdd(provider.ProviderType, provider))
            {
                throw new InvalidOperationException($"Provider '{provider.ProviderType}' is registered more than once.");
            }
        }

        this.providers = new ReadOnlyDictionary<ProviderType, IDatabaseProvider>(registered);
        Providers = new ReadOnlyCollection<IDatabaseProvider>(registered.Values.ToArray());
    }

    public IReadOnlyCollection<IDatabaseProvider> Providers { get; }

    public bool TryResolve(ProviderType providerType, out IDatabaseProvider provider) =>
        providers.TryGetValue(providerType, out provider!);
}
