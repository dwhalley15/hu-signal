/// <summary>
/// Defines the persistence operations used by Hu Signal to store and
/// retrieve Microsoft Clarity snapshots and their associated breakdown data.
/// </summary>
/// <remarks>
/// The repository provides the data-access boundary between Hu Signal's
/// application services and the underlying database.
///
/// Daily Clarity metrics are stored as <see cref="ClaritySnapshot"/> records,
/// while dimensions such as pages, referrers, countries, devices, browsers
/// and operating systems are stored as associated
/// <see cref="ClarityBreakdown"/> records.
///
/// Higher-level services should use this repository to access persisted
/// Clarity data rather than interacting with the database directly.
/// Aggregation, AI interpretation and PDF generation are intentionally
/// handled by separate services.
/// </remarks>
public interface IClarityRepository
{
    /// <summary>
    /// Saves a daily Clarity snapshot together with its associated
    /// breakdown records.
    /// </summary>
    /// <param name="snapshot">
    /// The daily <see cref="ClaritySnapshot"/> containing the primary
    /// traffic, engagement and behavioural metrics to persist.
    /// </param>
    /// <param name="breakdowns">
    /// The collection of <see cref="ClarityBreakdown"/> records associated
    /// with the snapshot, such as page, referrer, country, device, browser
    /// and operating-system breakdowns.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the database operation.
    /// </param>
    /// <returns>
    /// A task that resolves to the database identifier of the saved
    /// <see cref="ClaritySnapshot"/>.
    /// </returns>
    /// <remarks>
    /// Snapshots are identified by their snapshot date. The repository
    /// implementation is expected to preserve the daily snapshot model
    /// used by Hu Signal so that importing the same reporting date does
    /// not create duplicate daily snapshot records.
    ///
    /// The supplied breakdown records belong to the same daily snapshot
    /// and should be persisted in association with that snapshot.
    /// </remarks>
    Task<int> SaveSnapshotAsync(
        ClaritySnapshot snapshot,
        IReadOnlyList<ClarityBreakdown> breakdowns,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent Clarity snapshot stored by Hu Signal.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the database operation.
    /// </param>
    /// <returns>
    /// A task that resolves to the most recent
    /// <see cref="ClaritySnapshot"/>, or <see langword="null"/> when
    /// no snapshots have been stored.
    /// </returns>
    /// <remarks>
    /// This method is primarily used by the Latest view of the
    /// Hu Signal dashboard to display the newest available daily snapshot.
    /// </remarks>
    Task<ClaritySnapshot?> GetLatestAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all stored Clarity snapshots within the specified
    /// inclusive date range.
    /// </summary>
    /// <param name="from">
    /// The first snapshot date to include in the result.
    /// </param>
    /// <param name="to">
    /// The final snapshot date to include in the result.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the database operation.
    /// </param>
    /// <returns>
    /// A task that resolves to a read-only collection of
    /// <see cref="ClaritySnapshot"/> records within the requested
    /// reporting period.
    /// </returns>
    /// <remarks>
    /// This method returns the stored daily snapshots only. It does not
    /// perform monthly or yearly aggregation.
    ///
    /// Period aggregation is handled by the reporting service using the
    /// snapshots returned by this repository.
    /// </remarks>
    Task<IReadOnlyList<ClaritySnapshot>> GetSnapshotsAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all Clarity breakdown records associated with a
    /// specific daily snapshot.
    /// </summary>
    /// <param name="snapshotId">
    /// The database identifier of the
    /// <see cref="ClaritySnapshot"/> whose breakdowns should be returned.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the database operation.
    /// </param>
    /// <returns>
    /// A task that resolves to a read-only collection of
    /// <see cref="ClarityBreakdown"/> records associated with the
    /// specified snapshot.
    /// </returns>
    /// <remarks>
    /// This overload is useful when loading the breakdown data for a
    /// single daily snapshot, such as the Latest dashboard view.
    /// </remarks>
    Task<IReadOnlyList<ClarityBreakdown>> GetBreakdownsAsync(
        int snapshotId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all Clarity breakdown records associated with snapshots
    /// stored within the specified inclusive date range.
    /// </summary>
    /// <param name="from">
    /// The first snapshot date whose breakdown records should be included.
    /// </param>
    /// <param name="to">
    /// The final snapshot date whose breakdown records should be included.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the database operation.
    /// </param>
    /// <returns>
    /// A task that resolves to a read-only collection of
    /// <see cref="ClarityBreakdown"/> records belonging to snapshots
    /// within the requested reporting period.
    /// </returns>
    /// <remarks>
    /// This overload supports period-based reporting by retrieving the
    /// breakdown records required to build monthly or yearly summaries.
    ///
    /// The repository returns the persisted records; aggregation of those
    /// records into a <see cref="ClarityPeriodSummary"/> is the
    /// responsibility of the reporting service.
    /// </remarks>
    Task<IReadOnlyList<ClarityBreakdown>> GetBreakdownsAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the years for which Hu Signal has stored
    /// Microsoft Clarity snapshot data.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the database operation.
    /// </param>
    /// <returns>
    /// A task that resolves to a read-only collection of years
    /// containing at least one stored Clarity snapshot.
    /// </returns>
    /// <remarks>
    /// This method is used to populate the Year selector in the
    /// Hu Signal backoffice dashboard so that users are presented with
    /// reporting years for which stored data actually exists.
    /// </remarks>
    Task<IReadOnlyList<int>> GetAvailableYearsAsync(
        CancellationToken cancellationToken = default);
}