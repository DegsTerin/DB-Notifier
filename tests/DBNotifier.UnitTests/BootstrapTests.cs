namespace DBNotifier.UnitTests;

public sealed class BootstrapTests
{
    [Fact]
    public void DomainAssemblyCanBeLoaded()
    {
        Assert.Equal("DBNotifier.Domain", typeof(DBNotifier.Domain.AssemblyMarker).Assembly.GetName().Name);
    }
}
