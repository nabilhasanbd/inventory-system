using backend_api.DTOs;
using backend_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockTransactionsController : ControllerBase
{
    private readonly IStockTransactionService _service;

    public StockTransactionsController(IStockTransactionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StockTransactionResponseDto>>> GetList([FromQuery] StockTransactionFilterDto filter, CancellationToken ct)
        => Ok(await _service.GetListAsync(filter, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StockTransactionResponseDto>> GetById(int id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost("receive")]
    public async Task<ActionResult<StockTransactionResponseDto>> CreateReceive([FromBody] CreateStockTransactionDto dto, CancellationToken ct)
    {
        var created = await _service.CreateReceiveAsync(dto, ct);
        return Created($"/api/stocktransactions/{created.Id}", created);
    }

    [HttpPost("issue")]
    public async Task<ActionResult<StockTransactionResponseDto>> CreateIssue([FromBody] CreateStockTransactionDto dto, CancellationToken ct)
    {
        var created = await _service.CreateIssueAsync(dto, ct);
        return Created($"/api/stocktransactions/{created.Id}", created);
    }
}
