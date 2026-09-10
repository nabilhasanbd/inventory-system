using backend_api.DTOs;
using backend_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _service;

    public ItemsController(IItemService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemResponseDto>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemResponseDto>> GetById(int id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ItemResponseDto>> Create([FromBody] CreateItemDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ItemResponseDto>> Update(int id, [FromBody] UpdateItemDto dto, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, dto, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/activate")]
    public async Task<ActionResult<ItemResponseDto>> Activate(int id, CancellationToken ct)
        => Ok(await _service.SetActiveStatusAsync(id, true, ct));

    [HttpPatch("{id:int}/deactivate")]
    public async Task<ActionResult<ItemResponseDto>> Deactivate(int id, CancellationToken ct)
        => Ok(await _service.SetActiveStatusAsync(id, false, ct));
}
