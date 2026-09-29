/// <summary>
/// Defines the service responsible for importing the daily
/// Microsoft Clarity analytics data into Hu Signal.
/// </summary>
/// <remarks>
/// The implementation is responsible for coordinating the daily
/// Clarity import process, including retrieving the latest available
/// analytics data and persisting the resulting snapshot and associated
/// breakdown data.
///
/// The import operation is designed to be used by both the scheduled
/// background import process and manual imports triggered from the
/// Hu Signal backoffice dashboard.
/// </remarks>
public interface IClarityImportService
{
    /// <summary>
    /// Retrieves and imports the latest daily Microsoft Clarity data.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the import operation,
    /// including the outbound Microsoft Clarity request and any
    /// subsequent persistence operations.
    /// </param>
    /// <returns>
    /// A task that resolves to a <see cref="ClarityImportResult"/>
    /// describing the imported snapshot, including whether the data
    /// was saved successfully.
    /// </returns>
    /// <remarks>
    /// The daily import uses the snapshot date to determine whether
    /// the data has already been stored, preventing repeated imports
    /// from creating duplicate daily snapshots.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Microsoft Clarity configuration is unavailable
    /// or when the returned data cannot be processed into a valid
    /// Hu Signal snapshot.
    /// </exception>
    Task<ClarityImportResult> ImportDailyAsync(
        CancellationToken cancellationToken = default);
}