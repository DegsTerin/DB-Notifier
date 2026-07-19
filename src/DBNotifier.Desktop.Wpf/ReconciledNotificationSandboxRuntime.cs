// Module purpose: Composes the opt-in WPF notification consumer only for an exact ephemeral local sandbox invocation.
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows.Threading;
using DBNotifier.Application.Presentation;
using DBNotifier.Infrastructure.Presentation;

namespace DBNotifier.Desktop.Wpf;

/// <summary>Contains validated non-operational inputs for one explicitly opted-in local notification sandbox.</summary>
/// <param name="ApiBaseAddress">Pinned HTTPS loopback API origin.</param>
/// <param name="StateDirectory">Dedicated temporary directory for the isolated ledger.</param>
/// <param name="CertificateThumbprint">Expected ephemeral test-certificate thumbprint.</param>
/// <param name="TestSubject">Bounded test-only authentication subject.</param>
/// <param name="Quiet">Whether valid transitions are durably suppressed.</param>
internal sealed record ReconciledNotificationSandboxActivation(
    Uri ApiBaseAddress,
    string StateDirectory,
    string CertificateThumbprint,
    string TestSubject,
    bool Quiet);

/// <summary>Fails closed unless every local-only argument and the explicit notification opt-in are present and valid.</summary>
internal static class ReconciledNotificationSandboxActivationPolicy
{
    private const string ActivationFlag = "--reconciled-notification-sandbox";
    private const string OptInFlag = "--notifications-opt-in";
    private const string QuietFlag = "--notifications-quiet";

    /// <summary>Resolves one exact sandbox activation; absence or malformed input leaves the runtime disabled.</summary>
    /// <param name="arguments">WPF process arguments.</param>
    /// <returns>Validated activation, or null to preserve the disabled default.</returns>
    public static ReconciledNotificationSandboxActivation? Resolve(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Count(item => string.Equals(item, ActivationFlag, StringComparison.Ordinal)) != 1 ||
            arguments.Count(item => string.Equals(item, OptInFlag, StringComparison.Ordinal)) != 1)
        {
            return null;
        }

        string? apiBase = ReadValue(arguments, "--api-base");
        string? stateDirectory = ReadValue(arguments, "--state-directory");
        string? certificateThumbprint = ReadValue(arguments, "--test-certificate-thumbprint");
        string? testSubject = ReadValue(arguments, "--test-subject");
        if (stateDirectory is null || certificateThumbprint is null || testSubject is null ||
            !Uri.TryCreate(apiBase, UriKind.Absolute, out Uri? apiUri) ||
            !string.Equals(apiUri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) ||
            !IPAddress.TryParse(apiUri.Host, out IPAddress? address) || !IPAddress.IsLoopback(address) ||
            !string.IsNullOrEmpty(apiUri.UserInfo) || !string.IsNullOrEmpty(apiUri.Query) ||
            !string.IsNullOrEmpty(apiUri.Fragment) || apiUri.AbsolutePath != "/" ||
            string.IsNullOrWhiteSpace(stateDirectory) || !Path.IsPathFullyQualified(stateDirectory) ||
            !IsUnderTemporaryRoot(stateDirectory) ||
            certificateThumbprint.Length != 64 ||
            certificateThumbprint.Any(character => !Uri.IsHexDigit(character)) ||
            string.IsNullOrWhiteSpace(testSubject) || testSubject.Length > 64 ||
            testSubject.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')))
        {
            return null;
        }

        return new(
            apiUri,
            Path.GetFullPath(stateDirectory),
            certificateThumbprint.ToUpperInvariant(),
            testSubject,
            arguments.Contains(QuietFlag, StringComparer.Ordinal));
    }

    private static string? ReadValue(IReadOnlyList<string> arguments, string option)
    {
        int index = -1;
        for (int candidate = 0; candidate < arguments.Count; candidate++)
        {
            if (!string.Equals(arguments[candidate], option, StringComparison.Ordinal))
            {
                continue;
            }
            if (index >= 0)
            {
                return null;
            }
            index = candidate;
        }
        return index >= 0 && index + 1 < arguments.Count ? arguments[index + 1] : null;
    }

    private static bool IsUnderTemporaryRoot(string path)
    {
        string root = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string candidate = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) && candidate.Length > root.Length;
    }
}

