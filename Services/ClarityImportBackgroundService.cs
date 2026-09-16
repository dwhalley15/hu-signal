using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

public class ClarityImportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClarityImportBackgroundService> _logger;

    public ClarityImportBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ClarityImportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();

            _logger.LogInformation(
                "Hu Signal Clarity import scheduled in {Delay}",
                delay);

            await Task.Delay(delay, stoppingToken);

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var importService =
                    scope.ServiceProvider
                        .GetRequiredService<IClarityImportService>();

                var result =
                    await importService.ImportDailyAsync(
                        stoppingToken);

                _logger.LogInformation(
                    "Hu Signal imported Clarity snapshot {SnapshotDate} with {Sessions} sessions",
                    result.SnapshotDate,
                    result.Sessions);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Hu Signal Clarity daily import failed");
            }
        }
    }

    private static TimeSpan GetDelayUntilNextRun()
    {
        var now = DateTime.Now;

        var nextRun = now.Date.AddHours(1);

        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - now;
    }
}