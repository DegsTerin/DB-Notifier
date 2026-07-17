// Module purpose: Defines bounded, provenance-aware offline MOD-12 evaluation datasets and deterministic execution without persistence, network access or mode promotion.
namespace DBNotifier.Application.AIOps;

/// <summary>Distinguishes ordinary reference cases from deliberately hostile data-quality cases.</summary>
public enum ObserverOfflineCaseKind
{
    /// <summary>A governed reference case used to measure deterministic detection quality.</summary>
    Reference = 1,

    /// <summary>A deliberately invalid or unauthorised case used to test data-poisoning resistance.</summary>
    Adversarial = 2,
}

/// <summary>Defines the exact offline outcome asserted by one reference case.</summary>
public enum ObserverOfflineExpectedDisposition
{
    /// <summary>The data boundary must reject the case before analysis.</summary>
    AdaptationRejected = 1,

    /// <summary>The deterministic threshold must report a detected condition.</summary>
    FindingDetected = 2,

    /// <summary>The deterministic threshold must report that the condition was not detected.</summary>
    FindingNotDetected = 3,

    /// <summary>The deterministic threshold must report insufficient evidence.</summary>
    InsufficientEvidence = 4,
}

/// <summary>Identifies one provider, version and platform segment without adding provider rules to the neutral core.</summary>
public sealed class ObserverOfflineEvaluationSegment
{
    /// <summary>Initialises one stable offline evaluation segment.</summary>
    /// <param name="providerType">Stable provider identifier used only for result segmentation.</param>
    /// <param name="providerVersion">Stable provider-version identifier used only for result segmentation.</param>
    /// <param name="platform">Stable platform identifier used only for result segmentation.</param>
    /// <exception cref="ArgumentNullException">Thrown when an identifier is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an identifier is empty, unsafe or excessive.</exception>
    public ObserverOfflineEvaluationSegment(string providerType, string providerVersion, string platform)
    {
        ProviderType = ObserverContractGuard.StableIdentifier(providerType, nameof(providerType));
        ProviderVersion = ObserverContractGuard.StableIdentifier(providerVersion, nameof(providerVersion));
        Platform = ObserverContractGuard.StableIdentifier(platform, nameof(platform));
    }

    /// <summary>Gets the provider segment identifier without implying homologation or support.</summary>
    public string ProviderType { get; }

    /// <summary>Gets the provider-version segment identifier.</summary>
    public string ProviderVersion { get; }

    /// <summary>Gets the platform segment identifier.</summary>
    public string Platform { get; }
}

/// <summary>
/// Defines one reproducible offline case containing typed canonical telemetry, an explicit data policy, one
/// deterministic threshold and an exact expected outcome.
/// </summary>
public sealed class ObserverOfflineEvaluationCase
{
    /// <summary>Initialises one bounded and immutable offline evaluation case.</summary>
    /// <param name="caseId">Stable case identifier.</param>
    /// <param name="analysisId">Non-empty reproducible analysis identifier.</param>
    /// <param name="kind">Reference or adversarial case kind.</param>
    /// <param name="segment">Provider/version/platform result segment.</param>
    /// <param name="dataPolicy">Explicit offline data policy evaluated by the trusted adapter.</param>
    /// <param name="analysisAt">Trusted UTC case instant.</param>
    /// <param name="telemetry">One to the analysis sample bound canonical telemetry envelopes.</param>
    /// <param name="thresholdRule">Single deterministic threshold evaluated after adaptation.</param>
    /// <param name="expectedDisposition">Exact expected boundary or finding disposition.</param>
    /// <param name="expectedCode">Exact stable expected outcome code.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required object or collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown for unsafe time, null entries or repeated evidence identifiers.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for empty identifiers, invalid enums or sample bounds.</exception>
    public ObserverOfflineEvaluationCase(
        string caseId,
        Guid analysisId,
        ObserverOfflineCaseKind kind,
        ObserverOfflineEvaluationSegment segment,
        ObserverDataPolicy dataPolicy,
        DateTimeOffset analysisAt,
        IReadOnlyCollection<ObserverCanonicalHealthTelemetry> telemetry,
        ObserverThresholdRule thresholdRule,
        ObserverOfflineExpectedDisposition expectedDisposition,
        string expectedCode)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(analysisId, Guid.Empty);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (!Enum.IsDefined(expectedDisposition))
        {
            throw new ArgumentOutOfRangeException(nameof(expectedDisposition));
        }

