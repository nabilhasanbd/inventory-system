using backend_api.DTOs;
using backend_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly IStoreService _service;

    public StoresController(IStoreService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StoreResponseDto>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StoreResponseDto>> GetById(int id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<StoreResponseDto>> Create([FromBody] CreateStoreDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<StoreResponseDto>> Update(int id, [FromBody] UpdateStoreDto dto, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, dto, ct));

    [HttpPatch("{id:int}/activate")]
    public async Task<ActionResult<StoreResponseDto>> Activate(int id, CancellationToken ct)
        => Ok(await _service.SetActiveStatusAsync(id, true, ct));

    [HttpPatch("{id:int}/deactivate")]
    public async Task<ActionResult<StoreResponseDto>> Deactivate(int id, CancellationToken ct)
        => Ok(await _service.SetActiveStatusAsync(id, false, ct));
}
