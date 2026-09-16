using Umbraco.Cms.Infrastructure.Migrations;

public class CreateClarityTables : AsyncMigrationBase
{
    public CreateClarityTables(IMigrationContext context)
        : base(context)
    {
    }

    protected override Task MigrateAsync()
    {
        if (!TableExists("HuSignalClaritySnapshot"))
        {
            Create.Table("HuSignalClaritySnapshot")
                .WithColumn("Id")
                    .AsInt32()
                    .PrimaryKey("PK_HuSignalClaritySnapshot")
                    .Identity()

                .WithColumn("SnapshotDate")
                    .AsDateTime()
                    .NotNullable()

                .WithColumn("RetrievedAtUtc")
                    .AsDateTime()
                    .NotNullable()

                .WithColumn("TotalSessions")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("DistinctUsers")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("BotSessions")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("PagesPerSession")
                    .AsDecimal()
                    .NotNullable()

                .WithColumn("AverageScrollDepth")
                    .AsDecimal()
                    .NotNullable()

                .WithColumn("EngagementTotalTime")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("EngagementActiveTime")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("DeadClicks")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("RageClicks")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("Quickbacks")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("ScriptErrors")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("ErrorClicks")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("ExcessiveScrolls")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("RawJson")
                    .AsCustom("NVARCHAR(MAX)")
                    .NotNullable()

                .Do();
        }

        if (!TableExists("HuSignalClarityBreakdown"))
        {
            Create.Table("HuSignalClarityBreakdown")
                .WithColumn("Id")
                    .AsInt32()
                    .PrimaryKey("PK_HuSignalClarityBreakdown")
                    .Identity()

                .WithColumn("SnapshotId")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("MetricName")
                    .AsString(255)
                    .NotNullable()

                .WithColumn("Name")
                    .AsString(1000)
                    .Nullable()

                .WithColumn("Url")
                    .AsString(2000)
                    .Nullable()

                .WithColumn("SessionsCount")
                    .AsInt32()
                    .Nullable()

                .WithColumn("VisitsCount")
                    .AsInt32()
                    .Nullable()

                .Do();
        }

        return Task.CompletedTask;
    }
}