        ArgumentNullException.ThrowIfNull(segment);
        ArgumentNullException.ThrowIfNull(dataPolicy);
        if (dataPolicy.DataUse != ObserverDataUse.OfflineEvaluation)
        {
            throw new ArgumentException("Offline cases require an offline-evaluation data policy.", nameof(dataPolicy));
        }

        ValidateUtcInstant(analysisAt, nameof(analysisAt));
        ArgumentNullException.ThrowIfNull(telemetry);
        ObserverCanonicalHealthTelemetry?[] boundedTelemetry = telemetry
            .Take(ObserverAnalysisRequest.MaximumSampleCount + 1)
            .ToArray();
        ArgumentOutOfRangeException.ThrowIfLessThan(boundedTelemetry.Length, 1, nameof(telemetry));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            boundedTelemetry.Length,
            ObserverAnalysisRequest.MaximumSampleCount,
            nameof(telemetry));
        if (boundedTelemetry.Any(item => item is null))
        {
            throw new ArgumentException("Offline cases cannot contain null telemetry envelopes.", nameof(telemetry));
        }

        ObserverCanonicalHealthTelemetry[] copiedTelemetry = boundedTelemetry.Select(item => item!).ToArray();
        if (copiedTelemetry
                .Select(item => item.Observation.ObservationId)
                .Distinct()
                .Count() != copiedTelemetry.Length)
        {
            throw new ArgumentException("Offline cases cannot repeat canonical observation identifiers.", nameof(telemetry));
        }

        if (copiedTelemetry.Any(item =>
                !string.Equals(
                    item.Observation.ProviderType.Value,
                    segment.ProviderType,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    item.Observation.ProviderVersion,
                    segment.ProviderVersion,
                    StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Offline telemetry must match its declared provider and version segment.",
                nameof(telemetry));
        }

        ArgumentNullException.ThrowIfNull(thresholdRule);
        CaseId = ObserverContractGuard.StableIdentifier(caseId, nameof(caseId));
        AnalysisId = analysisId;
        Kind = kind;
        Segment = segment;
        DataPolicy = dataPolicy;
        AnalysisAt = analysisAt;
        Telemetry = Array.AsReadOnly(copiedTelemetry
            .OrderBy(item => item.Observation.ObservedAt)
            .ThenBy(item => item.Observation.ObservationId)
            .ToArray());
        ThresholdRule = thresholdRule;
        ExpectedDisposition = expectedDisposition;
        ExpectedCode = ObserverContractGuard.StableIdentifier(expectedCode, nameof(expectedCode));
    }

    /// <summary>Gets the stable case identifier.</summary>
    public string CaseId { get; }

    /// <summary>Gets the reproducible non-empty analysis identifier.</summary>
    public Guid AnalysisId { get; }

    /// <summary>Gets whether the case is reference or adversarial.</summary>
    public ObserverOfflineCaseKind Kind { get; }

    /// <summary>Gets the provider/version/platform reporting segment.</summary>
    public ObserverOfflineEvaluationSegment Segment { get; }

    /// <summary>Gets the explicit purpose-specific data policy.</summary>
    public ObserverDataPolicy DataPolicy { get; }

    /// <summary>Gets the trusted UTC case instant.</summary>
    public DateTimeOffset AnalysisAt { get; }

    /// <summary>Gets the immutable canonical telemetry candidates.</summary>
    public IReadOnlyList<ObserverCanonicalHealthTelemetry> Telemetry { get; }

    /// <summary>Gets the sole deterministic threshold evaluated by the case.</summary>
    public ObserverThresholdRule ThresholdRule { get; }

    /// <summary>Gets the exact expected boundary or finding disposition.</summary>
    public ObserverOfflineExpectedDisposition ExpectedDisposition { get; }

    /// <summary>Gets the exact stable expected outcome code.</summary>
    public string ExpectedCode { get; }

    /// <summary>Rejects default or offset-bearing evaluation instants.</summary>
    /// <param name="value">Instant to validate.</param>
    /// <param name="parameterName">Public parameter name used by any exception.</param>
    /// <exception cref="ArgumentException">Thrown when the instant is default or not expressed as UTC.</exception>
    private static void ValidateUtcInstant(DateTimeOffset value, string parameterName)
    {
        if (value == default || value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Offline evaluation instants must be explicit UTC values.", parameterName);
        }
    }
}

