public interface IClarityService
{
    Task<IReadOnlyList<ClarityMetricResponse>> GetDailyInsightsAsync(
        CancellationToken cancellationToken = default);
}