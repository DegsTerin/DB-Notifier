// Module purpose: Produces explainable capacity forecasts from bounded sanitised evidence without external models, persistence or action authority.
namespace DBNotifier.Application.AIOps;

/// <summary>Defines one bounded ordinary-least-squares capacity forecast policy.</summary>
public sealed class ObserverCapacityForecastPolicy
{
    /// <summary>Initialises one provider-neutral capacity forecast policy.</summary>
    /// <param name="policyId">Stable lower-case policy identifier.</param>
    /// <param name="policyVersion">Stable version identifier.</param>
    /// <param name="metricKey">Provider-neutral metric identifier.</param>
    /// <param name="unit">Canonical metric unit.</param>
    /// <param name="capacityLimit">Finite capacity boundary expressed in <paramref name="unit"/>.</param>
    /// <param name="severity">Deterministic severity assigned to an in-horizon forecast.</param>
    /// <param name="minimumSampleCount">Minimum sample count from three to one thousand.</param>
    /// <param name="lookback">Positive source window no greater than one year.</param>
    /// <param name="maximumSampleAge">Positive freshness limit no greater than <paramref name="lookback"/>.</param>
    /// <param name="forecastHorizon">Positive detection horizon no greater than ten years.</param>
    /// <param name="minimumFitQuality">Minimum coefficient of determination between zero and one.</param>
    /// <param name="minimumGrowthPerHour">Finite non-negative growth rate below which no growth is detected.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required identifier is null.</exception>
    /// <exception cref="ArgumentException">Thrown for unsafe identifiers or non-finite numeric policy values.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a bound or enum is outside policy.</exception>
    public ObserverCapacityForecastPolicy(
        string policyId,
        string policyVersion,
        string metricKey,
        string unit,
        double capacityLimit,
        ObserverFindingSeverity severity,
        int minimumSampleCount,
        TimeSpan lookback,
        TimeSpan maximumSampleAge,
        TimeSpan forecastHorizon,
        double minimumFitQuality,
        double minimumGrowthPerHour)
    {
        if (!double.IsFinite(capacityLimit))
        {
            throw new ArgumentException("Observer capacity limits must be finite.", nameof(capacityLimit));
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(minimumSampleCount, 3);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimumSampleCount, 1_000);
        ObserverTimeBounds.PositiveAtMost(lookback, TimeSpan.FromDays(365), nameof(lookback));
        ObserverTimeBounds.PositiveAtMost(maximumSampleAge, lookback, nameof(maximumSampleAge));
        ObserverTimeBounds.PositiveAtMost(forecastHorizon, TimeSpan.FromDays(3650), nameof(forecastHorizon));
        if (!double.IsFinite(minimumFitQuality) || minimumFitQuality is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumFitQuality));
        }

        if (!double.IsFinite(minimumGrowthPerHour) || minimumGrowthPerHour < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumGrowthPerHour));
        }

        PolicyId = ObserverContractGuard.StableIdentifier(policyId, nameof(policyId));
        PolicyVersion = ObserverContractGuard.StableIdentifier(policyVersion, nameof(policyVersion));
        MetricKey = ObserverContractGuard.StableIdentifier(metricKey, nameof(metricKey));
        Unit = ObserverContractGuard.StableIdentifier(unit, nameof(unit));
        CapacityLimit = capacityLimit;
        Severity = severity;
        MinimumSampleCount = minimumSampleCount;
        Lookback = lookback;
        MaximumSampleAge = maximumSampleAge;
        ForecastHorizon = forecastHorizon;
        MinimumFitQuality = minimumFitQuality;
        MinimumGrowthPerHour = minimumGrowthPerHour;
    }

    /// <summary>Gets the stable policy identifier.</summary>
    public string PolicyId { get; }

    /// <summary>Gets the policy version used to reproduce the forecast.</summary>
    public string PolicyVersion { get; }

    /// <summary>Gets the provider-neutral metric identifier.</summary>
    public string MetricKey { get; }

    /// <summary>Gets the canonical metric unit.</summary>
    public string Unit { get; }

    /// <summary>Gets the capacity boundary.</summary>
    public double CapacityLimit { get; }

    /// <summary>Gets the deterministic severity assigned to a detected forecast.</summary>
    public ObserverFindingSeverity Severity { get; }

    /// <summary>Gets the minimum sample count.</summary>
    public int MinimumSampleCount { get; }

    /// <summary>Gets the maximum source history considered.</summary>
    public TimeSpan Lookback { get; }

    /// <summary>Gets the newest-sample freshness limit.</summary>
    public TimeSpan MaximumSampleAge { get; }

    /// <summary>Gets the future horizon within which a forecast is detected.</summary>
    public TimeSpan ForecastHorizon { get; }

    /// <summary>Gets the minimum coefficient of determination accepted as sufficiently fitted.</summary>
    public double MinimumFitQuality { get; }

    /// <summary>Gets the minimum meaningful growth rate per hour.</summary>
    public double MinimumGrowthPerHour { get; }
}

