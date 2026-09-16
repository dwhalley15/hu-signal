public interface IClarityRepository
{
      Task<int> SaveSnapshotAsync(
        ClaritySnapshot snapshot,
        IReadOnlyList<ClarityBreakdown> breakdowns,
        CancellationToken cancellationToken = default);

    Task<ClaritySnapshot?> GetLatestAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClaritySnapshot>> GetSnapshotsAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClarityBreakdown>> GetBreakdownsAsync(
        int snapshotId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClarityBreakdown>> GetBreakdownsAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}