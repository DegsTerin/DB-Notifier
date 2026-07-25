// Module purpose: Verifies that normal server composition exposes only the inactive MOD-12 control plane and unavailable authority.
using DBNotifier.Application.AIOps;
using DBNotifier.Server.Api;
using Microsoft.Extensions.DependencyInjection;

namespace DBNotifier.UnitTests;

/// <summary>Protects the zero-work and zero-publication guarantees of the normal Observer control-plane boundary.</summary>
public sealed class ObserverControlPlaneTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Confirms normal composition resolves only a dormant coordinator and unavailable authority.</summary>
    [Fact]
    public async Task NormalCompositionRemainsNoneAndCannotAdmitEvaluateOrPublish()
    {
        ServiceCollection registrations = new();
        registrations.AddDormantObserverControlPlane();
        await using ServiceProvider services = registrations.BuildServiceProvider();
        IObserverControlPlane controlPlane = services.GetRequiredService<IObserverControlPlane>();
        IObserverActivationAuthority authority = services.GetRequiredService<IObserverActivationAuthority>();
        ObserverOptInApproval approval = Approval();

        ObserverApprovalAuthentication authentication = await authority.AuthenticateAsync(approval);
        ObserverControlAdmission admission = await controlPlane.TryAdmitAsync(approval, Now.AddSeconds(1));
        ObserverControlPlaneStatus status = controlPlane.GetStatus();

        Assert.IsType<DormantObserverControlPlane>(controlPlane);
        Assert.IsType<UnavailableObserverActivationAuthority>(authority);
        Assert.False(authentication.Authenticated);
        Assert.Equal("aiops.observer.activation.authority_unavailable", authentication.Code);
        Assert.False(admission.Admitted);
        Assert.Equal(ObserverActivationState.None, admission.ActivationState);
        Assert.False(admission.MayEvaluate);
        Assert.False(admission.MayPublish);
        Assert.Equal(0, admission.Fence);
        Assert.Equal(ObserverActivationState.None, status.ActivationState);
        Assert.True(status.KillSwitchEngaged);
        Assert.Equal(0, status.EvaluationStartCount);
        Assert.Equal(0, status.PublicationCount);
        Assert.Equal(2, registrations.Count);
        Assert.DoesNotContain(
            typeof(DormantObserverControlPlane).GetFields(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic),
            field => field.FieldType != typeof(IObserverActivationAuthority));
    }

    /// <summary>Confirms approvals are bounded, dual-controlled and exact even though normal composition rejects them.</summary>
    [Fact]
    public void ApprovalContractRejectsMissingRoleExcessLifetimeAndUnsafeScope()
    {
        ObserverApprovalSignature first = Signature(ObserverApprovalRole.ControlApproverA, "control-key-a");
        ObserverApprovalSignature second = Signature(ObserverApprovalRole.ControlApproverB, "control-key-b");

        Assert.Throws<ArgumentException>(() => new ObserverOptInApproval(
            Guid.NewGuid(),
            "OBS-PILOT-PG16-LOCAL-001",
            "local-laboratory",
            "read-only-observation",
            "revision-1",
            Now,
            Now.AddMinutes(1),
            [first, first]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ObserverOptInApproval(
            Guid.NewGuid(),
            "OBS-PILOT-PG16-LOCAL-001",
            "local-laboratory",
            "read-only-observation",
            "revision-1",
            Now,
            Now.Add(ObserverOptInApproval.MaximumLifetime).AddTicks(1),
            [first, second]));
        Assert.Throws<ArgumentException>(() => new ObserverOptInApproval(
            Guid.NewGuid(),
            "pilot with spaces",
            "local-laboratory",
            "read-only-observation",
            "revision-1",
            Now,
            Now.AddMinutes(1),
            [first, second]));
    }

    /// <summary>Creates one structurally valid but deliberately unauthorised approval fixture.</summary>
    /// <returns>Bounded approval fixture.</returns>
    private static ObserverOptInApproval Approval() =>
        new(
            Guid.Parse("aa311482-30bb-44f8-94ae-50cc19b0124e"),
            "OBS-PILOT-PG16-LOCAL-001",
            "local-laboratory",
            "read-only-observation",
            "revision-1",
            Now,
            Now.AddMinutes(1),
            [
                Signature(ObserverApprovalRole.ControlApproverA, "control-key-a"),
                Signature(ObserverApprovalRole.ControlApproverB, "control-key-b"),
            ]);

    /// <summary>Creates a bounded non-secret signature placeholder for contract validation.</summary>
    /// <param name="role">Independent approval role.</param>
    /// <param name="keyId">Synthetic key identifier.</param>
    /// <returns>Signature placeholder.</returns>
    private static ObserverApprovalSignature Signature(ObserverApprovalRole role, string keyId) =>
        new(role, keyId, new byte[32]);
}
