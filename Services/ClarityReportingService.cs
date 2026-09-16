public class ClarityReportingService : IClarityReportingService
{
    private readonly IClarityRepository _clarityRepository;

    public ClarityReportingService(
        IClarityRepository clarityRepository)
    {
        _clarityRepository = clarityRepository;
    }

    public async Task<ClarityPeriodSummary> GetSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var snapshots =
            await _clarityRepository.GetSnapshotsAsync(
                from,
                to,
                cancellationToken);

        if (snapshots.Count == 0)
        {
            return new ClarityPeriodSummary
            {
                From = from,
                To = to,
                DaysWithData = 0
            };
        }

        var breakdowns =
            await _clarityRepository.GetBreakdownsAsync(
                from,
                to,
                cancellationToken);

        var totalSessions = snapshots.Sum(x => x.TotalSessions);

        var weightedScrollDepth =
            totalSessions > 0
                ? snapshots.Sum(
                    x => x.AverageScrollDepth * x.TotalSessions
                ) / totalSessions
                : 0;

        var weightedPagesPerSession =
            totalSessions > 0
                ? snapshots.Sum(
                    x => x.PagesPerSession * x.TotalSessions
                ) / totalSessions
                : 0;

        var groupedBreakdowns =
            breakdowns
                .GroupBy(x => new
                {
                    x.MetricName,
                    x.Name,
                    x.Url
                })
                .Select(group => new ClarityPeriodBreakdown
                {
                    MetricName = group.Key.MetricName,
                    Name = group.Key.Name,
                    Url = group.Key.Url,

                    Count = group.Sum(x =>
                        x.SessionsCount ??
                        x.VisitsCount ??
                        0)
                })
                .OrderBy(x => x.MetricName)
                .ThenByDescending(x => x.Count)
                .ToList();

        return new ClarityPeriodSummary
        {
            From = from,
            To = to,
            DaysWithData = snapshots.Count,

            TotalSessions = totalSessions,

            BotSessions =
                snapshots.Sum(x => x.BotSessions),

            AveragePagesPerSession =
                weightedPagesPerSession,

            AverageScrollDepth =
                weightedScrollDepth,

            DeadClicks =
                snapshots.Sum(x => x.DeadClicks),

            RageClicks =
                snapshots.Sum(x => x.RageClicks),

            Quickbacks =
                snapshots.Sum(x => x.Quickbacks),

            ScriptErrors =
                snapshots.Sum(x => x.ScriptErrors),

            ErrorClicks =
                snapshots.Sum(x => x.ErrorClicks),

            ExcessiveScrolls =
                snapshots.Sum(x => x.ExcessiveScrolls),

            Breakdowns = groupedBreakdowns
        };
    }
}