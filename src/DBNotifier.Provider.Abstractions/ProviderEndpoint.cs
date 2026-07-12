using System.Collections.ObjectModel;
using DBNotifier.Domain;

namespace DBNotifier.Provider.Abstractions;

public sealed class ProviderEndpoint
{
    private static readonly string[] ForbiddenKeyFragments =
    [
        "password",
        "secret",
        "token",
        "connectionstring",
        "privatekey",
    ];

    private readonly ReadOnlyDictionary<string, string> properties;

    public ProviderEndpoint(ProviderType providerType, IEnumerable<KeyValuePair<string, string>> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (!ProviderType.TryParse(providerType.Value, out ProviderType canonicalProviderType) ||
            canonicalProviderType != providerType)
        {
            throw new ArgumentException("Provider type must be canonical.", nameof(providerType));
        }

        Dictionary<string, string> copy = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string key, string value) in properties)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (ForbiddenKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Endpoint property '{key}' is reserved for secret references.", nameof(properties));
            }

            if (!copy.TryAdd(key, value ?? string.Empty))
            {
                throw new ArgumentException($"Duplicate endpoint property '{key}'.", nameof(properties));
            }
        }

        ProviderType = canonicalProviderType;
        this.properties = new ReadOnlyDictionary<string, string>(copy);
    }

    public ProviderType ProviderType { get; }

    public IReadOnlyDictionary<string, string> Properties => properties;

    public bool TryGetValue(string key, out string value) => properties.TryGetValue(key, out value!);
}
