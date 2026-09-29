using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class ClarityPdfReportService : IClarityPdfReportService
{
    public byte[] GeneratePdf(
        GenerateClarityReportResponse response)
    {
        var report = response.Report;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);

                page.Margin(40);

                page.DefaultTextStyle(
                    style => style.FontSize(10));

                page.Header()
                    .Column(column =>
                    {
                        column.Spacing(5);

                        column.Item()
                            .Text("Hu Signal")
                            .FontSize(12)
                            .SemiBold();

                        column.Item()
                            .Text(report.Title)
                            .FontSize(22)
                            .Bold();

                        column.Item()
                            .Text(
                                $"{response.From:dd MMMM yyyy} - " +
                                $"{response.To:dd MMMM yyyy}")
                            .FontSize(10);
                    });

                page.Content()
                    .PaddingVertical(20)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        AddSection(
                            column,
                            "Executive Summary",
                            report.ExecutiveSummary);

                        AddSection(
                            column,
                            "Traffic & Engagement",
                            report.TrafficAndEngagement);

                        AddSection(
                            column,
                            "User Behaviour Issues",
                            report.UserBehaviourIssues);

                        AddSection(
                            column,
                            "Popular Content",
                            report.PopularContent);

                        AddSection(
                            column,
                            "Audience & Technology",
                            report.AudienceAndTechnology);

                        AddSection(
                            column,
                            "Referrals",
                            report.Referrals);

                        AddRecommendations(
                            column,
                            report.Recommendations);

                        AddSection(
                            column,
                            "Limitations",
                            report.Limitations);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Hu Signal - ");

                        text.CurrentPageNumber();

                        text.Span(" / ");

                        text.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    private static void AddSection(
        ColumnDescriptor column,
        string title,
        string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        column.Item()
            .Column(section =>
            {
                section.Spacing(8);

                section.Item()
                    .Text(title)
                    .FontSize(16)
                    .SemiBold();

                section.Item()
                    .Text(content)
                    .FontSize(10)
                    .LineHeight(1.4f);
            });
    }

    private static void AddRecommendations(
        ColumnDescriptor column,
        IReadOnlyList<ClarityAiRecommendation> recommendations)
    {
        if (recommendations.Count == 0)
        {
            return;
        }

        column.Item()
            .Column(section =>
            {
                section.Spacing(10);

                section.Item()
                    .Text("Recommended Actions")
                    .FontSize(16)
                    .SemiBold();

                foreach (var recommendation in recommendations)
                {
                    section.Item()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(12)
                        .Column(item =>
                        {
                            item.Spacing(5);

                            item.Item()
                                .Text(
                                    $"{recommendation.Priority} Priority")
                                .FontSize(9)
                                .SemiBold();

                            item.Item()
                                .Text(recommendation.Title)
                                .FontSize(12)
                                .SemiBold();

                            item.Item()
                                .Text(recommendation.Description)
                                .FontSize(10)
                                .LineHeight(1.4f);

                            if (recommendation.RequiresFurtherInvestigation)
                            {
                                item.Item()
                                    .Text("Requires further investigation")
                                    .FontSize(9)
                                    .Italic();
                            }
                        });
                }
            });
    }
}