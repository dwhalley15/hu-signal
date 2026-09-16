using Umbraco.Cms.Infrastructure.Scoping;

public class ClarityRepository : IClarityRepository
{
    private readonly IScopeProvider _scopeProvider;

    public ClarityRepository(IScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public async Task<int> SaveSnapshotAsync(
        ClaritySnapshot snapshot,
        IReadOnlyList<ClarityBreakdown> breakdowns,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeProvider.CreateScope();

        var database = scope.Database;

        var existing =
            await database.FirstOrDefaultAsync<ClaritySnapshot>(
                "WHERE SnapshotDate = @0",
                new object[] { snapshot.SnapshotDate },
                cancellationToken);

        int snapshotId;

        if (existing is not null)
        {
            snapshot.Id = existing.Id;

            await database.UpdateAsync(
                snapshot,
                cancellationToken);

            snapshotId = existing.Id;

            await database.ExecuteAsync(
                "DELETE FROM HuSignalClarityBreakdown WHERE SnapshotId = @0",
                new object[] { snapshotId },
                cancellationToken);
        }
        else
        {
            var insertedId = await database.InsertAsync(
                snapshot,
                cancellationToken);

            snapshotId = Convert.ToInt32(insertedId);
        }

        foreach (var breakdown in breakdowns)
        {
            breakdown.SnapshotId = snapshotId;

            await database.InsertAsync(
                breakdown,
                cancellationToken);
        }

        scope.Complete();

        return snapshotId;
    }

    public async Task<ClaritySnapshot?> GetLatestAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope =
            _scopeProvider.CreateScope(autoComplete: true);

        return await scope.Database
            .FirstOrDefaultAsync<ClaritySnapshot>(
                "ORDER BY SnapshotDate DESC",
                Array.Empty<object>(),
                cancellationToken);
    }

    public async Task<IReadOnlyList<ClaritySnapshot>> GetSnapshotsAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        using var scope =
            _scopeProvider.CreateScope(autoComplete: true);

        var results =
            await scope.Database.FetchAsync<ClaritySnapshot>(
                "WHERE SnapshotDate >= @0 AND SnapshotDate <= @1 ORDER BY SnapshotDate",
                new object[] { from, to },
                cancellationToken);

        return results;
    }

    public async Task<IReadOnlyList<ClarityBreakdown>> GetBreakdownsAsync(
    int snapshotId,
    CancellationToken cancellationToken = default)
    {
        using var scope =
            _scopeProvider.CreateScope(autoComplete: true);

        var results =
            await scope.Database.FetchAsync<ClarityBreakdown>(
                "WHERE SnapshotId = @0 ORDER BY MetricName, Id",
                new object[] { snapshotId },
                cancellationToken);

        return results;
    }

    public async Task<IReadOnlyList<ClarityBreakdown>> GetBreakdownsAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        using var scope =
            _scopeProvider.CreateScope(autoComplete: true);

        var results =
            await scope.Database.FetchAsync<ClarityBreakdown>(
                """
            SELECT b.*
            FROM HuSignalClarityBreakdown b
            INNER JOIN HuSignalClaritySnapshot s
                ON s.Id = b.SnapshotId
            WHERE s.SnapshotDate >= @0
              AND s.SnapshotDate <= @1
            ORDER BY s.SnapshotDate, b.MetricName, b.Id
            """,
                new object[] { from, to },
                cancellationToken);

        return results;
    }
}