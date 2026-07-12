// Module purpose: Defines Provider Type domain semantics independently of providers, persistence and presentation.
namespace DBNotifier.Domain;

public readonly record struct ProviderType
{
    private const int MaximumLength = 64;

    private ProviderType(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ProviderType Parse(string value)
    {
        if (!TryParse(value, out ProviderType providerType))
        {
            throw new ArgumentException("Provider type must be a stable lowercase identifier.", nameof(value));
        }

        return providerType;
    }

    public static bool TryParse(string? value, out ProviderType providerType)
    {
        providerType = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaximumLength || !char.IsAsciiLetterOrDigit(normalized[0]))
        {
            return false;
        }

        foreach (char character in normalized)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not '.' and not '-' and not '_')
            {
                return false;
            }
        }

        providerType = new ProviderType(normalized);
        return true;
    }

    public override string ToString() => Value ?? string.Empty;
}
