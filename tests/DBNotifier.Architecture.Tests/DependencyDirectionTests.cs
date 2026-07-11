namespace DBNotifier.Architecture.Tests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void DomainDoesNotReferenceOuterDBNotifierAssemblies()
    {
        string[] references = typeof(DBNotifier.Domain.AssemblyMarker).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .Where(name => name.StartsWith("DBNotifier.", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(references);
    }
}
