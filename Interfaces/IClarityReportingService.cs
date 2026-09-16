public interface IClarityReportingService
{
    Task<ClarityPeriodSummary> GetSummaryAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}