// Module purpose: Verifies Credential Vault Tests behaviour and protects the documented project contract.
using DBNotifier.Application.Security;
using DBNotifier.Domain;
using DBNotifier.Infrastructure.Security;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.UnitTests;

public sealed class CredentialVaultTests
{
    [Fact]
    public async Task CompositeVaultSelectsAdapterByStableProviderId()
    {
        StubAdapter adapter = new("test-vault");
        CompositeCredentialVault vault = new([adapter]);
        CredentialReference reference = new(
            Guid.NewGuid(),
            "test-vault",
            "opaque-locator",
            CredentialPurpose.Monitoring);

        using IProviderCredential credential = await vault.ResolveAsync(reference, CancellationToken.None);

        Assert.Equal("monitor", credential.UserName);
        Assert.Equal("temporary", new string(credential.Secret.Span));
        Assert.Equal("opaque-locator", adapter.ResolvedLocator);
    }

    [Fact]
    public async Task CompositeVaultRejectsUnknownProviderWithoutFallback()
    {
        CompositeCredentialVault vault = new([]);
        CredentialReference reference = new(
            Guid.NewGuid(),
            "unknown-vault",
            "opaque-locator",
            CredentialPurpose.Monitoring);

        CredentialUnavailableException exception = await Assert.ThrowsAsync<CredentialUnavailableException>(async () =>
            await vault.ResolveAsync(reference, CancellationToken.None));

        Assert.DoesNotContain("opaque-locator", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LinuxSecretServiceRemainsUnavailableWithoutResolvingPath()
    {
        LinuxSecretServiceVaultAdapter adapter = new();

        CredentialUnavailableException exception = await Assert.ThrowsAsync<CredentialUnavailableException>(async () =>
            await adapter.ResolveAsync("synthetic-locator", CancellationToken.None));

        Assert.DoesNotContain("synthetic-locator", exception.Message, StringComparison.Ordinal);
        Assert.Contains("unavailable", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            typeof(LinuxSecretServiceVaultAdapter).GetMethods(
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic),
            method => method.ReturnType == typeof(System.Diagnostics.ProcessStartInfo));
    }

    private sealed class StubAdapter(string providerId) : ICredentialVaultAdapter
    {
        public string ProviderId { get; } = providerId;

        public string? ResolvedLocator { get; private set; }

        public ValueTask<IProviderCredential> ResolveAsync(string locator, CancellationToken cancellationToken)
        {
            ResolvedLocator = locator;
            return ValueTask.FromResult<IProviderCredential>(
                new ProviderCredentialLease("monitor", "temporary".AsSpan()));
        }
    }
}
