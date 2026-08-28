// Module purpose: Reads the provider-neutral Desktop Fleet API through a bounded authenticated HTTPS client without owning identity or providers.
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBNotifier.Application.Presentation;
using DBNotifier.Infrastructure.Http;

namespace DBNotifier.Infrastructure.Presentation;

/// <summary>
/// Adapts one caller-owned authenticated HTTP client to the Tray snapshot source contract. The caller retains
/// responsibility for short-lived human identity, exact network policy and disabled redirects; this adapter never
/// acquires, persists or logs bearer material.
/// </summary>
public sealed class HttpDesktopFleetSnapshotSource : IDesktopFleetSnapshotSource
{
    private static readonly IReadOnlySet<string> AllowedMediaTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
    };

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 8,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly HttpClient httpClient;
    private readonly Uri endpoint;
    private readonly TimeProvider timeProvider;

    /// <summary>Initialises one bounded read-only Desktop Fleet transport.</summary>
    /// <param name="httpClient">Caller-owned client already configured with the authorised in-memory human identity and no redirects.</param>
    /// <param name="apiBaseAddress">Absolute HTTPS Server base address without user information, query or fragment.</param>
    /// <param name="timeProvider">Clock used to reject future Server evidence.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required dependency is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the Server base address is outside the HTTPS boundary.</exception>
    public HttpDesktopFleetSnapshotSource(
        HttpClient httpClient,
        Uri apiBaseAddress,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(apiBaseAddress);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (!apiBaseAddress.IsAbsoluteUri ||
            !string.Equals(apiBaseAddress.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(apiBaseAddress.UserInfo) ||
            !string.IsNullOrEmpty(apiBaseAddress.Query) ||
            !string.IsNullOrEmpty(apiBaseAddress.Fragment))
        {
            throw new ArgumentException("The Desktop Fleet API base address must be an absolute HTTPS origin or path.", nameof(apiBaseAddress));
        }

        this.httpClient = httpClient;
        endpoint = new Uri(EnsureTrailingSlash(apiBaseAddress), DesktopFleetApiContract.SnapshotRoute.TrimStart('/'));
        this.timeProvider = timeProvider;
    }

    /// <summary>Reads and validates one complete API snapshot without mutating the Server, Agent or monitored database.</summary>
    /// <param name="cancellationToken">Cancellation requested by the owning desktop lifecycle.</param>
    /// <returns>A typed factual outcome containing no response body or provider-native diagnostic text.</returns>
    /// <exception cref="OperationCanceledException">Propagated only when the caller cancels the acquisition.</exception>
    public async ValueTask<DesktopFleetSnapshotReadResult> ReadAsync(CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("DBN-Protocol-Version", "1");
        request.Headers.TryAddWithoutValidation("DBN-Message-Schema", DesktopFleetApiContract.CurrentSchemaVersion);

        try
        {
            using HttpResponseMessage response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return MapFailure(response.StatusCode);
            }

            if (response.RequestMessage?.RequestUri is not Uri responseUri ||
                !Uri.Compare(responseUri, endpoint, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase).Equals(0) ||
                !HasExactResponseHeaders(response))
            {
                return Incompatible("source.agent-api.protocol-invalid");
            }

            DesktopFleetApiSnapshot wire = await BoundedHttpJsonReader.ReadAsync<DesktopFleetApiSnapshot>(
                response.Content,
                DesktopFleetApiContract.MaximumResponseBytes,
                SerializerOptions,
                AllowedMediaTypes,
                cancellationToken).ConfigureAwait(false);
            if (!DesktopFleetApiSnapshotMapper.TryMap(
                    wire,
                    timeProvider.GetUtcNow().ToUniversalTime(),
                    out InventorySnapshot? snapshot))
            {
                return Incompatible("source.agent-api.snapshot-invalid");
            }

            return new DesktopFleetSnapshotReadResult(
                DesktopFleetReadDisposition.Accepted,
                snapshot,
                "source.agent-api.accepted");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is BoundedHttpJsonException or JsonException or NotSupportedException)
        {
            return Incompatible("source.agent-api.response-invalid");
        }
        catch (Exception exception) when (
            exception is HttpRequestException or IOException or OperationCanceledException)
        {
            return new DesktopFleetSnapshotReadResult(
                DesktopFleetReadDisposition.Offline,
                null,
                "source.agent-api.offline");
        }
    }

    /// <summary>Maps HTTP status without retaining titles, bodies, headers or remote diagnostics.</summary>
    private static DesktopFleetSnapshotReadResult MapFailure(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new(
            DesktopFleetReadDisposition.Denied,
            null,
            "source.agent-api.denied"),
        HttpStatusCode.NotFound or HttpStatusCode.UpgradeRequired => Incompatible("source.agent-api.incompatible"),
        HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or
            HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => new(
                DesktopFleetReadDisposition.Offline,
                null,
                "source.agent-api.offline"),
        _ => new DesktopFleetSnapshotReadResult(
            DesktopFleetReadDisposition.Failed,
            null,
            "source.agent-api.failed"),
    };

    /// <summary>Creates one stable incompatible result without untrusted diagnostic content.</summary>
    private static DesktopFleetSnapshotReadResult Incompatible(string reasonCode) =>
        new(DesktopFleetReadDisposition.Incompatible, null, reasonCode);

    /// <summary>Requires the exact protocol and schema echoed by the Server response.</summary>
    private static bool HasExactResponseHeaders(HttpResponseMessage response) =>
        response.Headers.TryGetValues("DBN-Protocol-Version", out IEnumerable<string>? protocols) &&
        protocols.SequenceEqual(["1"], StringComparer.Ordinal) &&
        response.Headers.TryGetValues("DBN-Message-Schema", out IEnumerable<string>? schemas) &&
        schemas.SequenceEqual([DesktopFleetApiContract.CurrentSchemaVersion], StringComparer.Ordinal);

    /// <summary>Normalises base-path combination without changing the configured HTTPS origin or path.</summary>
    private static Uri EnsureTrailingSlash(Uri value) =>
        value.AbsolutePath.EndsWith('/')
            ? value
            : new UriBuilder(value) { Path = value.AbsolutePath + "/" }.Uri;
}
