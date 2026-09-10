using backend_api.DTOs;
using backend_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers;

[ApiController]
[Route("api/stockreports")]
public class StockReportsController : ControllerBase
{
    private readonly IStockReportService _stockReportService;
    private readonly IStockReportDocumentService _stockReportDocumentService;

    public StockReportsController(
        IStockReportService stockReportService,
        IStockReportDocumentService stockReportDocumentService)
    {
        _stockReportService = stockReportService;
        _stockReportDocumentService = stockReportDocumentService;
    }

    [HttpGet("movement")]
    public async Task<ActionResult<IEnumerable<StockMovementReportRowDto>>> GetStockMovement(
        [FromQuery] StockMovementReportFilterDto filter,
        CancellationToken ct)
    {
        var report = await _stockReportService.GetStockMovementAsync(filter, ct);
        return Ok(report);
    }

    [HttpGet("transaction-details")]
    public async Task<ActionResult<IEnumerable<TransactionDetailReportRowDto>>> GetTransactionDetails(
        [FromQuery] TransactionDetailReportFilterDto filter,
        CancellationToken ct)
    {
        var report = await _stockReportService.GetTransactionDetailsAsync(filter, ct);
        return Ok(report);
    }

    [HttpGet("movement/export")]
    public async Task<IActionResult> ExportStockMovement(
        [FromQuery] StockMovementReportFilterDto filter,
        [FromQuery] string? format,
        CancellationToken ct)
    {
        var report = await _stockReportDocumentService.RenderStockMovementAsync(filter, format, ct);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("transaction-details/export")]
    public async Task<IActionResult> ExportTransactionDetails(
        [FromQuery] TransactionDetailReportFilterDto filter,
        [FromQuery] string? format,
        CancellationToken ct)
    {
        var report = await _stockReportDocumentService.RenderTransactionDetailsAsync(filter, format, ct);
        return File(report.Content, report.ContentType, report.FileName);
    }
}
