// Module purpose: Defines the provider-neutral, fail-closed control-plane boundary that keeps MOD-12 dormant in normal composition.
namespace DBNotifier.Application.AIOps;

/// <summary>Names the two independent synthetic approval roles required by a future Observer opt-in decision.</summary>
public enum ObserverApprovalRole
{
    /// <summary>Represents the first independent control-plane approver.</summary>
    ControlApproverA = 1,

    /// <summary>Represents the second independent control-plane approver.</summary>
    ControlApproverB = 2,
}

/// <summary>Represents one bounded signature over an exact future Observer opt-in approval.</summary>
public sealed class ObserverApprovalSignature
{
    /// <summary>Maximum signature size accepted at the control-plane boundary.</summary>
    public const int MaximumSignatureBytes = 512;

    /// <summary>Initialises one immutable approval signature.</summary>
    /// <param name="role">Independent role represented by the signing key.</param>
    /// <param name="keyId">Stable non-secret key identifier.</param>
    /// <param name="signature">Bounded detached signature bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for an unsupported role or signature size.</exception>
    /// <exception cref="ArgumentException">Thrown for an invalid key identifier.</exception>
    public ObserverApprovalSignature(
        ObserverApprovalRole role,
        string keyId,
        ReadOnlySpan<byte> signature)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        if (signature.IsEmpty || signature.Length > MaximumSignatureBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(signature));
        }

        Role = role;
        KeyId = ObserverContractGuard.StableIdentifier(keyId, nameof(keyId));
        Signature = Array.AsReadOnly(signature.ToArray());
    }

    /// <summary>Gets the independent approval role.</summary>
    public ObserverApprovalRole Role { get; }

    /// <summary>Gets the non-secret signing-key identifier.</summary>
    public string KeyId { get; }

    /// <summary>Gets a defensive copy of the detached signature.</summary>
    public IReadOnlyList<byte> Signature { get; }
}

/// <summary>
/// Carries an exact, expiring and one-use future Observer opt-in decision without creating an activation path.
/// Authentication and durable consumption belong to an explicitly authorised control-plane implementation.
/// </summary>
public sealed class ObserverOptInApproval
{
    /// <summary>Maximum approval lifetime accepted by the provider-neutral contract.</summary>
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromMinutes(15);

    /// <summary>Initialises one scope-bound approval envelope.</summary>
    /// <param name="approvalId">Unique nonce consumed at most once.</param>
    /// <param name="cellId">Exact approved pilot-cell identifier.</param>
    /// <param name="environment">Exact approved environment identifier.</param>
    /// <param name="purpose">Exact approved purpose identifier.</param>
    /// <param name="revision">Exact immutable configuration revision.</param>
    /// <param name="notBefore">Inclusive UTC start of the approval validity interval.</param>
    /// <param name="expiresAt">Exclusive UTC end of the bounded validity interval.</param>
    /// <param name="signatures">Exactly one signature from each independent approval role.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for an empty identifier, invalid UTC interval or invalid signature count.</exception>
    /// <exception cref="ArgumentException">Thrown for an invalid scope identifier or repeated approval role.</exception>
    public ObserverOptInApproval(
        Guid approvalId,
        string cellId,
        string environment,
        string purpose,
        string revision,
        DateTimeOffset notBefore,
        DateTimeOffset expiresAt,
        IReadOnlyCollection<ObserverApprovalSignature> signatures)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(approvalId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(signatures);
        if (notBefore == default ||
            expiresAt == default ||
            notBefore.Offset != TimeSpan.Zero ||
            expiresAt.Offset != TimeSpan.Zero ||
            expiresAt <= notBefore ||
            expiresAt - notBefore > MaximumLifetime)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt));
        }

        ObserverApprovalSignature[] copiedSignatures = signatures.Take(3).ToArray();
        if (copiedSignatures.Length != 2 ||
            copiedSignatures.Select(item => item.Role).Distinct().Count() != 2)
        {
            throw new ArgumentException(
                "Observer opt-in approvals require exactly one signature from each independent role.",
                nameof(signatures));
        }

        ApprovalId = approvalId;
        CellId = BoundedScopeIdentifier(cellId, nameof(cellId));
        Environment = ObserverContractGuard.StableIdentifier(environment, nameof(environment));
        Purpose = ObserverContractGuard.StableIdentifier(purpose, nameof(purpose));
        Revision = ObserverContractGuard.StableIdentifier(revision, nameof(revision));
        NotBefore = notBefore;
        ExpiresAt = expiresAt;
        Signatures = Array.AsReadOnly(
            copiedSignatures
                .OrderBy(item => item.Role)
                .ToArray());
    }

    /// <summary>Gets the one-use approval nonce.</summary>
    public Guid ApprovalId { get; }

    /// <summary>Gets the exact approved pilot-cell identifier.</summary>
    public string CellId { get; }

    /// <summary>Gets the exact approved environment identifier.</summary>
    public string Environment { get; }

    /// <summary>Gets the exact approved purpose identifier.</summary>
    public string Purpose { get; }

    /// <summary>Gets the immutable approved configuration revision.</summary>
    public string Revision { get; }

    /// <summary>Gets the inclusive UTC start of the validity interval.</summary>
    public DateTimeOffset NotBefore { get; }

    /// <summary>Gets the exclusive UTC end of the validity interval.</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Gets the two independent detached signatures in stable role order.</summary>
    public IReadOnlyList<ObserverApprovalSignature> Signatures { get; }

    /// <summary>Validates an exact approved scope identifier while preserving its case-sensitive canonical spelling.</summary>
    /// <param name="value">Candidate scope identifier.</param>
    /// <param name="parameterName">Public parameter name used by validation failures.</param>
    /// <returns>The validated unchanged identifier.</returns>
    /// <exception cref="ArgumentException">Thrown for an empty, oversized or unsafe identifier.</exception>
    private static string BoundedScopeIdentifier(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > 128 ||
            value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '-' and not '_' and not '.'))
        {
            throw new ArgumentException(
                "Observer scope identifiers must be bounded ASCII identifiers.",
                parameterName);
        }

        return value;
    }
}

