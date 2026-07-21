// Module purpose: Verifies exact byte, media type, depth and schema limits for the shared HTTP JSON response boundary.
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBNotifier.Infrastructure.Http;

namespace DBNotifier.UnitTests;

/// <summary>Exercises streamed response limits independently of Content-Length and any concrete transport.</summary>
public sealed class BoundedHttpJsonReaderTests
{
    private static readonly HashSet<string> JsonMediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
    };
    private static readonly JsonSerializerOptions StrictOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 8,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>Proves exactly N streamed bytes are accepted while N+1 is refused without a declared length.</summary>
    [Fact]
    public async Task StreamedByteLimitAcceptsNAndRejectsNPlusOne()
    {
        const int maximumBytes = 128;
        byte[] prefix = Encoding.UTF8.GetBytes("{\"value\":\"");
        byte[] suffix = Encoding.UTF8.GetBytes("\"}");
        string value = new('x', maximumBytes - prefix.Length - suffix.Length);
        byte[] exact = Encoding.UTF8.GetBytes($"{{\"value\":\"{value}\"}}");
        Assert.Equal(maximumBytes, exact.Length);

        using HttpContent acceptedContent = new UnknownLengthJsonContent(exact);
        BoundedFixturePayload accepted = await BoundedHttpJsonReader.ReadAsync<BoundedFixturePayload>(
            acceptedContent,
            maximumBytes,
            StrictOptions,
            JsonMediaTypes,
            CancellationToken.None);
        Assert.Equal(value, accepted.Value);

        using HttpContent refusedContent = new UnknownLengthJsonContent([.. exact, (byte)' ']);
        BoundedHttpJsonException exception = await Assert.ThrowsAsync<BoundedHttpJsonException>(() =>
            BoundedHttpJsonReader.ReadAsync<BoundedFixturePayload>(
                refusedContent,
                maximumBytes,
                StrictOptions,
                JsonMediaTypes,
                CancellationToken.None).AsTask());
        Assert.Equal(BoundedHttpJsonFailure.TooLarge, exception.Failure);
    }

    /// <summary>Proves media type, unknown members and excessive JSON depth all fail before a value is trusted.</summary>
    [Fact]
    public async Task ReaderRejectsInvalidContentTypeUnknownSchemaAndDepth()
    {
        using HttpContent invalidType = new UnknownLengthJsonContent(
            Encoding.UTF8.GetBytes("{\"value\":\"fixture\"}"),
            "text/plain");
        BoundedHttpJsonException contentTypeError = await Assert.ThrowsAsync<BoundedHttpJsonException>(() =>
            BoundedHttpJsonReader.ReadAsync<BoundedFixturePayload>(
                invalidType,
                1024,
                StrictOptions,
                JsonMediaTypes,
                CancellationToken.None).AsTask());
        Assert.Equal(BoundedHttpJsonFailure.ContentTypeInvalid, contentTypeError.Failure);

        using HttpContent unknownMember = new UnknownLengthJsonContent(
            Encoding.UTF8.GetBytes("{\"value\":\"fixture\",\"unexpected\":true}"));
        await Assert.ThrowsAsync<JsonException>(() => BoundedHttpJsonReader.ReadAsync<BoundedFixturePayload>(
            unknownMember,
            1024,
            StrictOptions,
            JsonMediaTypes,
            CancellationToken.None).AsTask());

        using HttpContent excessiveDepth = new UnknownLengthJsonContent(
            Encoding.UTF8.GetBytes("{\"value\":{\"a\":{\"b\":{\"c\":{\"d\":{\"e\":1}}}}}}"));
        await Assert.ThrowsAsync<JsonException>(() => BoundedHttpJsonReader.ReadAsync<BoundedJsonEnvelope>(
            excessiveDepth,
            1024,
            new JsonSerializerOptions(StrictOptions) { MaxDepth = 4 },
            JsonMediaTypes,
            CancellationToken.None).AsTask());
    }

    /// <summary>Contains one exact strict fixture contract.</summary>
    /// <param name="Value">Bounded fixture text.</param>
    private sealed record BoundedFixturePayload(string Value);

    /// <summary>Allows depth-only validation while preserving an exact outer schema.</summary>
    /// <param name="Value">Untrusted nested JSON value.</param>
    private sealed record BoundedJsonEnvelope(JsonElement Value);

    /// <summary>Streams supplied bytes without exposing Content-Length, reproducing chunked or misreported responses.</summary>
    private sealed class UnknownLengthJsonContent : HttpContent
    {
        private readonly byte[] content;

        /// <summary>Initialises exact bytes and response media type without declaring their length.</summary>
        /// <param name="bytes">Bytes returned by the synthetic response stream.</param>
        /// <param name="mediaType">Exact response media type.</param>
        public UnknownLengthJsonContent(byte[] bytes, string mediaType = "application/json")
        {
            content = bytes ?? throw new ArgumentNullException(nameof(bytes));
            Headers.ContentType = new MediaTypeHeaderValue(mediaType) { CharSet = "utf-8" };
        }

        /// <inheritdoc />
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            int split = Math.Min(7, content.Length);
            await stream.WriteAsync(content.AsMemory(0, split)).ConfigureAwait(false);
            await stream.WriteAsync(content.AsMemory(split)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