/// <summary>
/// Contains one explainable capacity result, including the model and policy versions, evidence window, historical
/// error, fit quality, growth-rate confidence interval, prediction validity and stable limitations.
/// </summary>
public sealed class ObserverCapacityForecastResult
{
    /// <summary>Initialises one immutable statistical result from validated policy and evidence.</summary>
    /// <param name="policy">Policy whose identity and statistical limits are retained.</param>
    /// <param name="request">Scope-bound request whose identity and analysis instant are retained.</param>
    /// <param name="disposition">Factual detected, not-detected or insufficient disposition.</param>
    /// <param name="code">Stable non-secret outcome code.</param>
    /// <param name="windowStart">First modelled observation instant, when evidence was usable.</param>
    /// <param name="windowEnd">Last modelled observation instant, when evidence was usable.</param>
    /// <param name="validUntil">Forecast freshness boundary.</param>
    /// <param name="sampleCount">Number of observations used by the model.</param>
    /// <param name="latestValue">Newest modelled value.</param>
    /// <param name="growthPerHour">Fitted point growth rate.</param>
    /// <param name="growthRateLowerBound95">Lower two-sided 95% slope bound.</param>
    /// <param name="growthRateUpperBound95">Upper two-sided 95% slope bound.</param>
    /// <param name="fitQuality">Coefficient of determination.</param>
    /// <param name="rootMeanSquaredError">Historical residual error.</param>
    /// <param name="predictedAt">Point threshold estimate.</param>
    /// <param name="earliestSensitivityAt">Conditional threshold estimate from the upper slope bound.</param>
    /// <param name="latestSensitivityAt">Conditional threshold estimate from the lower slope bound, when bounded.</param>
    /// <param name="evidenceIds">Immutable evidence links.</param>
    /// <param name="limitationCodes">Immutable factual limitations.</param>
    internal ObserverCapacityForecastResult(
        ObserverCapacityForecastPolicy policy,
        ObserverAnalysisRequest request,
        ObserverFindingDisposition disposition,
        string code,
        DateTimeOffset? windowStart,
        DateTimeOffset? windowEnd,
        DateTimeOffset? validUntil,
        int sampleCount,
        double? latestValue,
        double? growthPerHour,
        double? growthRateLowerBound95,
        double? growthRateUpperBound95,
        double? fitQuality,
        double? rootMeanSquaredError,
        DateTimeOffset? predictedAt,
        DateTimeOffset? earliestSensitivityAt,
        DateTimeOffset? latestSensitivityAt,
        IReadOnlyList<Guid> evidenceIds,
        IReadOnlyList<string> limitationCodes)
    {
        PolicyId = policy.PolicyId;
        PolicyVersion = policy.PolicyVersion;
        AnalysisId = request.AnalysisId;
        InstanceId = request.InstanceId;
        AuthorisationScopeId = request.AuthorisationScopeId;
        MetricKey = policy.MetricKey;
        Unit = policy.Unit;
        CapacityLimit = policy.CapacityLimit;
        PolicySeverity = policy.Severity;
        EvaluatedAt = request.AsOf;
        Disposition = disposition;
        Code = code;
        WindowStart = windowStart;
        WindowEnd = windowEnd;
        ValidUntil = validUntil;
        SampleCount = sampleCount;
        LatestValue = latestValue;
        GrowthPerHour = growthPerHour;
        GrowthRateLowerBound95 = growthRateLowerBound95;
        GrowthRateUpperBound95 = growthRateUpperBound95;
        FitQuality = fitQuality;
        RootMeanSquaredError = rootMeanSquaredError;
        PredictedAt = predictedAt;
        EarliestSensitivityAt = earliestSensitivityAt;
        LatestSensitivityAt = latestSensitivityAt;
        EvidenceIds = evidenceIds;
        LimitationCodes = limitationCodes;
    }

