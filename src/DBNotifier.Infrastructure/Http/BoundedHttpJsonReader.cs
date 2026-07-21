// Module purpose: Provides one bounded, strict JSON response reader for every DB-Notifier HTTP client transport.
using System.Net.Http.Headers;
using System.Text.Json;

namespace DBNotifier.Infrastructure.Http;

/// <summary>Classifies bounded HTTP JSON refusal without retaining response content.</summary>
public enum BoundedHttpJsonFailure
{
    /// <summary>The response media type or character set was not admitted.</summary>
    ContentTypeInvalid,

    /// <summary>The declared or streamed response exceeded its exact byte ceiling.</summary>
    TooLarge,

    /// <summary>The response contained no JSON bytes.</summary>
    Empty,
}

/// <summary>Reports one sanitised bounded-reader refusal without embedding untrusted response material.</summary>
/// <param name="failure">Typed refusal category.</param>
public sealed class BoundedHttpJsonException(BoundedHttpJsonFailure failure)
    : IOException("The HTTP JSON response is outside the admitted transport envelope.")
{
    /// <summary>Gets the typed failure category.</summary>
    public BoundedHttpJsonFailure Failure { get; } = failure;
}

/// <summary>
/// Reads untrusted HTTP response content under an exact media type and byte ceiling before strict JSON materialisation.
/// Content-Length is only an early refusal; the streamed byte count remains authoritative.
/// </summary>
public static class BoundedHttpJsonReader
{
    /// <summary>Reads a JSON response into one typed value without allowing an unbounded response allocation.</summary>
    /// <typeparam name="T">Expected response contract.</typeparam>
    /// <param name="content">Untrusted HTTP content.</param>
    /// <param name="maximumBytes">Positive maximum number of response bytes.</param>
    /// <param name="serializerOptions">Strict serializer options, including the contract depth and unknown-member policy.</param>
    /// <param name="allowedMediaTypes">Exact JSON media types admitted by the selected protocol path.</param>
    /// <param name="cancellationToken">Cancellation propagated to every stream read.</param>
    /// <returns>The required typed response.</returns>
    /// <exception cref="BoundedHttpJsonException">Thrown for an invalid content type, byte ceiling or empty response.</exception>
    /// <exception cref="JsonException">Thrown when JSON or the typed contract is invalid.</exception>
    public static async ValueTask<T> ReadAsync<T>(
        HttpContent content,
        int maximumBytes,
        JsonSerializerOptions serializerOptions,
        IReadOnlySet<string> allowedMediaTypes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serializerOptions);
        byte[] bytes = await ReadBytesAsync(
            content,
            maximumBytes,
            allowedMediaTypes,
            cancellationToken).ConfigureAwait(false);
        if (bytes.Length == 0)
        {
            throw new BoundedHttpJsonException(BoundedHttpJsonFailure.Empty);
        }

        return JsonSerializer.Deserialize<T>(bytes, serializerOptions) ??
            throw new BoundedHttpJsonException(BoundedHttpJsonFailure.Empty);
    }

    /// <summary>Reads exact response bytes after enforcing media type and streamed byte-count limits.</summary>
    /// <param name="content">Untrusted HTTP content.</param>
    /// <param name="maximumBytes">Positive maximum number of bytes.</param>
    /// <param name="allowedMediaTypes">Exact admitted JSON media types.</param>
    /// <param name="cancellationToken">Cancellation propagated to every stream read.</param>
    /// <returns>A caller-owned byte array no larger than the configured ceiling.</returns>
    /// <exception cref="BoundedHttpJsonException">Thrown when headers or streamed content exceed policy.</exception>
    public static async ValueTask<byte[]> ReadBytesAsync(
        HttpContent content,
        int maximumBytes,
        IReadOnlySet<string> allowedMediaTypes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumBytes, 1);
        ArgumentNullException.ThrowIfNull(allowedMediaTypes);
        ValidateContentType(content.Headers.ContentType, allowedMediaTypes);
        if (content.Headers.ContentLength is long declaredLength &&
            (declaredLength < 0 || declaredLength > maximumBytes))
        {
            throw new BoundedHttpJsonException(BoundedHttpJsonFailure.TooLarge);
        }

        await using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using MemoryStream buffer = new(Math.Min(maximumBytes, 64 * 1024));
        byte[] chunk = new byte[Math.Min(maximumBytes, 16 * 1024)];
        int total = 0;
        while (true)
        {
            int read = await stream.ReadAsync(chunk.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return buffer.ToArray();
            }

            try
            {
                total = checked(total + read);
            }
            catch (OverflowException)
            {
                throw new BoundedHttpJsonException(BoundedHttpJsonFailure.TooLarge);
            }

            if (total > maximumBytes)
            {
                throw new BoundedHttpJsonException(BoundedHttpJsonFailure.TooLarge);
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ValidateContentType(
        MediaTypeHeaderValue? contentType,
        IReadOnlySet<string> allowedMediaTypes)
    {
        string? mediaType = contentType?.MediaType;
        string? characterSet = contentType?.CharSet;
        if (mediaType is null || !allowedMediaTypes.Contains(mediaType) ||
            (characterSet is not null && !string.Equals(characterSet, "utf-8", StringComparison.OrdinalIgnoreCase)))
        {
            throw new BoundedHttpJsonException(BoundedHttpJsonFailure.ContentTypeInvalid);
        }
    }
}
