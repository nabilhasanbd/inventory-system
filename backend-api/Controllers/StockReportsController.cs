using backend_api.DTOs;
using backend_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers;

[ApiController]
[Route("api/stockreports")]
public class StockReportsController : ControllerBase
{
    private readonly IStockReportService _stockReportService;

    public StockReportsController(IStockReportService stockReportService)
    {
        _stockReportService = stockReportService;
    }

    [HttpGet("movement")]
    public async Task<ActionResult<IEnumerable<StockMovementReportRowDto>>> GetStockMovement(
        [FromQuery] StockMovementReportFilterDto filter,
        CancellationToken ct)
    {
        var report = await _stockReportService.GetStockMovementAsync(filter, ct);
        return Ok(report);
    }
}
