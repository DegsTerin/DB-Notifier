// Module purpose: Verifies Bootstrap Tests behaviour and protects the documented project contract.
namespace DBNotifier.UnitTests;

public sealed class BootstrapTests
{
    [Fact]
    public void DomainAssemblyCanBeLoaded()
    {
        Assert.Equal("DBNotifier.Domain", typeof(DBNotifier.Domain.AssemblyMarker).Assembly.GetName().Name);
    }
}