    /// <summary>Gets the fixed statistical model identifier.</summary>
    public string ModelId { get; } = "ordinary-least-squares";

    /// <summary>Gets the fixed implementation version of the statistical model.</summary>
    public string ModelVersion { get; } = "1.0.0";

    /// <summary>Gets a value indicating whether the statistical model ran and produced fitted values.</summary>
    public bool ModelApplied => GrowthPerHour is not null;

    /// <summary>Gets the stable forecast policy identifier.</summary>
    public string PolicyId { get; }

    /// <summary>Gets the forecast policy version.</summary>
    public string PolicyVersion { get; }

    /// <summary>Gets the analysis correlation identifier retained from the scope-bound request.</summary>
    public Guid AnalysisId { get; }

    /// <summary>Gets the database-instance identifier.</summary>
    public Guid InstanceId { get; }

    /// <summary>Gets the authorisation scope under which the evidence was analysed.</summary>
    public Guid AuthorisationScopeId { get; }

    /// <summary>Gets the provider-neutral metric identifier.</summary>
    public string MetricKey { get; }

    /// <summary>Gets the canonical metric unit.</summary>
    public string Unit { get; }

    /// <summary>Gets the configured capacity boundary.</summary>
    public double CapacityLimit { get; }

    /// <summary>Gets the policy severity that applies only when <see cref="Disposition"/> is detected.</summary>
    public ObserverFindingSeverity PolicySeverity { get; }

    /// <summary>Gets the authoritative analysis instant in UTC.</summary>
    public DateTimeOffset EvaluatedAt { get; }

    /// <summary>Gets the detected, not-detected or insufficient-evidence disposition.</summary>
    public ObserverFindingDisposition Disposition { get; }

    /// <summary>Gets the stable, non-secret result code.</summary>
    public string Code { get; }

    /// <summary>Gets the first selected observation instant.</summary>
    public DateTimeOffset? WindowStart { get; }

    /// <summary>Gets the last selected observation instant.</summary>
    public DateTimeOffset? WindowEnd { get; }

    /// <summary>Gets the instant after which the forecast is stale unless new evidence is analysed.</summary>
    public DateTimeOffset? ValidUntil { get; }

    /// <summary>Gets the number of samples used by the model.</summary>
    public int SampleCount { get; }

    /// <summary>Gets the newest selected value.</summary>
    public double? LatestValue { get; }

    /// <summary>Gets the fitted metric growth per hour.</summary>
    public double? GrowthPerHour { get; }

    /// <summary>Gets the lower bound of the two-sided 95% slope interval under the OLS assumptions.</summary>
    public double? GrowthRateLowerBound95 { get; }

    /// <summary>Gets the upper bound of the two-sided 95% slope interval under the OLS assumptions.</summary>
    public double? GrowthRateUpperBound95 { get; }

    /// <summary>Gets the coefficient of determination, bounded from zero to one.</summary>
    public double? FitQuality { get; }

    /// <summary>Gets the historical root-mean-square residual error in the canonical unit.</summary>
    public double? RootMeanSquaredError { get; }

    /// <summary>Gets the point estimate for reaching the configured capacity.</summary>
    public DateTimeOffset? PredictedAt { get; }

    /// <summary>Gets the conditional earlier estimate with the fitted intercept held fixed at the upper slope bound.</summary>
    public DateTimeOffset? EarliestSensitivityAt { get; }

