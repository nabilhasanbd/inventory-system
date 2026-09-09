using backend_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockBalancesController : ControllerBase
{
    private readonly IStockBalanceService _balanceService;

    public StockBalancesController(IStockBalanceService balanceService)
    {
        _balanceService = balanceService;
    }

    // GET /api/stockbalances?storeId=1&itemId=2 -> { storeId, itemId, quantity }
    [HttpGet]
    public async Task<ActionResult<object>> GetCurrent(
        [FromQuery] int storeId,
        [FromQuery] int itemId,
        CancellationToken ct)
    {
        var quantity = await _balanceService.GetCurrentStockAsync(storeId, itemId, ct);
        return Ok(new { storeId, itemId, quantity });
    }
}
