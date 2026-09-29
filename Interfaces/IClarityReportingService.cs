/// <summary>
/// Defines the service responsible for aggregating stored
/// Microsoft Clarity data into reporting-period summaries.
/// </summary>
/// <remarks>
/// This service provides the reporting layer between the raw daily
/// Clarity snapshots stored by Hu Signal and consumers such as the
/// backoffice dashboard and AI report generation service.
///
/// The implementation combines the available daily snapshots and
/// breakdown records within a requested date range into a single
/// <see cref="ClarityPeriodSummary"/>.
///
/// The resulting summary contains aggregated traffic, engagement,
/// behavioural and breakdown data for the requested reporting period.
/// </remarks>
public interface IClarityReportingService
{
    /// <summary>
    /// Generates an aggregated Microsoft Clarity summary for the
    /// specified inclusive date range.
    /// </summary>
    /// <param name="from">
    /// The first date to include in the reporting period.
    /// </param>
    /// <param name="to">
    /// The final date to include in the reporting period.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the reporting operation,
    /// including retrieval of stored snapshots and breakdown data.
    /// </param>
    /// <returns>
    /// A task that resolves to a <see cref="ClarityPeriodSummary"/>
    /// containing the aggregated Clarity metrics and breakdowns for
    /// the requested reporting period.
    /// </returns>
    /// <remarks>
    /// The requested date range represents the complete reporting
    /// period and is distinct from the number of days for which
    /// Clarity data is actually available.
    ///
    /// The returned summary therefore includes the number of stored
    /// data days separately, allowing consumers to identify incomplete
    /// or partial reporting periods.
    ///
    /// This method performs deterministic aggregation of the stored
    /// Clarity data. It does not perform AI analysis or generate
    /// interpretive recommendations.
    /// </remarks>
    Task<ClarityPeriodSummary> GetSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}