// Module purpose: Exposes the sole marker-gated O5-R5-B physical entrypoint and remains inert during ordinary test execution.
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Runs the physical campaign in the existing dedicated sandbox host without xUnit process noise.</summary>
public static class O5R5PhysicalCampaignProcess
{
    internal const string ActivationMarker = "pf-obs-1-physical-test-only";
    private static readonly TimeSpan CampaignDeadline = TimeSpan.FromMinutes(10);

    /// <summary>Validates exact bounded arguments, runs HM-01–HM-03 and writes sanitised evidence.</summary>
    /// <param name="args">Exact activation, output, SDK and installed-memory arguments.</param>
    /// <returns>Zero for an accepted campaign, two for invalid activation or three for a failed campaign.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 8 ||
            args[0] != "--activation" ||
            args[1] != ActivationMarker ||
            args[2] != "--output" ||
            args[4] != "--sdk" ||
            args[6] != "--installed-memory" ||
            !long.TryParse(args[7], NumberStyles.None, CultureInfo.InvariantCulture, out long installedMemory) ||
            installedMemory < 1)
        {
            Console.Error.WriteLine("o5r5b.physical.activation_invalid");
            return 2;
        }

        try
        {
            string destination = O5R5PhysicalEvidenceWriter.ValidateDestination(args[3]);
            string sdk = RequiredBoundedText(args[5], "o5r5b.environment.sdk_required");
            O5R5MeasurementProtocol protocol = O5R5MeasurementProtocol.CreateFrozen();
            O5R5PhysicalEnvironment environment = new(
                RuntimeInformation.OSDescription,
                RuntimeInformation.OSArchitecture.ToString(),
                RuntimeInformation.ProcessArchitecture.ToString(),
                RuntimeInformation.FrameworkDescription,
                sdk,
                Environment.ProcessorCount,
                installedMemory,
                Stopwatch.Frequency);
            using O5R5DotNetMeasurementSource source = new();
            O5R5PhysicalCampaignDriver driver = new(protocol, source);
            using CancellationTokenSource deadline = new(CampaignDeadline);
            O5R5PhysicalCampaignReport report = await driver.RunAsync(environment, deadline.Token);
            await O5R5PhysicalEvidenceWriter.WriteAsync(destination, report, CancellationToken.None);
            if (!report.Passed ||
                report.CompletedSampleCount != O5R5PhysicalCampaignDriver.ExpectedSampleCount ||
                report.Summaries.Count != 16)
            {
                Console.Error.WriteLine(report.Code);
                return 3;
            }
            Console.WriteLine("o5r5b.physical.accepted");
            return 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"o5r5b.physical.failed:{exception.GetType().Name}");
            return 3;
        }
    }

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
}

/// <summary>Runs the physical campaign only after exact environment opt-in and output validation.</summary>
public sealed class O5R5PhysicalCampaignEntryPointTests
{
    internal const string PhysicalMarkerVariable = "DBNOTIFIER_O5_R5_B_PHYSICAL_TEST_ONLY";
    internal const string OutputVariable = "DBNOTIFIER_O5_R5_B_PHYSICAL_OUTPUT";
    internal const string SdkVariable = "DBNOTIFIER_O5_R5_B_DOTNET_SDK";
    internal const string InstalledMemoryVariable = "DBNOTIFIER_O5_R5_B_INSTALLED_MEMORY_BYTES";
    internal const string ExactPhysicalMarker =
        "60C7559F42960878B03269A1A6AAE40C944DE2DC805D8C7A73A2EF2274C2395A";
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
        using O5R5DotNetMeasurementSource source = new();
        O5R5PhysicalCampaignDriver driver = new(protocol, source);
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
