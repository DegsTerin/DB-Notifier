// Module purpose: Verifies Dependency Direction Tests behaviour and protects the documented project contract.
using System.Reflection;

namespace DBNotifier.Architecture.Tests;

/// <summary>Protects inward dependencies and the deliberately narrow public surfaces of core DB-Notifier assemblies.</summary>
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

    /// <summary>Proves the temporary resilience harness and the ordinary Agent Worker cannot compose one another.</summary>
    [Fact]
    public void AgentFleetSandboxHarnessRemainsOutsideTheOrdinaryWorker()
    {
        string[] workerReferences = GetDBNotifierReferences(typeof(DBNotifier.Agent.Worker.AgentFleetClientOptions).Assembly);
        string[] harnessReferences = GetDBNotifierReferences(
            typeof(DBNotifier.AgentFleet.SandboxHost.SandboxHostMarker).Assembly);

        Assert.DoesNotContain("DBNotifier.AgentFleet.SandboxHost", workerReferences);
        Assert.DoesNotContain("DBNotifier.Agent.Worker", harnessReferences);
        Assert.DoesNotContain("DBNotifier.Persistence.Server.PostgreSql", harnessReferences);
        Assert.DoesNotContain("DBNotifier.Server.Api", harnessReferences);
        Assert.DoesNotContain("DBNotifier.Providers.PostgreSql", harnessReferences);
        Assert.Contains("DBNotifier.Persistence.Agent.Sqlite", harnessReferences);
        Assert.Contains("DBNotifier.Provider.Abstractions", harnessReferences);
    }

    /// <summary>Verifies the exact approved AIOps surface and its sole intentional Domain health-observation dependency.</summary>
    [Fact]
    public void AIOpsObserverPublicSurfaceExposesOnlyApprovedContracts()
    {
        Type[] observerTypes = typeof(DBNotifier.Application.AssemblyMarker).Assembly
            .GetExportedTypes()
            .Where(type => string.Equals(type.Namespace, "DBNotifier.Application.AIOps", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(observerTypes);
        string[] approvedTypeNames =
        [
            "CanonicalObserverTelemetryAdapter",
            "CapacityForecastAnalyser",
            "DeterministicThresholdAnalyser",
            "ObserverActivationState",
            "ObserverAnalysisDisposition",
            "ObserverAnalysisExecutionContext",
            "ObserverAnalysisPolicy",
            "ObserverAnalysisReport",
            "ObserverAnalysisRequest",
            "ObserverAnalysisService",
            "ObserverCapacityForecastPolicy",
            "ObserverCapacityForecastResult",
            "ObserverCapabilityProfile",
            "ObserverCanonicalHealthTelemetry",
            "ObserverCorpusProvenanceAuthority",
            "ObserverDataClassification",
            "ObserverDataOptInState",
            "ObserverDataPolicy",
            "ObserverDataPolicyGrant",
            "ObserverDataPolicyVerificationContext",
            "ObserverDataScope",
            "ObserverDataUse",
            "ObserverEvidenceQuality",
            "ObserverFindingDisposition",
            "ObserverFindingSeverity",
            "ObserverMetricCardinality",
            "ObserverMetricSample",
            "ObserverMissingness",
            "ObserverOfflineCaseKind",
            "ObserverOfflineEvaluationCase",
            "ObserverOfflineEvaluationCaseResult",
            "ObserverOfflineEvaluationDataset",
            "ObserverOfflineEvaluationReport",
            "ObserverOfflineEvaluationRunner",
            "ObserverOfflineEvaluationSegment",
            "ObserverOfflineSegmentResult",
            "ObserverOfflineExpectedDisposition",
            "ObserverPolicyProvenancePayload",
            "ObserverPolicyRevocationSnapshot",
            "ObserverPolicyTrustAnchor",
            "ObserverPolicyTrustConfiguration",
            "ObserverProcessingBudget",
            "ObserverPermittedPurpose",
            "ObserverRedactionStatus",
            "ObserverRetentionClass",
            "ObserverTelemetryAdaptationDisposition",
            "ObserverTelemetryAdaptationResult",
            "ObserverThresholdComparison",
            "ObserverThresholdResult",
            "ObserverThresholdRule",
        ];
        Assert.Equal(
            approvedTypeNames.Order(StringComparer.Ordinal),
            observerTypes.Select(type => type.Name).Order(StringComparer.Ordinal));

        Type[] outerTypes = observerTypes
            .SelectMany(GetPublicSignatureTypes)
            .Where(type =>
                (type.Assembly.GetName().Name ?? string.Empty).StartsWith("DBNotifier.", StringComparison.Ordinal) &&
                !string.Equals(
                    type.Assembly.GetName().Name,
                    "DBNotifier.Application",
                    StringComparison.Ordinal))
            .Distinct()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal([typeof(DBNotifier.Domain.HealthObservation)], outerTypes);

        string[] declaredOperationNames = observerTypes
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Adapt", "Analyse", "CreateGrant", "CreateRevocation", "Evaluate"], declaredOperationNames);
    }

    /// <summary>Returns public constructor, property and method types for one exported contract type.</summary>
    /// <param name="contractType">Exported AIOps contract type to inspect.</param>
    /// <returns>The contract and every type exposed directly or through a generic wrapper.</returns>
    private static IEnumerable<Type> GetPublicSignatureTypes(Type contractType)
    {
        yield return contractType;
        foreach (Type type in contractType
                     .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                     .SelectMany(constructor => constructor.GetParameters())
                     .Select(parameter => parameter.ParameterType)
                     .Concat(contractType
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                         .Select(property => property.PropertyType))
                     .Concat(contractType
                         .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                         .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)
                             .Append(method.ReturnType)))
                     .SelectMany(ExpandType))
        {
            yield return type;
        }
    }

    /// <summary>Expands arrays and generic arguments so hidden outer-layer types cannot cross a wrapper.</summary>
    /// <param name="type">Signature type to expand recursively.</param>
    /// <returns>The supplied type, its element type and all generic arguments.</returns>
    private static IEnumerable<Type> ExpandType(Type type)
    {
        yield return type;
        if (type.HasElementType && type.GetElementType() is { } elementType)
        {
            foreach (Type expanded in ExpandType(elementType))
            {
                yield return expanded;
            }
        }

        foreach (Type argument in type.GetGenericArguments())
        {
            foreach (Type expanded in ExpandType(argument))
            {
                yield return expanded;
            }
        }
    }

    /// <summary>Returns project references from one assembly in stable name order.</summary>
    /// <param name="assembly">Assembly whose DB-Notifier references are inspected.</param>
    /// <returns>DB-Notifier assembly names in ordinal order.</returns>
    private static string[] GetDBNotifierReferences(System.Reflection.Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("DBNotifier.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
}