/// <summary>
/// Groups a versioned offline corpus with explicit provenance, authority, classification, expiry and segmentation.
/// The dataset remains caller-owned and is never collected or persisted by MOD-12.
/// </summary>
public sealed class ObserverOfflineEvaluationDataset
{
    /// <summary>Maximum reference and adversarial cases accepted in one bounded evaluation.</summary>
    public const int MaximumCaseCount = 1_000;

    /// <summary>Initialises one immutable governed offline dataset.</summary>
    /// <param name="datasetId">Stable dataset identifier.</param>
    /// <param name="datasetVersion">Stable dataset version.</param>
    /// <param name="provenanceReference">Stable non-secret origin reference.</param>
    /// <param name="authorityReference">Stable non-secret approval or ownership reference.</param>
    /// <param name="classification">Explicit operational-telemetry classification.</param>
    /// <param name="createdAt">UTC dataset creation instant.</param>
    /// <param name="expiresAt">Exclusive UTC dataset expiry.</param>
    /// <param name="cases">One to <see cref="MaximumCaseCount"/> unique offline cases.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cases"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown for invalid time, repeated cases or null entries.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for invalid classification or case bounds.</exception>
    public ObserverOfflineEvaluationDataset(
        string datasetId,
        string datasetVersion,
        string provenanceReference,
        string authorityReference,
        ObserverDataClassification classification,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        IReadOnlyCollection<ObserverOfflineEvaluationCase> cases)
    {
        if (classification != ObserverDataClassification.OperationalTelemetry)
        {
            throw new ArgumentOutOfRangeException(nameof(classification));
        }

        ValidateUtcInstant(createdAt, nameof(createdAt));
        ValidateUtcInstant(expiresAt, nameof(expiresAt));
        if (expiresAt <= createdAt)
        {
            throw new ArgumentException("Offline dataset expiry must follow its creation instant.", nameof(expiresAt));
        }

        ArgumentNullException.ThrowIfNull(cases);
        ObserverOfflineEvaluationCase?[] boundedCases = cases.Take(MaximumCaseCount + 1).ToArray();
        ArgumentOutOfRangeException.ThrowIfLessThan(boundedCases.Length, 1, nameof(cases));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(boundedCases.Length, MaximumCaseCount, nameof(cases));
        if (boundedCases.Any(item => item is null))
        {
            throw new ArgumentException("Offline datasets cannot contain null cases.", nameof(cases));
        }

        ObserverOfflineEvaluationCase[] copiedCases = boundedCases.Select(item => item!).ToArray();
        if (copiedCases.Select(item => item.CaseId).Distinct(StringComparer.Ordinal).Count() != copiedCases.Length)
        {
            throw new ArgumentException("Offline dataset case identifiers must be unique.", nameof(cases));
        }

        DatasetId = ObserverContractGuard.StableIdentifier(datasetId, nameof(datasetId));
        DatasetVersion = ObserverContractGuard.StableIdentifier(datasetVersion, nameof(datasetVersion));
        ProvenanceReference = ObserverContractGuard.StableIdentifier(
            provenanceReference,
            nameof(provenanceReference));
        AuthorityReference = ObserverContractGuard.StableIdentifier(authorityReference, nameof(authorityReference));
        Classification = classification;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        Cases = Array.AsReadOnly(copiedCases.OrderBy(item => item.CaseId, StringComparer.Ordinal).ToArray());
        Segments = Array.AsReadOnly(Cases
            .Select(item => item.Segment)
            .GroupBy(item => (item.ProviderType, item.ProviderVersion, item.Platform))
            .Select(group => group.First())
            .OrderBy(item => item.ProviderType, StringComparer.Ordinal)
            .ThenBy(item => item.ProviderVersion, StringComparer.Ordinal)
            .ThenBy(item => item.Platform, StringComparer.Ordinal)
            .ToArray());
    }

