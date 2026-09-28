public class GenerateClarityReportRequest
{
    public string PeriodType { get; set; } = "monthly";

    public int Year { get; set; }

    public int? Month { get; set; }
}