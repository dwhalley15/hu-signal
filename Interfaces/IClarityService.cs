/// <summary>
/// Defines the service responsible for retrieving analytics insights
/// from the Microsoft Clarity API.
/// </summary>
/// <remarks>
/// This service provides the external integration boundary between
/// Hu Signal and Microsoft Clarity.
///
/// The implementation is responsible for communicating with the configured
/// Microsoft Clarity API, authenticating requests using the configured
/// Clarity credentials, and converting the returned API data into
/// <see cref="ClarityMetricResponse"/> records that can be consumed by
/// the Hu Signal import process.
///
/// This service retrieves Clarity data only. It does not persist snapshots,
/// aggregate reporting periods, perform AI analysis, or generate PDF reports.
/// Those responsibilities are handled by the corresponding Hu Signal services.
/// </remarks>
public interface IClarityService
{
    /// <summary>
    /// Retrieves the latest daily analytics insights available from
    /// Microsoft Clarity.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the outbound Microsoft Clarity
    /// API request and any associated response processing.
    /// </param>
    /// <returns>
    /// A task that resolves to a read-only collection of
    /// <see cref="ClarityMetricResponse"/> records containing the
    /// daily metrics and breakdown data returned by Microsoft Clarity.
    /// </returns>
    /// <remarks>
    /// The returned data represents the daily Clarity insights used by
    /// Hu Signal's import process to construct a
    /// <see cref="ClaritySnapshot"/> and its associated
    /// <see cref="ClarityBreakdown"/> records.
    ///
    /// This method does not save the returned data to the Hu Signal
    /// database. Persistence is coordinated separately by
    /// <see cref="IClarityImportService"/>.
    /// </remarks>
    Task<IReadOnlyList<ClarityMetricResponse>> GetDailyInsightsAsync(
        CancellationToken cancellationToken = default);
}