    /// <summary>Gets the stable dataset identifier.</summary>
    public string DatasetId { get; }

    /// <summary>Gets the stable dataset version.</summary>
    public string DatasetVersion { get; }

    /// <summary>Gets the stable non-secret provenance reference.</summary>
    public string ProvenanceReference { get; }

    /// <summary>Gets the stable non-secret authority reference.</summary>
    public string AuthorityReference { get; }

    /// <summary>Gets the dataset's explicit operational-telemetry classification.</summary>
    public ObserverDataClassification Classification { get; }

    /// <summary>Gets the UTC dataset creation instant.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets the exclusive UTC dataset expiry.</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Gets the immutable cases in deterministic identifier order.</summary>
    public IReadOnlyList<ObserverOfflineEvaluationCase> Cases { get; }

    /// <summary>Gets the distinct provider/version/platform segments without support claims.</summary>
    public IReadOnlyList<ObserverOfflineEvaluationSegment> Segments { get; }

    /// <summary>Rejects default or offset-bearing dataset instants.</summary>
    /// <param name="value">Instant to validate.</param>
    /// <param name="parameterName">Public parameter name used by any exception.</param>
    /// <exception cref="ArgumentException">Thrown when the instant is default or not expressed as UTC.</exception>
    private static void ValidateUtcInstant(DateTimeOffset value, string parameterName)
    {
        if (value == default || value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Offline dataset instants must be explicit UTC values.", parameterName);
        }
    }
}

/// <summary>Reports one exact expected-versus-actual offline case comparison without source payload content.</summary>
public sealed class ObserverOfflineEvaluationCaseResult
{
    /// <summary>Initialises one immutable offline case result.</summary>
    /// <param name="evaluationCase">Case metadata and expected outcome.</param>
    /// <param name="actualDisposition">Observed boundary or finding disposition.</param>
    /// <param name="actualCode">Observed stable outcome code.</param>
    internal ObserverOfflineEvaluationCaseResult(
        ObserverOfflineEvaluationCase evaluationCase,
        ObserverOfflineExpectedDisposition actualDisposition,
        string actualCode)
    {
        CaseId = evaluationCase.CaseId;
        Kind = evaluationCase.Kind;
        Segment = evaluationCase.Segment;
        ExpectedDisposition = evaluationCase.ExpectedDisposition;
        ExpectedCode = evaluationCase.ExpectedCode;
        ActualDisposition = actualDisposition;
        ActualCode = actualCode;
        Passed = ExpectedDisposition == ActualDisposition &&
            string.Equals(ExpectedCode, ActualCode, StringComparison.Ordinal);
    }

    /// <summary>Gets the stable case identifier.</summary>
    public string CaseId { get; }

    /// <summary>Gets whether the case is reference or adversarial.</summary>
    public ObserverOfflineCaseKind Kind { get; }

    /// <summary>Gets the result segment.</summary>
    public ObserverOfflineEvaluationSegment Segment { get; }