/// <summary>Reports whether an approval was authenticated without granting activation or publication authority.</summary>
public sealed class ObserverApprovalAuthentication
{
    /// <summary>Initialises one immutable authentication result.</summary>
    /// <param name="authenticated">Whether both independent signatures were authenticated.</param>
    /// <param name="code">Stable sanitised result code.</param>
    public ObserverApprovalAuthentication(bool authenticated, string code)
    {
        Authenticated = authenticated;
        Code = ObserverContractGuard.StableIdentifier(code, nameof(code));
    }

    /// <summary>Gets whether authentication succeeded.</summary>
    public bool Authenticated { get; }

    /// <summary>Gets the stable sanitised result code.</summary>
    public string Code { get; }

    /// <summary>Creates a fail-closed authentication refusal with a sanitised stable code.</summary>
    /// <param name="code">Stable refusal code.</param>
    /// <returns>A non-authorising refusal.</returns>
    public static ObserverApprovalAuthentication Refused(string code) =>
        new(false, ObserverContractGuard.StableIdentifier(code, nameof(code)));
}

/// <summary>Authenticates a bounded opt-in envelope; normal composition deliberately supplies an unavailable authority.</summary>
public interface IObserverActivationAuthority
{
    /// <summary>Authenticates one approval without consuming it or changing activation state.</summary>
    /// <param name="approval">Exact scope-bound approval.</param>
    /// <param name="cancellationToken">Cancellation observed before authentication.</param>
    /// <returns>An authenticated result or a sanitised fail-closed refusal.</returns>
    ValueTask<ObserverApprovalAuthentication> AuthenticateAsync(
        ObserverOptInApproval approval,
        CancellationToken cancellationToken = default);
}

/// <summary>Fails every approval closed because normal DB-Notifier composition has no activation trust material.</summary>
public sealed class UnavailableObserverActivationAuthority : IObserverActivationAuthority
{
    /// <inheritdoc />
    public ValueTask<ObserverApprovalAuthentication> AuthenticateAsync(
        ObserverOptInApproval approval,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(approval);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(
            ObserverApprovalAuthentication.Refused("aiops.observer.activation.authority_unavailable"));
    }
}

/// <summary>Reports the immutable state and zero-work counters exposed by the dormant normal control plane.</summary>
public sealed class ObserverControlPlaneStatus
{
    /// <summary>Initialises one complete dormant status snapshot.</summary>
    /// <param name="activationState">Current activation state.</param>
    /// <param name="killSwitchEngaged">Whether admission is blocked.</param>
    /// <param name="quarantined">Whether continuity is quarantined.</param>
    /// <param name="evaluationStartCount">Count of evaluation starts.</param>
    /// <param name="publicationCount">Count of publications.</param>
    /// <param name="code">Stable sanitised state code.</param>
    public ObserverControlPlaneStatus(
        ObserverActivationState activationState,
        bool killSwitchEngaged,
        bool quarantined,
        long evaluationStartCount,
        long publicationCount,
        string code)
    {
        ActivationState = activationState;
        KillSwitchEngaged = killSwitchEngaged;
        Quarantined = quarantined;
        EvaluationStartCount = evaluationStartCount;
        PublicationCount = publicationCount;
        Code = ObserverContractGuard.StableIdentifier(code, nameof(code));
    }