    /// <summary>Gets the conditional later estimate with the fitted intercept held fixed at the lower slope bound.</summary>
    public DateTimeOffset? LatestSensitivityAt { get; }

    /// <summary>Gets immutable links to evidence used or rejected.</summary>
    public IReadOnlyList<Guid> EvidenceIds { get; }

    /// <summary>Gets stable limitations that qualify the forecast.</summary>
    public IReadOnlyList<string> LimitationCodes { get; }
}

/// <summary>Fits one bounded, deterministic capacity forecast without network, persistence, LLM or executor access.</summary>
public static class CapacityForecastAnalyser
{
    private const string SensitivityLimitation = "aiops.observer.forecast.time_bounds_are_sensitivity";

    /// <summary>Analyses one capacity policy against the request's sanitised evidence.</summary>
    /// <param name="request">Validated, single-instance Observer analysis request.</param>
    /// <param name="policy">Versioned capacity policy and statistical acceptance limits.</param>
    /// <returns>An explainable forecast result that never grants action authority.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> or <paramref name="policy"/> is null.</exception>
    public static ObserverCapacityForecastResult Analyse(
        ObserverAnalysisRequest request,
        ObserverCapacityForecastPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(policy);

        ObserverEvidenceSelection selection = ObserverEvidenceWindow.Select(
            request,
            policy.MetricKey,
            policy.Unit,
            policy.Lookback,
            policy.MaximumSampleAge);
        if (!selection.IsUsable)
        {
            return EmptyResult(request, policy, selection.FailureCode!, selection.EvidenceIds, selection.LimitationCodes);
        }

        ObserverMetricSample latest = selection.Samples[^1];
        DateTimeOffset validUntil = ObserverTimeBounds.AddOrMaximum(latest.ObservedAt, policy.MaximumSampleAge);
        if (latest.Value >= policy.CapacityLimit)
        {
            return Result(
                request,
                policy,
                selection,
                ObserverFindingDisposition.Detected,
                "aiops.observer.forecast.capacity_reached",
                validUntil,
                latest.Value,
                null,
                null,
                null,
                null,
                null,
                latest.ObservedAt,
                null,
                null,
                selection.LimitationCodes);
        }

        if (selection.Samples.Count < policy.MinimumSampleCount)
        {
            return EmptyResult(
                request,
                policy,
                "aiops.observer.forecast.insufficient_samples",
                selection.EvidenceIds,
                [.. selection.LimitationCodes, "aiops.observer.forecast.insufficient_samples"]);
        }

        ObserverMetricSample first = selection.Samples[0];

        double[] x = selection.Samples
            .Select(sample => (sample.ObservedAt - first.ObservedAt).TotalHours)
            .ToArray();
        double[] y = selection.Samples.Select(sample => sample.Value).ToArray();
        double meanX = x.Average();
        double meanY = y[0];
        for (int index = 1; index < y.Length; index++)
        {
            meanY += (y[index] - meanY) / (index + 1);
        }

        if (!double.IsFinite(meanY))
        {
            return NumericFailure(request, policy, selection);
        }

        double sumSquaresX = 0;
        double sumProducts = 0;
        double totalSquaresY = 0;
        for (int index = 0; index < x.Length; index++)
        {
            double centredX = x[index] - meanX;
            double centredY = y[index] - meanY;
            sumSquaresX += centredX * centredX;
            sumProducts += centredX * centredY;
            totalSquaresY += centredY * centredY;
        }

        if (!AllFinite(sumSquaresX, sumProducts, totalSquaresY))
        {
            return NumericFailure(request, policy, selection);
        }

        if (sumSquaresX <= 0)
        {
            return EmptyResult(
                request,
                policy,
                "aiops.observer.forecast.insufficient_time_span",
                selection.EvidenceIds,
                [.. selection.LimitationCodes, "aiops.observer.forecast.insufficient_time_span"]);
        }

        double growthPerHour = sumProducts / sumSquaresX;
        double intercept = meanY - (growthPerHour * meanX);
        if (!AllFinite(growthPerHour, intercept))
        {
            return NumericFailure(request, policy, selection);
        }

        double residualSquares = 0;
        for (int index = 0; index < x.Length; index++)
        {
            double residual = y[index] - (intercept + (growthPerHour * x[index]));
            residualSquares += residual * residual;
        }


        if (!double.IsFinite(residualSquares))
        {
            return NumericFailure(request, policy, selection);
        }

        double fitQuality = totalSquaresY == 0
            ? (residualSquares == 0 ? 1 : 0)
            : Math.Clamp(1 - (residualSquares / totalSquaresY), 0, 1);
        double rootMeanSquaredError = Math.Sqrt(residualSquares / x.Length);
        double residualVariance = residualSquares / (x.Length - 2);
        double slopeStandardError = Math.Sqrt(residualVariance / sumSquaresX);
        double criticalValue = CriticalValue95(x.Length - 2);
        double growthLower95 = growthPerHour - (criticalValue * slopeStandardError);
        double growthUpper95 = growthPerHour + (criticalValue * slopeStandardError);
        if (!AllFinite(
                fitQuality,
                rootMeanSquaredError,
                residualVariance,
                slopeStandardError,
                growthLower95,
                growthUpper95))
        {
            return NumericFailure(request, policy, selection);
        }

        if (fitQuality < policy.MinimumFitQuality)
        {
            return Result(
                request,
                policy,
                selection,
                ObserverFindingDisposition.InsufficientEvidence,
                "aiops.observer.forecast.fit_below_policy",
                validUntil,
                latest.Value,
                growthPerHour,
                growthLower95,
                growthUpper95,
                fitQuality,
                rootMeanSquaredError,
                null,
                null,
                null,
                [.. selection.LimitationCodes, "aiops.observer.forecast.fit_below_policy"]);
        }

        if (growthUpper95 <= policy.MinimumGrowthPerHour)
        {
            return Result(
                request,
                policy,
                selection,
                ObserverFindingDisposition.NotDetected,
                "aiops.observer.forecast.growth_not_detected",
                validUntil,
                latest.Value,
                growthPerHour,
                growthLower95,
                growthUpper95,
                fitQuality,
                rootMeanSquaredError,
                null,
                null,
                null,
                selection.LimitationCodes);
        }

        if (growthLower95 <= policy.MinimumGrowthPerHour)
        {
            return Result(
                request,
                policy,
                selection,
                ObserverFindingDisposition.InsufficientEvidence,
                "aiops.observer.forecast.growth_uncertain",
                validUntil,
                latest.Value,
                growthPerHour,
                growthLower95,
                growthUpper95,
                fitQuality,
                rootMeanSquaredError,
                null,
                null,
                null,
                [.. selection.LimitationCodes, "aiops.observer.forecast.growth_uncertain"]);
        }

        double capacityOffset = policy.CapacityLimit - intercept;
        if (!double.IsFinite(capacityOffset))
        {
            return NumericFailure(request, policy, selection);
        }

        DateTimeOffset? predictedAt = TryAddHours(first.ObservedAt, capacityOffset / growthPerHour);
        DateTimeOffset? earliestSensitivityAt = TryAddHours(first.ObservedAt, capacityOffset / growthUpper95);
        DateTimeOffset? latestSensitivityAt = TryAddHours(first.ObservedAt, capacityOffset / growthLower95);
        string[] sensitivityLimitations =
        [
            .. selection.LimitationCodes,
            SensitivityLimitation,
        ];

        if (predictedAt is null || earliestSensitivityAt is null || latestSensitivityAt is null ||
            predictedAt <= request.AsOf || earliestSensitivityAt <= request.AsOf || latestSensitivityAt <= request.AsOf)
        {
            return Result(
                request,
                policy,
                selection,
                ObserverFindingDisposition.InsufficientEvidence,
                "aiops.observer.forecast.projection_conflict",
                validUntil,
                latest.Value,
                growthPerHour,
                growthLower95,
                growthUpper95,
                fitQuality,
                rootMeanSquaredError,
                predictedAt,
                earliestSensitivityAt,
                latestSensitivityAt,
                [.. sensitivityLimitations, "aiops.observer.forecast.projection_conflict"]);
        }

        DateTimeOffset horizonEnd = ObserverTimeBounds.AddOrMaximum(request.AsOf, policy.ForecastHorizon);
        if (latestSensitivityAt <= horizonEnd)
        {
            return Result(
                request,
                policy,
                selection,
                ObserverFindingDisposition.Detected,
                "aiops.observer.forecast.capacity_within_horizon",
                validUntil,
                latest.Value,
                growthPerHour,
                growthLower95,
                growthUpper95,
                fitQuality,
                rootMeanSquaredError,
                predictedAt,
                earliestSensitivityAt,
                latestSensitivityAt,
                sensitivityLimitations);
        }

        if (earliestSensitivityAt > horizonEnd)
        {
            return Result(
                request,
                policy,
                selection,
                ObserverFindingDisposition.NotDetected,
                "aiops.observer.forecast.capacity_outside_horizon",
                validUntil,
                latest.Value,
                growthPerHour,
                growthLower95,
                growthUpper95,
                fitQuality,
                rootMeanSquaredError,
                predictedAt,
                earliestSensitivityAt,
                latestSensitivityAt,
                sensitivityLimitations);
        }

        return Result(
            request,
            policy,
            selection,
            ObserverFindingDisposition.InsufficientEvidence,
            "aiops.observer.forecast.horizon_uncertain",
            validUntil,
            latest.Value,
            growthPerHour,
            growthLower95,
            growthUpper95,
            fitQuality,
            rootMeanSquaredError,
            predictedAt,
            earliestSensitivityAt,
            latestSensitivityAt,
            [.. sensitivityLimitations, "aiops.observer.forecast.horizon_uncertain"]);
    }

