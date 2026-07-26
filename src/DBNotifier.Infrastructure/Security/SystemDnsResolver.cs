// Module purpose: Resolves DNS through a bounded injectable boundary without making resolver results authoritative.
using System.Net;

namespace DBNotifier.Infrastructure.Security;

/// <summary>Abstracts DNS so policy evaluation can be validated without performing external resolution.</summary>
internal interface IDnsResolver
{
    /// <summary>Resolves one validated hostname while honouring the supplied bounded cancellation token.</summary>
    /// <param name="host">Validated DNS hostname.</param>
    /// <param name="cancellationToken">Combined caller and resolver deadline.</param>
    /// <returns>The untrusted addresses returned by the resolver.</returns>
    ValueTask<IReadOnlyList<IPAddress>> ResolveAsync(
        string host,
        CancellationToken cancellationToken);
}

/// <summary>Adapts the operating-system DNS resolver behind the bounded policy seam.</summary>
internal sealed class SystemDnsResolver : IDnsResolver
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<IPAddress>> ResolveAsync(
        string host,
        CancellationToken cancellationToken) =>
        await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
}