    /// <summary>Gets the current activation state.</summary>
    public ObserverActivationState ActivationState { get; }

    /// <summary>Gets whether all admissions are blocked.</summary>
    public bool KillSwitchEngaged { get; }

    /// <summary>Gets whether continuity has entered quarantine.</summary>
    public bool Quarantined { get; }

    /// <summary>Gets the number of evaluation starts.</summary>
    public long EvaluationStartCount { get; }

    /// <summary>Gets the number of publications.</summary>
    public long PublicationCount { get; }

    /// <summary>Gets the stable sanitised state code.</summary>
    public string Code { get; }
}

/// <summary>Reports a control-plane admission refusal without exposing evaluator or publisher capabilities.</summary>
public sealed class ObserverControlAdmission
{
    /// <summary>Initialises one complete non-authorising admission result.</summary>
    /// <param name="admitted">Whether control admission succeeded.</param>
    /// <param name="activationState">Activation state after the decision.</param>
    /// <param name="mayEvaluate">Whether the result grants evaluation authority.</param>
    /// <param name="mayPublish">Whether the result grants publication authority.</param>
    /// <param name="fence">Monotonic fence, or zero for refusal.</param>
    /// <param name="code">Stable sanitised result code.</param>
    public ObserverControlAdmission(
        bool admitted,
        ObserverActivationState activationState,
        bool mayEvaluate,
        bool mayPublish,
        long fence,
        string code)
    {
        Admitted = admitted;
        ActivationState = activationState;
        MayEvaluate = mayEvaluate;
        MayPublish = mayPublish;
        Fence = fence;
        Code = ObserverContractGuard.StableIdentifier(code, nameof(code));
    }

    /// <summary>Gets whether control admission succeeded.</summary>
    public bool Admitted { get; }

    /// <summary>Gets the activation state after the decision.</summary>
    public ObserverActivationState ActivationState { get; }

    /// <summary>Gets whether evaluation is authorised.</summary>
    public bool MayEvaluate { get; }

    /// <summary>Gets whether publication is authorised.</summary>
    public bool MayPublish { get; }

    /// <summary>Gets the monotonic fence, or zero for refusal.</summary>
    public long Fence { get; }

    /// <summary>Gets the stable sanitised result code.</summary>
    public string Code { get; }
}

/// <summary>Defines the provider-neutral control-plane surface available to normal composition.</summary>
public interface IObserverControlPlane
{
    /// <summary>Returns the current non-authorising control-plane status.</summary>
    /// <returns>A complete status snapshot.</returns>
    ObserverControlPlaneStatus GetStatus();

    /// <summary>Attempts one bounded admission without ever changing the normal inactive state.</summary>
    /// <param name="approval">Approval envelope that normal composition must refuse.</param>
    /// <param name="deadline">Exclusive absolute UTC deadline.</param>
    /// <param name="cancellationToken">Cancellation observed before returning.</param>
    /// <returns>A fail-closed admission result.</returns>
    ValueTask<ObserverControlAdmission> TryAdmitAsync(
        ObserverOptInApproval approval,
        DateTimeOffset deadline,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implements the only normal MOD-12 control plane: an inert boundary that remains at
/// <see cref="ObserverActivationState.None"/> and owns no evaluator, publisher, store or background work.
/// </summary>
public sealed class DormantObserverControlPlane : IObserverControlPlane
{
    private readonly IObserverActivationAuthority activationAuthority;

    /// <summary>Initialises the dormant coordinator with the deliberately unavailable normal authority.</summary>
    /// <param name="activationAuthority">Authority used only to preserve an explicit dependency boundary.</param>
    public DormantObserverControlPlane(IObserverActivationAuthority activationAuthority)
    {
        this.activationAuthority = activationAuthority ?? throw new ArgumentNullException(nameof(activationAuthority));
    }

    /// <inheritdoc />
    public ObserverControlPlaneStatus GetStatus() =>
        new(
            ObserverActivationState.None,
            killSwitchEngaged: true,
            quarantined: false,
            evaluationStartCount: 0,
            publicationCount: 0,
            "aiops.observer.activation.none");

    /// <inheritdoc />
    public ValueTask<ObserverControlAdmission> TryAdmitAsync(
        ObserverOptInApproval approval,
        DateTimeOffset deadline,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(approval);
        if (deadline == default || deadline.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Observer control deadlines must be explicit UTC values.", nameof(deadline));
        }

        cancellationToken.ThrowIfCancellationRequested();
        _ = activationAuthority;
        return ValueTask.FromResult(
            new ObserverControlAdmission(
                admitted: false,
                ObserverActivationState.None,
                mayEvaluate: false,
                mayPublish: false,
                fence: 0,
                "aiops.observer.activation.none"));
    }
}