    /// <summary>Creates a result for evidence rejected before a statistical model could run.</summary>
    /// <param name="request">Scope-bound analysis request.</param>
    /// <param name="policy">Forecast policy that could not be evaluated.</param>
    /// <param name="code">Stable insufficiency code.</param>
    /// <param name="evidenceIds">Evidence links relevant to the refusal.</param>
    /// <param name="limitationCodes">Stable limitations explaining the refusal.</param>
    /// <returns>An insufficient-evidence result without statistical values.</returns>
    private static ObserverCapacityForecastResult EmptyResult(
        ObserverAnalysisRequest request,
        ObserverCapacityForecastPolicy policy,
        string code,
        IReadOnlyList<Guid> evidenceIds,
        IReadOnlyList<string> limitationCodes) =>
        new(
            policy,
            request,
            ObserverFindingDisposition.InsufficientEvidence,
            code,
            null,
            null,
            null,
            0,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            evidenceIds,
            Array.AsReadOnly(limitationCodes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()));

    /// <summary>Creates a fully attributed result from a usable evidence window.</summary>
    /// <param name="request">Scope-bound analysis request.</param>
    /// <param name="policy">Policy whose identity and limits are retained.</param>
    /// <param name="selection">Usable evidence window.</param>
    /// <param name="disposition">Factual analysis disposition.</param>
    /// <param name="code">Stable outcome code.</param>
    /// <param name="validUntil">Forecast freshness boundary.</param>
    /// <param name="latestValue">Newest selected value.</param>
    /// <param name="growthPerHour">Fitted point growth rate.</param>
    /// <param name="growthLower95">Lower two-sided 95% slope bound.</param>
    /// <param name="growthUpper95">Upper two-sided 95% slope bound.</param>
    /// <param name="fitQuality">Coefficient of determination.</param>
    /// <param name="rootMeanSquaredError">Historical residual error.</param>
    /// <param name="predictedAt">Point threshold estimate from the fitted line.</param>
    /// <param name="earliestSensitivityAt">Conditional estimate from the upper slope bound.</param>
    /// <param name="latestSensitivityAt">Conditional estimate from the lower slope bound.</param>
    /// <param name="limitationCodes">Stable limitations qualifying the result.</param>
    /// <returns>A fully attributed immutable capacity result.</returns>
    private static ObserverCapacityForecastResult Result(
        ObserverAnalysisRequest request,
        ObserverCapacityForecastPolicy policy,
        ObserverEvidenceSelection selection,
        ObserverFindingDisposition disposition,
        string code,
        DateTimeOffset validUntil,
        double latestValue,
        double? growthPerHour,
        double? growthLower95,
        double? growthUpper95,
        double? fitQuality,
        double? rootMeanSquaredError,
        DateTimeOffset? predictedAt,
        DateTimeOffset? earliestSensitivityAt,
        DateTimeOffset? latestSensitivityAt,
        IEnumerable<string> limitationCodes) =>
        new(
            policy,
            request,
            disposition,
            code,
            selection.Samples[0].ObservedAt,
            selection.Samples[^1].ObservedAt,
            validUntil,
            selection.Samples.Count,
            latestValue,
            growthPerHour,
            growthLower95,
            growthUpper95,
            fitQuality,
            rootMeanSquaredError,
            predictedAt,
            earliestSensitivityAt,
            latestSensitivityAt,
            selection.EvidenceIds,
            Array.AsReadOnly(limitationCodes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()));