    /// <summary>Gets the exact expected disposition.</summary>
    public ObserverOfflineExpectedDisposition ExpectedDisposition { get; }

    /// <summary>Gets the exact expected stable code.</summary>
    public string ExpectedCode { get; }

    /// <summary>Gets the actual disposition.</summary>
    public ObserverOfflineExpectedDisposition ActualDisposition { get; }

    /// <summary>Gets the actual stable code.</summary>
    public string ActualCode { get; }

    /// <summary>Gets whether both disposition and code matched exactly.</summary>
    public bool Passed { get; }
}

/// <summary>
/// Summarises one deterministic offline run with case accuracy, binary detection counts and adversarial acceptance.
/// The report grants no runtime or mode authority.
/// </summary>
public sealed class ObserverOfflineEvaluationReport
{
    /// <summary>Initialises one immutable offline report.</summary>
    /// <param name="dataset">Governed dataset being evaluated.</param>
    /// <param name="evaluatedAt">Trusted UTC evaluation instant.</param>
    /// <param name="datasetAccepted">Whether dataset validity permitted case execution.</param>
    /// <param name="code">Stable dataset-level outcome code.</param>
    /// <param name="caseResults">Deterministic case results, or an empty collection for dataset refusal.</param>
    internal ObserverOfflineEvaluationReport(
        ObserverOfflineEvaluationDataset dataset,
        DateTimeOffset evaluatedAt,
        bool datasetAccepted,
        string code,
        IReadOnlyList<ObserverOfflineEvaluationCaseResult> caseResults)
    {
        DatasetId = dataset.DatasetId;
        DatasetVersion = dataset.DatasetVersion;
        ProvenanceReference = dataset.ProvenanceReference;
        AuthorityReference = dataset.AuthorityReference;
        EvaluatedAt = evaluatedAt;
        DatasetAccepted = datasetAccepted;
        Code = code;
        CaseResults = caseResults;
        PassedCaseCount = caseResults.Count(result => result.Passed);
        FailedCaseCount = caseResults.Count - PassedCaseCount;
        RejectedAdversarialCaseCount = caseResults.Count(result =>
            result.Kind == ObserverOfflineCaseKind.Adversarial &&
            result.ActualDisposition == ObserverOfflineExpectedDisposition.AdaptationRejected);
        AcceptedAdversarialCaseCount = caseResults.Count(result =>
            result.Kind == ObserverOfflineCaseKind.Adversarial &&
            result.ActualDisposition != ObserverOfflineExpectedDisposition.AdaptationRejected);

        foreach (ObserverOfflineEvaluationCaseResult result in caseResults)
        {
            bool expectedPositive = result.ExpectedDisposition == ObserverOfflineExpectedDisposition.FindingDetected;
            bool expectedNegative = result.ExpectedDisposition == ObserverOfflineExpectedDisposition.FindingNotDetected;
            bool actualPositive = result.ActualDisposition == ObserverOfflineExpectedDisposition.FindingDetected;
            bool actualNegative = result.ActualDisposition == ObserverOfflineExpectedDisposition.FindingNotDetected;
            if (expectedPositive)
            {
                if (actualPositive)
                {
                    TruePositiveCount++;
                }
                else if (actualNegative)
                {
                    FalseNegativeCount++;
                }
                else
                {
                    UnscoredDetectionCaseCount++;
                }
            }
            else if (expectedNegative)
            {
                if (actualPositive)
                {
                    FalsePositiveCount++;
                }
                else if (actualNegative)
                {
                    TrueNegativeCount++;
                }
                else
                {
                    UnscoredDetectionCaseCount++;
                }
            }
        }

        int precisionDenominator = TruePositiveCount + FalsePositiveCount;
        int recallDenominator = TruePositiveCount + FalseNegativeCount;
        Precision = precisionDenominator == 0 ? null : (double)TruePositiveCount / precisionDenominator;
        Recall = recallDenominator == 0 ? null : (double)TruePositiveCount / recallDenominator;
    }

