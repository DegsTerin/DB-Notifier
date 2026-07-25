// Module purpose: Exposes the sole marker-gated O5-R5-B physical entrypoint and remains inert during ordinary test execution.
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Runs the physical campaign only after exact environment opt-in and output validation.</summary>
public sealed class O5R5PhysicalCampaignEntryPointTests
{
    internal const string PhysicalMarkerVariable = "DBNOTIFIER_O5_R5_B_PHYSICAL_TEST_ONLY";
    internal const string OutputVariable = "DBNOTIFIER_O5_R5_B_PHYSICAL_OUTPUT";
    internal const string SdkVariable = "DBNOTIFIER_O5_R5_B_DOTNET_SDK";
    internal const string InstalledMemoryVariable = "DBNOTIFIER_O5_R5_B_INSTALLED_MEMORY_BYTES";
    internal const string ExactPhysicalMarker =
        "266B7A952DF1A46BEE4577894D0A9206D92917AC661E0E17F9052DE1EB415DD7";
    private static readonly TimeSpan CampaignDeadline = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Executes HM-01–HM-03 only when the exact digest marker and sanitised environment declaration
    /// are present; ordinary test runs return before constructing the physical source.
    /// </summary>
    [Fact]
    [Trait("Category", "O5R5Physical")]
    public async Task RunPhysicalCampaignWhenExplicitlyAuthorised()
    {
        string? marker = Environment.GetEnvironmentVariable(PhysicalMarkerVariable);
        if (marker is null)
        {
            return;
        }

        Assert.Equal(ExactPhysicalMarker, marker);
        string destination = O5R5PhysicalEvidenceWriter.ValidateDestination(
            Environment.GetEnvironmentVariable(OutputVariable));
        string sdk = RequiredBoundedText(
            Environment.GetEnvironmentVariable(SdkVariable),
            "o5r5b.environment.sdk_required");
        long installedMemory = RequiredPositiveInt64(
            Environment.GetEnvironmentVariable(InstalledMemoryVariable),
            "o5r5b.environment.memory_required");

        O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
        Assert.Equal(ExactPhysicalMarker, protocol.Digest);
        O5R5PhysicalEnvironment environment = new(
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            RuntimeInformation.ProcessArchitecture.ToString(),
            RuntimeInformation.FrameworkDescription,
            sdk,
            Environment.ProcessorCount,
            installedMemory,
            Stopwatch.Frequency);

        // Physical construction occurs only after every opt-in and destination boundary has passed.
        O5R5PhysicalCampaignDriver driver = new(protocol, new O5R5DotNetMeasurementSource());
        using CancellationTokenSource deadline = new(CampaignDeadline);
        O5R5PhysicalCampaignReport report = await driver.RunAsync(environment, deadline.Token);
        await O5R5PhysicalEvidenceWriter.WriteAsync(destination, report, CancellationToken.None);

        Assert.True(report.Passed, report.Code);
        Assert.Equal(O5R5PhysicalCampaignDriver.ExpectedSampleCount, report.CompletedSampleCount);
        Assert.Equal(16, report.Summaries.Count);
    }

    /// <summary>Validates a small non-secret environment value without accepting peripheral whitespace.</summary>
    /// <param name="value">Candidate environment value.</param>
    /// <param name="code">Stable failure code.</param>
    /// <returns>The validated exact value.</returns>
    private static string RequiredBoundedText(string? value, string code)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > 32 ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(code);
        }

        return value;
    }

    /// <summary>Parses a positive invariant integer environment declaration.</summary>
    /// <param name="value">Candidate integer text.</param>
    /// <param name="code">Stable failure code.</param>
    /// <returns>The validated positive value.</returns>
    private static long RequiredPositiveInt64(string? value, string code)
    {
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long parsed) ||
            parsed < 1)
        {
            throw new InvalidOperationException(code);
        }

        return parsed;
    }
}
