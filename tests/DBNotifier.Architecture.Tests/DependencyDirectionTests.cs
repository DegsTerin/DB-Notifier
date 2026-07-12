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

    [Fact]
    public void ApplicationDoesNotReferenceConcreteProviders()
    {
        string[] references = GetDBNotifierReferences(typeof(DBNotifier.Application.AssemblyMarker).Assembly);

        Assert.DoesNotContain(references, name => name.StartsWith("DBNotifier.Providers.", StringComparison.Ordinal));
        Assert.Contains("DBNotifier.Domain", references);
        Assert.Contains("DBNotifier.Provider.Abstractions", references);
    }

    [Fact]
    public void ProviderAbstractionsDoNotReferenceApplicationOrConcreteProviders()
    {
        string[] references = GetDBNotifierReferences(typeof(DBNotifier.Provider.Abstractions.AssemblyMarker).Assembly);

        Assert.Equal(["DBNotifier.Domain"], references);
    }

    [Fact]
    public void PostgreSqlProviderDoesNotReferenceApplication()
    {
        string[] references = GetDBNotifierReferences(typeof(DBNotifier.Providers.PostgreSql.AssemblyMarker).Assembly);

        Assert.DoesNotContain("DBNotifier.Application", references);
        Assert.Contains("DBNotifier.Provider.Abstractions", references);
    }

    [Fact]
    public void ConfigurationMigratorCliIsIsolatedFromProductRuntimeAssemblies()
    {
        string[] references = GetDBNotifierReferences(typeof(DBNotifier.ConfigMigrator.AssemblyMarker).Assembly);

        Assert.Empty(references);
    }

    private static string[] GetDBNotifierReferences(System.Reflection.Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("DBNotifier.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
}
