// Module purpose: Verifies the bounded Desktop Fleet HTTP source, protocol mapping and Tray reconciliation integration.
using System.Net;
using System.Net.Http.Json;
using DBNotifier.Application.Presentation;
using DBNotifier.Infrastructure.Presentation;

namespace DBNotifier.UnitTests;

/// <summary>Protects the authenticated read-only HTTP adapter from protocol, transport and source-truth regressions.</summary>
public sealed class HttpDesktopFleetSnapshotSourceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly Uri BaseAddress = new("https://db-notifier.invalid/base/");

    [Fact]
    public async Task AcceptedResponseFeedsTheExistingTrayCoordinator()
    {
        RecordingHandler handler = new(request => Response(
            request,
            HttpStatusCode.OK,
            new DesktopFleetApiSnapshot(
                DesktopFleetApiContract.CurrentSchemaVersion,
                Now,
                [new DesktopFleetApiItem(
                    Guid.Parse("00000000-0000-0000-0000-000000000201"),
                    "Orders",
                    "provider.fixture",
                    "Provider-readiness evidence",
                    "production",
                    "Remote Agent",
                    "degraded",
                    Now.AddSeconds(-2),
                    Now.AddSeconds(-1),
                    125,
                    true)])));
        using HttpClient client = new(handler);
        HttpDesktopFleetSnapshotSource source = new(client, BaseAddress, new FixedTimeProvider(Now));
        DesktopFleetReconciliationCoordinator coordinator = new(source, TimeSpan.FromMinutes(5), new FixedTimeProvider(Now));

        DesktopFleetReconciliationFrame frame = await coordinator.ReconcileAsync(DesktopFleetRefreshTrigger.Initial);

        Assert.Equal(DesktopFleetReadDisposition.Accepted, frame.Disposition);
        Assert.Equal("source.agent-api.accepted", frame.ReasonCode);
        Assert.Equal(TrayAggregateState.Warning, frame.Summary.State);
        Assert.Equal("provider.fixture", Assert.Single(frame.Snapshot!.Items).ProviderType);
        Assert.Equal(HttpMethod.Get, handler.Request!.Method);
        Assert.Equal(new Uri(BaseAddress, "api/v1/desktop/fleet-snapshot"), handler.Request.RequestUri);
        Assert.Equal("1", Assert.Single(handler.Request.Headers.GetValues("DBN-Protocol-Version")));
        Assert.Equal(
            DesktopFleetApiContract.CurrentSchemaVersion,
            Assert.Single(handler.Request.Headers.GetValues("DBN-Message-Schema")));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, DesktopFleetReadDisposition.Denied, "source.agent-api.denied")]
    [InlineData(HttpStatusCode.Forbidden, DesktopFleetReadDisposition.Denied, "source.agent-api.denied")]
    [InlineData(HttpStatusCode.UpgradeRequired, DesktopFleetReadDisposition.Incompatible, "source.agent-api.incompatible")]
    [InlineData(HttpStatusCode.ServiceUnavailable, DesktopFleetReadDisposition.Offline, "source.agent-api.offline")]
    [InlineData(HttpStatusCode.BadRequest, DesktopFleetReadDisposition.Failed, "source.agent-api.failed")]
    public async Task HttpFailureMapsToStableNonSecretOutcome(
        HttpStatusCode statusCode,
        DesktopFleetReadDisposition disposition,
        string reasonCode)
    {
        RecordingHandler handler = new(request => new HttpResponseMessage(statusCode) { RequestMessage = request });
        using HttpClient client = new(handler);
        HttpDesktopFleetSnapshotSource source = new(client, BaseAddress, new FixedTimeProvider(Now));

        DesktopFleetSnapshotReadResult result = await source.ReadAsync(CancellationToken.None);

        Assert.Equal(disposition, result.Disposition);
        Assert.Equal(reasonCode, result.ReasonCode);
        Assert.Null(result.Snapshot);
    }

    [Fact]
    public async Task InvalidSchemaIsRejectedWithoutReplacingEvidence()
    {
        RecordingHandler handler = new(request => Response(
            request,
            HttpStatusCode.OK,
            new DesktopFleetApiSnapshot("desktop-fleet.v2", Now, [])));
        using HttpClient client = new(handler);
        HttpDesktopFleetSnapshotSource source = new(client, BaseAddress, new FixedTimeProvider(Now));

        DesktopFleetSnapshotReadResult result = await source.ReadAsync(CancellationToken.None);

        Assert.Equal(DesktopFleetReadDisposition.Incompatible, result.Disposition);
        Assert.Equal("source.agent-api.snapshot-invalid", result.ReasonCode);
    }

    [Fact]
    public void NonHttpsBaseAddressIsRejectedBeforeAnyRequest()
    {
        using HttpClient client = new(new RecordingHandler(_ => throw new InvalidOperationException("No request expected.")));

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new HttpDesktopFleetSnapshotSource(client, new Uri("http://db-notifier.invalid/"), new FixedTimeProvider(Now)));

        Assert.Equal("apiBaseAddress", exception.ParamName);
    }

    /// <summary>Creates one protocol-complete JSON response for an exact request.</summary>
    private static HttpResponseMessage Response(
        HttpRequestMessage request,
        HttpStatusCode statusCode,
        DesktopFleetApiSnapshot snapshot)
    {
        HttpResponseMessage response = new(statusCode)
        {
            RequestMessage = request,
            Content = JsonContent.Create(snapshot),
        };
        response.Headers.TryAddWithoutValidation("DBN-Protocol-Version", "1");
        response.Headers.TryAddWithoutValidation("DBN-Message-Schema", DesktopFleetApiContract.CurrentSchemaVersion);
        return response;
    }

    /// <summary>Captures one request and returns the deterministic response selected by the test.</summary>
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(responseFactory(request));
        }
    }

    /// <summary>Supplies one deterministic UTC instant to source and coordinator tests.</summary>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
