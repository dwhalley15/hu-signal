using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

[TableName("HuSignalClaritySnapshot")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class ClaritySnapshot
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("SnapshotDate")]
    public DateTime SnapshotDate { get; set; }

    [Column("RetrievedAtUtc")]
    public DateTime RetrievedAtUtc { get; set; }

    [Column("TotalSessions")]
    public int TotalSessions { get; set; }

    [Column("DistinctUsers")]
    public int DistinctUsers { get; set; }

    [Column("BotSessions")]
    public int BotSessions { get; set; }

    [Column("PagesPerSession")]
    public decimal PagesPerSession { get; set; }

    [Column("AverageScrollDepth")]
    public decimal AverageScrollDepth { get; set; }

    [Column("EngagementTotalTime")]
    public int EngagementTotalTime { get; set; }

    [Column("EngagementActiveTime")]
    public int EngagementActiveTime { get; set; }

    [Column("DeadClicks")]
    public int DeadClicks { get; set; }

    [Column("RageClicks")]
    public int RageClicks { get; set; }

    [Column("Quickbacks")]
    public int Quickbacks { get; set; }

    [Column("ScriptErrors")]
    public int ScriptErrors { get; set; }

    [Column("ErrorClicks")]
    public int ErrorClicks { get; set; }

    [Column("ExcessiveScrolls")]
    public int ExcessiveScrolls { get; set; }

    [Column("RawJson")]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string RawJson { get; set; } = string.Empty;
}

[TableName("HuSignalClarityBreakdown")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public class ClarityBreakdown
{
    [Column("Id")]
    [PrimaryKeyColumn(AutoIncrement = true)]
    public int Id { get; set; }

    [Column("SnapshotId")]
    public int SnapshotId { get; set; }

    [Column("MetricName")]
    public string MetricName { get; set; } = string.Empty;

    [Column("Name")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? Name { get; set; }

    [Column("Url")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? Url { get; set; }

    [Column("SessionsCount")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public int? SessionsCount { get; set; }

    [Column("VisitsCount")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public int? VisitsCount { get; set; }
}