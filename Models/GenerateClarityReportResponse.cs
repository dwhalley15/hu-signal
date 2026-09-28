public class GenerateClarityReportResponse
{
    public string PeriodType { get; set; } = string.Empty;

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public string Report { get; set; } = string.Empty;
}