    /// <summary>Adds a finite projected duration while converting date-range overflow to explicit uncertainty.</summary>
    /// <param name="start">Fitted-line origin instant.</param>
    /// <param name="hours">Projected hours from the origin.</param>
    /// <returns>The projected instant, or <see langword="null"/> for non-finite or out-of-range input.</returns>
    private static DateTimeOffset? TryAddHours(DateTimeOffset start, double hours)
    {
        if (!double.IsFinite(hours))
        {
            return null;
        }

        try
        {
            return start.AddHours(hours);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>Returns a conservative two-sided 95% Student critical value for the bounded sample count.</summary>
    /// <param name="degreesOfFreedom">Positive residual degrees of freedom.</param>
    /// <returns>An exact tabulated or conservative bucketed critical value.</returns>
    private static double CriticalValue95(int degreesOfFreedom) =>
        degreesOfFreedom switch
        {
            1 => 12.706204736,
            2 => 4.302652730,
            3 => 3.182446305,
            4 => 2.776445105,
            5 => 2.570581836,
            6 => 2.446911851,
            7 => 2.364624252,
            8 => 2.306004135,
            9 => 2.262157163,
            10 => 2.228138852,
            11 => 2.200985160,
            12 => 2.178812830,
            13 => 2.160368656,
            14 => 2.144786688,
            15 => 2.131449546,
            16 => 2.119905299,
            17 => 2.109815578,
            18 => 2.100922040,
            19 => 2.093024054,
            20 => 2.085963447,
            21 => 2.079613845,
            22 => 2.073873068,
            23 => 2.068657610,
            24 => 2.063898562,
            25 => 2.059538553,
            26 => 2.055529439,
            27 => 2.051830516,
            28 => 2.048407142,
            29 => 2.045229642,
            30 => 2.042272456,
            <= 40 => 2.042272456,
            <= 60 => 2.021075390,
            <= 120 => 2.000297822,
            _ => 1.979930405,
        };

    /// <summary>Creates an explicit numeric-overflow result without retaining non-finite intermediate values.</summary>
    /// <param name="request">Scope-bound analysis request.</param>
    /// <param name="policy">Forecast policy being evaluated.</param>
    /// <param name="selection">Usable evidence window whose arithmetic overflowed.</param>
    /// <returns>An explicit insufficient-evidence result.</returns>
    private static ObserverCapacityForecastResult NumericFailure(
        ObserverAnalysisRequest request,
        ObserverCapacityForecastPolicy policy,
        ObserverEvidenceSelection selection) =>
        EmptyResult(
            request,
            policy,
            "aiops.observer.forecast.numeric_overflow",
            selection.EvidenceIds,
            [.. selection.LimitationCodes, "aiops.observer.forecast.numeric_overflow"]);

    /// <summary>Checks statistical intermediates before they can influence a factual result.</summary>
    /// <param name="values">Statistical intermediates to validate.</param>
    /// <returns><see langword="true"/> only when every value is finite.</returns>
    private static bool AllFinite(params double[] values) => values.All(double.IsFinite);
}