    /// <summary>Gets the evaluated dataset identifier.</summary>
    public string DatasetId { get; }

    /// <summary>Gets the evaluated dataset version.</summary>
    public string DatasetVersion { get; }

    /// <summary>Gets the dataset provenance reference.</summary>
    public string ProvenanceReference { get; }

    /// <summary>Gets the dataset authority reference.</summary>
    public string AuthorityReference { get; }

    /// <summary>Gets the trusted UTC evaluation instant.</summary>
    public DateTimeOffset EvaluatedAt { get; }

    /// <summary>Gets whether dataset validity permitted case execution.</summary>
    public bool DatasetAccepted { get; }

    /// <summary>Gets the stable dataset-level outcome code.</summary>
    public string Code { get; }

    /// <summary>Gets immutable case results in deterministic case order.</summary>
    public IReadOnlyList<ObserverOfflineEvaluationCaseResult> CaseResults { get; }

    /// <summary>Gets the exact-match case count.</summary>
    public int PassedCaseCount { get; }

    /// <summary>Gets the non-matching case count.</summary>
    public int FailedCaseCount { get; }

    /// <summary>Gets adversarial cases refused at the data boundary.</summary>
    public int RejectedAdversarialCaseCount { get; }

    /// <summary>Gets adversarial cases that crossed the data boundary and require investigation.</summary>
    public int AcceptedAdversarialCaseCount { get; }

    /// <summary>Gets reference detections that matched expected detections.</summary>
    public int TruePositiveCount { get; private set; }

    /// <summary>Gets reference non-detections incorrectly reported as detections.</summary>
    public int FalsePositiveCount { get; private set; }

    /// <summary>Gets expected reference detections that were not detected.</summary>
    public int FalseNegativeCount { get; private set; }

    /// <summary>Gets expected reference non-detections that did not produce a detection.</summary>
    public int TrueNegativeCount { get; private set; }

    /// <summary>Gets expected binary cases refused or left insufficient rather than scored as positive or negative.</summary>
    public int UnscoredDetectionCaseCount { get; private set; }

    /// <summary>Gets detection precision, or <see langword="null"/> when no positive result exists.</summary>
    public double? Precision { get; }

    /// <summary>Gets detection recall, or <see langword="null"/> when no expected positive case exists.</summary>
    public double? Recall { get; }

    /// <summary>Gets a value indicating exact case success and zero accepted adversarial cases.</summary>
    public bool Passed => DatasetAccepted && FailedCaseCount == 0 && AcceptedAdversarialCaseCount == 0;
}

