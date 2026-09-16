public class ClarityMetricResponse
{
    public string MetricName { get; set; } = string.Empty;

    public List<ClarityMetricInformation> Information { get; set; } = [];
}

public class ClarityMetricInformation
{
    public int? SessionsCount { get; set; }

    public decimal? SessionsWithMetricPercentage { get; set; }

    public decimal? SessionsWithoutMetricPercentage { get; set; }

    public int? PagesViews { get; set; }

    public int? SubTotal { get; set; }

    public decimal? AverageScrollDepth { get; set; }

    public int? TotalSessionCount { get; set; }

    public int? TotalBotSessionCount { get; set; }

    public int? DistinctUserCount { get; set; }

    public decimal? PagesPerSessionPercentage { get; set; }

    public int? TotalTime { get; set; }

    public int? ActiveTime { get; set; }

    public string? Name { get; set; }

    public string? Url { get; set; }

    public int? VisitsCount { get; set; }
}

public class ClarityImportResult
{
    public DateTime SnapshotDate { get; set; }

    public int SnapshotId { get; set; }

    public int Sessions { get; set; }

    public int DistinctUsers { get; set; }

    public decimal AverageScrollDepth { get; set; }

    public bool Saved { get; set; }
}

public class ClarityLatestResponse
{
    public ClaritySnapshot Snapshot { get; set; } = default!;

    public IReadOnlyList<ClarityBreakdown> Breakdowns { get; set; }
        = Array.Empty<ClarityBreakdown>();
}

public class ClarityPeriodSummary
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int DaysWithData { get; set; }

    public int TotalSessions { get; set; }

    public int BotSessions { get; set; }

    public decimal AveragePagesPerSession { get; set; }

    public decimal AverageScrollDepth { get; set; }

    public int DeadClicks { get; set; }

    public int RageClicks { get; set; }

    public int Quickbacks { get; set; }

    public int ScriptErrors { get; set; }

    public int ErrorClicks { get; set; }

    public int ExcessiveScrolls { get; set; }

    public IReadOnlyList<ClarityPeriodBreakdown> Breakdowns { get; set; }
        = Array.Empty<ClarityPeriodBreakdown>();
}

public class ClarityPeriodBreakdown
{
    public string MetricName { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? Url { get; set; }

    public int Count { get; set; }
}