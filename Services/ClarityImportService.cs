using System.Text.Json;

public class ClarityImportService : IClarityImportService
{
    private readonly IClarityService _clarityService;
    private readonly IClarityRepository _clarityRepository;

    public ClarityImportService(
        IClarityService clarityService,
        IClarityRepository clarityRepository)
    {
        _clarityService = clarityService;
        _clarityRepository = clarityRepository;
    }

    public async Task<ClarityImportResult> ImportDailyAsync(
        CancellationToken cancellationToken = default)
    {
        var metrics = await _clarityService.GetDailyInsightsAsync(
            cancellationToken);

        var rawJson = JsonSerializer.Serialize(metrics);

        var snapshotDate = DateTime.UtcNow.Date.AddDays(-1);

        var traffic = GetFirst(metrics, "Traffic");
        var scrollDepth = GetFirst(metrics, "ScrollDepth");
        var engagement = GetFirst(metrics, "EngagementTime");

        var snapshot = new ClaritySnapshot
        {
            SnapshotDate = snapshotDate,
            RetrievedAtUtc = DateTime.UtcNow,

            TotalSessions = traffic?.TotalSessionCount ?? 0,
            DistinctUsers = traffic?.DistinctUserCount ?? 0,
            BotSessions = traffic?.TotalBotSessionCount ?? 0,
            PagesPerSession = traffic?.PagesPerSessionPercentage ?? 0,

            AverageScrollDepth =
                scrollDepth?.AverageScrollDepth ?? 0,

            EngagementTotalTime =
                engagement?.TotalTime ?? 0,

            EngagementActiveTime =
                engagement?.ActiveTime ?? 0,

            DeadClicks = GetSubTotal(metrics, "DeadClickCount"),
            RageClicks = GetSubTotal(metrics, "RageClickCount"),
            Quickbacks = GetSubTotal(metrics, "QuickbackClick"),
            ScriptErrors = GetSubTotal(metrics, "ScriptErrorCount"),
            ErrorClicks = GetSubTotal(metrics, "ErrorClickCount"),
            ExcessiveScrolls = GetSubTotal(metrics, "ExcessiveScroll"),

            RawJson = rawJson
        };

        var breakdowns = BuildBreakdowns(metrics);

        var snapshotId =
            await _clarityRepository.SaveSnapshotAsync(
                snapshot,
                breakdowns,
                cancellationToken);

        return new ClarityImportResult
        {
            SnapshotDate = snapshotDate,
            SnapshotId = snapshotId,
            Sessions = snapshot.TotalSessions,
            DistinctUsers = snapshot.DistinctUsers,
            AverageScrollDepth = snapshot.AverageScrollDepth,
            Saved = true
        };
    }

    private static ClarityMetricInformation? GetFirst(
        IReadOnlyList<ClarityMetricResponse> metrics,
        string metricName)
    {
        return metrics
            .FirstOrDefault(x =>
                x.MetricName.Equals(
                    metricName,
                    StringComparison.OrdinalIgnoreCase))
            ?.Information
            .FirstOrDefault();
    }

    private static int GetSubTotal(
        IReadOnlyList<ClarityMetricResponse> metrics,
        string metricName)
    {
        return GetFirst(metrics, metricName)?.SubTotal ?? 0;
    }

    private static List<ClarityBreakdown> BuildBreakdowns(
        IReadOnlyList<ClarityMetricResponse> metrics)
    {
        var supportedMetrics = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "Browser",
            "Device",
            "OS",
            "Country",
            "PageTitle",
            "ReferrerUrl",
            "PopularPages"
        };

        var result = new List<ClarityBreakdown>();

        foreach (var metric in metrics)
        {
            if (!supportedMetrics.Contains(metric.MetricName))
                continue;

            foreach (var info in metric.Information)
            {
                result.Add(new ClarityBreakdown
                {
                    MetricName = metric.MetricName,
                    Name = info.Name,
                    Url = info.Url,
                    SessionsCount = info.SessionsCount,
                    VisitsCount = info.VisitsCount
                });
            }
        }

        return result;
    }
}