/// <summary>Executes governed offline cases explicitly and synchronously without I/O, persistence or runtime binding.</summary>
public static class ObserverOfflineEvaluationRunner
{
    /// <summary>Evaluates a current governed dataset through the trusted adapter and deterministic threshold analyser.</summary>
    /// <param name="dataset">Versioned dataset with provenance, authority, expiry and segmentation.</param>
    /// <param name="evaluatedAt">Trusted UTC evaluation instant.</param>
    /// <param name="cancellationToken">Cancellation checked between cases and telemetry items.</param>
    /// <returns>A deterministic dataset report with exact-match and quality metrics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataset"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="evaluatedAt"/> is default or not expressed as UTC.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested during the bounded run.</exception>
    public static ObserverOfflineEvaluationReport Evaluate(
        ObserverOfflineEvaluationDataset dataset,
        DateTimeOffset evaluatedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        if (evaluatedAt == default || evaluatedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Offline evaluation time must be an explicit UTC value.", nameof(evaluatedAt));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (evaluatedAt < dataset.CreatedAt)
        {
            return DatasetRejected(dataset, evaluatedAt, "aiops.observer.offline.dataset_not_effective");
        }

        if (evaluatedAt >= dataset.ExpiresAt)
        {
            return DatasetRejected(dataset, evaluatedAt, "aiops.observer.offline.dataset_expired");
        }

        List<ObserverOfflineEvaluationCaseResult> results = new(dataset.Cases.Count);
        foreach (ObserverOfflineEvaluationCase evaluationCase in dataset.Cases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(EvaluateCase(evaluationCase, cancellationToken));
        }

        return new ObserverOfflineEvaluationReport(
            dataset,
            evaluatedAt,
            datasetAccepted: true,
            "aiops.observer.offline.completed",
            results.AsReadOnly());
    }

    /// <summary>Runs one case while converting cross-evidence boundary conflicts to an explicit sanitised rejection.</summary>
    /// <param name="evaluationCase">Bounded offline case.</param>
    /// <param name="cancellationToken">Cancellation checked before every adaptation.</param>
    /// <returns>One exact expected-versus-actual comparison.</returns>
    private static ObserverOfflineEvaluationCaseResult EvaluateCase(
        ObserverOfflineEvaluationCase evaluationCase,
        CancellationToken cancellationToken)
    {
        List<ObserverMetricSample> samples = new(evaluationCase.Telemetry.Count);
        foreach (ObserverCanonicalHealthTelemetry telemetry in evaluationCase.Telemetry)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObserverTelemetryAdaptationResult adaptation = CanonicalObserverTelemetryAdapter.AdaptForOfflineEvaluation(
                telemetry,
                evaluationCase.DataPolicy,
                evaluationCase.AnalysisAt);
            if (adaptation.Disposition == ObserverTelemetryAdaptationDisposition.Rejected)
            {
                return new ObserverOfflineEvaluationCaseResult(
                    evaluationCase,
                    ObserverOfflineExpectedDisposition.AdaptationRejected,
                    adaptation.Code);
            }

            samples.Add(adaptation.Sample!);
        }

        ObserverAnalysisRequest request;
        try
        {
            request = new ObserverAnalysisRequest(
                evaluationCase.AnalysisId,
                samples[0].InstanceId,
                evaluationCase.DataPolicy.AuthorisationScopeId,
                evaluationCase.AnalysisAt,
                samples);
        }
        catch (ArgumentException)
        {
            return new ObserverOfflineEvaluationCaseResult(
                evaluationCase,
                ObserverOfflineExpectedDisposition.AdaptationRejected,
                "aiops.observer.offline.evidence_boundary_invalid");
        }

        ObserverThresholdResult threshold = DeterministicThresholdAnalyser.Analyse(
            request,
            evaluationCase.ThresholdRule);
        ObserverOfflineExpectedDisposition actualDisposition = threshold.Disposition switch
        {
            ObserverFindingDisposition.Detected => ObserverOfflineExpectedDisposition.FindingDetected,
            ObserverFindingDisposition.NotDetected => ObserverOfflineExpectedDisposition.FindingNotDetected,
            ObserverFindingDisposition.InsufficientEvidence => ObserverOfflineExpectedDisposition.InsufficientEvidence,
            _ => throw new InvalidOperationException("Unexpected deterministic Observer finding disposition."),
        };
        return new ObserverOfflineEvaluationCaseResult(evaluationCase, actualDisposition, threshold.Code);
    }

    /// <summary>Creates a dataset-level refusal without evaluating or exposing any case telemetry.</summary>
    /// <param name="dataset">Governed dataset whose period was refused.</param>
    /// <param name="evaluatedAt">Trusted UTC evaluation instant.</param>
    /// <param name="code">Stable refusal code.</param>
    /// <returns>An empty, non-authorising offline report.</returns>
    private static ObserverOfflineEvaluationReport DatasetRejected(
        ObserverOfflineEvaluationDataset dataset,
        DateTimeOffset evaluatedAt,
        string code) =>
        new(dataset, evaluatedAt, datasetAccepted: false, code, []);
}