/// <summary>Runs immediate and thirty-second serial reconciliation cycles and owns every temporary adapter.</summary>
internal sealed class ReconciledNotificationSandboxRuntime : IAsyncDisposable
{
    private static readonly TimeSpan ReconciliationInterval = TimeSpan.FromSeconds(30);
    private readonly CancellationTokenSource cancellation = new();
    private readonly HttpClient client;
    private readonly FileReconciledNotificationLedger ledger;
    private readonly ReconciledNotificationCoordinator coordinator;
    private readonly ReconciledNotificationSandboxPolicy policy;
    private readonly Task runTask;
    private bool disposed;

    /// <summary>Creates the isolated adapters and begins one immediate silent-baseline cycle.</summary>
    /// <param name="activation">Validated local-only activation.</param>
    /// <param name="dispatcher">Owning WPF dispatcher for the platform boundary.</param>
    /// <param name="deliver">Local Windows publication callback.</param>
    public ReconciledNotificationSandboxRuntime(
        ReconciledNotificationSandboxActivation activation,
        Dispatcher dispatcher,
        Func<ReconciledNotificationDeliveryRequest, ReconciledNotificationDeliveryResult> deliver)
    {
        ArgumentNullException.ThrowIfNull(activation);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(deliver);

        string expectedThumbprint = activation.CertificateThumbprint;
        HttpClientHandler handler = new()
        {
            ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                string.Equals(
                    certificate?.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256),
                    expectedThumbprint,
                    StringComparison.OrdinalIgnoreCase),
        };
        client = new HttpClient(handler)
        {
            BaseAddress = activation.ApiBaseAddress,
            Timeout = TimeSpan.FromSeconds(5),
        };
        client.DefaultRequestHeaders.Add(
            "X-DBN-TV-Test-Human",
            activation.TestSubject);
        ledger = new FileReconciledNotificationLedger(activation.StateDirectory);
        HttpReconciledNotificationTransitionReader reader = new(client, TimeProvider.System);
        coordinator = new ReconciledNotificationCoordinator(
            reader,
            ledger,
            new DispatcherNotificationSink(dispatcher, deliver),
            TimeProvider.System);
        policy = new(true, activation.Quiet);
        runTask = RunAsync(cancellation.Token);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        cancellation.Cancel();
        try
        {
            await runTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is the expected owner-controlled shutdown path.
        }
        coordinator.Dispose();
        await ledger.DisposeAsync().ConfigureAwait(false);
        client.Dispose();
        cancellation.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(ReconciliationInterval);
        try
        {
            do
            {
                ReconciledNotificationCycleResult result = await coordinator
                    .RunOnceAsync(policy, cancellationToken)
                    .ConfigureAwait(false);
                if (result.Disposition is ReconciledNotificationCycleDisposition.Denied or
                    ReconciledNotificationCycleDisposition.Incompatible or
                    ReconciledNotificationCycleDisposition.Conflict)
                {
                    Trace.TraceWarning("DB Notifier stopped the local notification sandbox after a fail-closed reconciliation result.");
                    return;
                }
            }
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Owner cancellation terminates the temporary cycle without retry or external side effect.
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            Trace.TraceWarning(
                "DB Notifier stopped the local notification sandbox after isolated ledger failure ({0}).",
                exception.GetType().Name);
        }
    }

    private sealed class DispatcherNotificationSink : IReconciledNotificationSink
    {
        private readonly Dispatcher dispatcher;
        private readonly Func<ReconciledNotificationDeliveryRequest, ReconciledNotificationDeliveryResult> deliver;

        public DispatcherNotificationSink(
            Dispatcher dispatcher,
            Func<ReconciledNotificationDeliveryRequest, ReconciledNotificationDeliveryResult> deliver)
        {
            this.dispatcher = dispatcher;
            this.deliver = deliver;
        }

        /// <inheritdoc />
        public async ValueTask<ReconciledNotificationDeliveryResult> DeliverAsync(
            ReconciledNotificationDeliveryRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (dispatcher.CheckAccess())
            {
                return deliver(request);
            }
            DispatcherOperation<ReconciledNotificationDeliveryResult> operation = dispatcher.InvokeAsync(
                () => deliver(request),
                DispatcherPriority.Normal,
                cancellationToken);
            return await operation.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
