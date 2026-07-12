// Module purpose: Defines Credential Reference domain semantics independently of providers, persistence and presentation.
namespace DBNotifier.Domain;

public enum CredentialPurpose
{
    Monitoring,
    Administration,
    OperatingSystemControl,
    CloudControlPlane,
}

public sealed record CredentialReference
{
    public CredentialReference(Guid referenceId, string vaultProvider, string locator, CredentialPurpose purpose)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(referenceId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(vaultProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(locator);

        ReferenceId = referenceId;
        VaultProvider = vaultProvider;
        Locator = locator;
        Purpose = purpose;
    }

    public Guid ReferenceId { get; }

    public string VaultProvider { get; }

    public string Locator { get; }

    public CredentialPurpose Purpose { get; }
}
