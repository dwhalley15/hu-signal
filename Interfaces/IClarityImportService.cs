public interface IClarityImportService
{
    Task<ClarityImportResult> ImportDailyAsync(
        CancellationToken cancellationToken = default);
}