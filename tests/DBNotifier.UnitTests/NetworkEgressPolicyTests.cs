// Module purpose: Verifies immutable CIDR admission, DNS rebinding resistance and sanitised network-egress failures.
using System.Net;
using System.Net.Sockets;
using DBNotifier.Infrastructure.Security;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.UnitTests;

/// <summary>Protects the provider-neutral network-egress policy without using operating-system DNS or sockets.</summary>
public sealed class NetworkEgressPolicyTests
{
    [Fact]
    public async Task CompiledPolicyDoesNotObserveLaterOptionMutation()
    {
        NetworkEgressOptions options = Options(["10.0.0.0/8"], [], [5432]);
        NetworkEgressPolicySet policySet = NetworkEgressPolicySet.Compile(options);
        options.Policies["test-policy"].AllowedCidrs.Clear();
        options.Policies["test-policy"].AllowedCidrs.Add("192.168.0.0/16");
        StubDnsResolver resolver = new([Addresses("10.20.30.40")]);
        NetworkEgressAuthorizer authorizer = new(policySet, resolver);

        NetworkEgressResolution result = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", "database.example.test", 5432),
            CancellationToken.None);

        Assert.True(result.IsApproved);
        Assert.Equal(IPAddress.Parse("10.20.30.40"), Assert.Single(result.Addresses));
    }

    [Theory]
    [InlineData("10.0.0.1/8")]
    [InlineData("10.0.0.0/33")]
    [InlineData("::ffff:192.0.2.0/95")]
    [InlineData("192.0.2.0")]
    [InlineData(" 192.0.2.0/24")]
    public void CompilationRejectsAmbiguousOrNonCanonicalCidr(string cidr)
    {
        NetworkEgressOptions options = Options([cidr], [], [443]);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => NetworkEgressPolicySet.Compile(options));

        Assert.Equal(NetworkEgressFailureCodes.DestinationInvalid, exception.Message);
    }

    [Fact]
    public async Task ExplicitDenyWinsOverAnAllowingSupernet()
    {
        NetworkEgressPolicySet policySet = NetworkEgressPolicySet.Compile(
            Options(["10.0.0.0/8"], ["10.20.0.0/16"], [5432]));
        NetworkEgressAuthorizer authorizer = new(
            policySet,
            new StubDnsResolver([Addresses("10.20.30.40")]));

        NetworkEgressResolution result = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", "database.example.test", 5432),
            CancellationToken.None);

        Assert.False(result.IsApproved);
        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, result.FailureCode);
        Assert.Empty(result.Addresses);
    }

    [Fact]
    public async Task MappedIpv4CidrAndAnswerAreNormalisedBeforeAdmission()
    {
        NetworkEgressPolicySet policySet = NetworkEgressPolicySet.Compile(
            Options(["::ffff:192.0.2.0/120"], [], [443]));
        IPAddress mapped = IPAddress.Parse("::ffff:192.0.2.42");
        NetworkEgressAuthorizer authorizer = new(
            policySet,
            new StubDnsResolver([[mapped]]));

        NetworkEgressResolution result = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", "service.example.test", 443),
            CancellationToken.None);

        Assert.True(result.IsApproved);
        IPAddress approved = Assert.Single(result.Addresses);
        Assert.Equal(AddressFamily.InterNetwork, approved.AddressFamily);
        Assert.Equal(IPAddress.Parse("192.0.2.42"), approved);
    }

    [Theory]
    [InlineData("0.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.100.100.200")]
    [InlineData("168.63.129.16")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    [InlineData("ff02::1")]
    [InlineData("fd00:ec2::254")]
    public async Task HardDeniedDestinationsCannotBeOverriddenByBroadAllowlist(string address)
    {
        NetworkEgressPolicySet policySet = NetworkEgressPolicySet.Compile(
            Options(["0.0.0.0/0", "::/0"], [], [443]));
        NetworkEgressAuthorizer authorizer = new(policySet, new UnexpectedDnsResolver());

        NetworkEgressResolution result = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", address, 443),
            CancellationToken.None);

        Assert.False(result.IsApproved);
        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, result.FailureCode);
    }

    [Fact]
    public async Task PrivateAndLoopbackAddressesRequireAnExplicitPositiveRule()
    {
        NetworkEgressPolicySet publicOnly = NetworkEgressPolicySet.Compile(
            Options(["203.0.113.0/24"], [], [443]));
        NetworkEgressResolution denied = await new NetworkEgressAuthorizer(
                publicOnly,
                new StubDnsResolver([Addresses("127.0.0.1")]))
            .ResolveAndAuthoriseAsync(
                new NetworkEgressRequest("test-policy", "service.example.test", 443),
                CancellationToken.None);

        NetworkEgressPolicySet loopbackAllowed = NetworkEgressPolicySet.Compile(
            Options(["127.0.0.0/8"], [], [443]));
        NetworkEgressResolution approved = await new NetworkEgressAuthorizer(
                loopbackAllowed,
                new StubDnsResolver([Addresses("127.0.0.1")]))
            .ResolveAndAuthoriseAsync(
                new NetworkEgressRequest("test-policy", "service.example.test", 443),
                CancellationToken.None);

        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, denied.FailureCode);
        Assert.True(approved.IsApproved);
    }

    [Fact]
    public async Task OneForbiddenDnsAnswerDeniesTheCompleteMixedResponse()
    {
        NetworkEgressPolicySet policySet = NetworkEgressPolicySet.Compile(
            Options(["203.0.113.0/24"], [], [443]));
        NetworkEgressAuthorizer authorizer = new(
            policySet,
            new StubDnsResolver([Addresses("203.0.113.8", "169.254.169.254")]));

        NetworkEgressResolution result = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", "mixed.example.test", 443),
            CancellationToken.None);

        Assert.False(result.IsApproved);
        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, result.FailureCode);
    }

    [Fact]
    public async Task ScopedIpv6DnsAnswerIsRejectedBeforeItsScopeCanBeDiscarded()
    {
        NetworkEgressPolicySet policySet = NetworkEgressPolicySet.Compile(
            Options(["2001:db8::/32"], [], [443]));
        IPAddress scopedAddress = new(
            IPAddress.Parse("2001:db8::8").GetAddressBytes(),
            scopeid: 7);
        NetworkEgressAuthorizer authorizer = new(
            policySet,
            new StubDnsResolver([[scopedAddress]]));

        NetworkEgressResolution result = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", "scoped.example.test", 443),
            CancellationToken.None);

        Assert.False(result.IsApproved);
        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, result.FailureCode);
    }

    [Fact]
    public async Task EmptyAndOversizedDnsResponsesFailWithinTheAnswerBound()
    {
        NetworkEgressOptions options = Options(["203.0.113.0/24"], [], [443]);
        options.Policies["test-policy"].MaximumResolvedAddresses = 1;
        NetworkEgressPolicySet policySet = NetworkEgressPolicySet.Compile(options);
        StubDnsResolver resolver = new(
        [
            Array.Empty<IPAddress>(),
            Addresses("203.0.113.8", "203.0.113.9"),
        ]);
        NetworkEgressAuthorizer authorizer = new(policySet, resolver);

        NetworkEgressResolution empty = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", "empty.example.test", 443),
            CancellationToken.None);
        NetworkEgressResolution oversized = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", "large.example.test", 443),
            CancellationToken.None);

        Assert.Equal(NetworkEgressFailureCodes.DnsAnswerLimit, empty.FailureCode);
        Assert.Equal(NetworkEgressFailureCodes.DnsAnswerLimit, oversized.FailureCode);
    }

    [Fact]
    public async Task ResolverDeadlineReturnsOnlyTheSanitisedTimeoutCode()
    {
        NetworkEgressOptions options = Options(["203.0.113.0/24"], [], [443]);
        options.Policies["test-policy"].DnsTimeoutSeconds = 1;
        NetworkEgressAuthorizer authorizer = new(
            NetworkEgressPolicySet.Compile(options),
            new BlockingDnsResolver());

        NetworkEgressResolution result = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", "slow.example.test", 443),
            CancellationToken.None);

        Assert.Equal(NetworkEgressFailureCodes.DnsTimeout, result.FailureCode);
    }

    [Fact]
    public async Task CallerCancellationRemainsDistinctFromResolverTimeout()
    {
        NetworkEgressAuthorizer authorizer = new(
            NetworkEgressPolicySet.Compile(Options(["203.0.113.0/24"], [], [443])),
            new BlockingDnsResolver());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await authorizer.ResolveAndAuthoriseAsync(
                new NetworkEgressRequest("test-policy", "cancelled.example.test", 443),
                cancellation.Token));
    }

    [Fact]
    public async Task ResolverFailureDoesNotExposeTheExceptionOrDestination()
    {
        NetworkEgressAuthorizer authorizer = new(
            NetworkEgressPolicySet.Compile(Options(["203.0.113.0/24"], [], [443])),
            new ThrowingDnsResolver());

        NetworkEgressResolution result = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", "secret-host.example.test", 443),
            CancellationToken.None);

        Assert.Equal(NetworkEgressFailureCodes.DnsFailed, result.FailureCode);
        Assert.DoesNotContain("secret-host", result.FailureCode, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EachAdmissionUsesAFreshDnsAnswerAndRejectsARebind()
    {
        StubDnsResolver resolver = new(
        [
            Addresses("203.0.113.8"),
            Addresses("169.254.169.254"),
        ]);
        NetworkEgressAuthorizer authorizer = new(
            NetworkEgressPolicySet.Compile(Options(["203.0.113.0/24"], [], [443])),
            resolver);

        NetworkEgressResolution first = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", "changing.example.test", 443),
            CancellationToken.None);
        NetworkEgressResolution second = await authorizer.ResolveAndAuthoriseAsync(
            new NetworkEgressRequest("test-policy", "changing.example.test", 443),
            CancellationToken.None);

        Assert.True(first.IsApproved);
        Assert.Equal(NetworkEgressFailureCodes.AddressDenied, second.FailureCode);
        Assert.Equal(2, resolver.CallCount);
    }

    [Fact]
    public void RequiredPolicyDistinguishesMissingAndDeniedPorts()
    {
        NetworkEgressPolicySet policySet = NetworkEgressPolicySet.Compile(
            Options(["203.0.113.0/24"], [], [443]));

        InvalidOperationException missing = Assert.Throws<InvalidOperationException>(
            () => policySet.Require("missing-policy", 443));
        InvalidOperationException denied = Assert.Throws<InvalidOperationException>(
            () => policySet.Require("test-policy", 8443));
        policySet.Require("test-policy", 443);

        Assert.Equal(NetworkEgressFailureCodes.PolicyUnavailable, missing.Message);
        Assert.Equal(NetworkEgressFailureCodes.PortDenied, denied.Message);
    }

    private static NetworkEgressOptions Options(
        IReadOnlyList<string> allowed,
        IReadOnlyList<string> denied,
        IReadOnlyList<int> ports) =>
        new()
        {
            Policies = new Dictionary<string, NetworkEgressPolicyOptions>(StringComparer.Ordinal)
            {
                ["test-policy"] = new()
                {
                    AllowedCidrs = [.. allowed],
                    DeniedCidrs = [.. denied],
                    AllowedPorts = [.. ports],
                },
            },
        };

    private static IPAddress[] Addresses(params string[] values) =>
        values.Select(IPAddress.Parse).ToArray();

    private sealed class StubDnsResolver(IEnumerable<IReadOnlyList<IPAddress>> responses) : IDnsResolver
    {
        private readonly Queue<IReadOnlyList<IPAddress>> responses = new(responses);

        internal int CallCount { get; private set; }

        public ValueTask<IReadOnlyList<IPAddress>> ResolveAsync(
            string host,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return ValueTask.FromResult(responses.Dequeue());
        }
    }

    private sealed class BlockingDnsResolver : IDnsResolver
    {
        public async ValueTask<IReadOnlyList<IPAddress>> ResolveAsync(
            string host,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Array.Empty<IPAddress>();
        }
    }

    private sealed class ThrowingDnsResolver : IDnsResolver
    {
        public ValueTask<IReadOnlyList<IPAddress>> ResolveAsync(
            string host,
            CancellationToken cancellationToken) =>
            throw new SocketException((int)SocketError.HostNotFound);
    }

    private sealed class UnexpectedDnsResolver : IDnsResolver
    {
        public ValueTask<IReadOnlyList<IPAddress>> ResolveAsync(
            string host,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("DNS must not run for an IP literal.");
    }
}
