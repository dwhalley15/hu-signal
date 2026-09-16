using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HuSignal.Controllers
{
    [ApiVersion("1.0")]
    [ApiExplorerSettings(GroupName = "HuSignal")]
    public class HuSignalApiController : HuSignalApiControllerBase
    {
        private readonly IClarityImportService _clarityImportService;
        private readonly IClarityRepository _clarityRepository;

        private readonly IClarityReportingService _clarityReportingService;

        public HuSignalApiController(
            IClarityImportService clarityImportService,
            IClarityRepository clarityRepository,
            IClarityReportingService clarityReportingService)
        {
            _clarityImportService = clarityImportService;
            _clarityRepository = clarityRepository;
            _clarityReportingService = clarityReportingService;
        }

        [HttpPost("clarity/import")]
        [ProducesResponseType(typeof(ClarityImportResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ImportClarity(
            CancellationToken cancellationToken)
        {
            var result =
                await _clarityImportService.ImportDailyAsync(
                    cancellationToken);

            return Ok(result);
        }

        [HttpGet("clarity/latest")]
        [ProducesResponseType(
        typeof(ClarityLatestResponse),
        StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestClarity(
        CancellationToken cancellationToken)
        {
            var snapshot =
                await _clarityRepository.GetLatestAsync(
                    cancellationToken);

            if (snapshot is null)
            {
                return NotFound();
            }

            var breakdowns =
                await _clarityRepository.GetBreakdownsAsync(
                    snapshot.Id,
                    cancellationToken);

            return Ok(new ClarityLatestResponse
            {
                Snapshot = snapshot,
                Breakdowns = breakdowns
            });
        }

        [HttpGet("clarity/monthly")]
        public async Task<IActionResult> GetMonthlyClarity(
        int year,
        int month,
        CancellationToken cancellationToken)
        {
            var from = new DateTime(year, month, 1);

            var to = from
                .AddMonths(1)
                .AddDays(-1);

            var result =
                await _clarityReportingService.GetSummaryAsync(
                    from,
                    to,
                    cancellationToken);

            return Ok(result);
        }

        [HttpGet("clarity/yearly")]
        public async Task<IActionResult> GetYearlyClarity(
            int year,
            CancellationToken cancellationToken)
        {
            var from = new DateTime(year, 1, 1);
            var to = new DateTime(year, 12, 31);

            var result =
                await _clarityReportingService.GetSummaryAsync(
                    from,
                    to,
                    cancellationToken);

            return Ok(result);
        }
    }
}