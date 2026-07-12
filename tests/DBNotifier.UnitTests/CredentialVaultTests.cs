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
    public void LinuxSecretToolUsesSeparatedArgumentsWithoutShell()
    {
        const string locator = "database;not-a-shell-command";

        System.Diagnostics.ProcessStartInfo startInfo = LinuxSecretServiceVaultAdapter.CreateStartInfo(locator);

        Assert.False(startInfo.UseShellExecute);
        Assert.Equal("secret-tool", startInfo.FileName);
        Assert.Equal(["lookup", "application", "db-notifier", "id", locator], startInfo.ArgumentList);
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
