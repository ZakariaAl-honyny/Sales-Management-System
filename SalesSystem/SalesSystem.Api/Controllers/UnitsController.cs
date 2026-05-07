using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Units;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/units")]
[Authorize(Policy = "ManagerAndAbove")]
public class UnitsController : ControllerBase
{
    private readonly IUnitService _unitService;

    public UnitsController(IUnitService unitService)
    {
        _unitService = unitService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _unitService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _unitService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest request, CancellationToken ct)
    {
        var result = await _unitService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUnitRequest request, CancellationToken ct)
    {
        var result = await _unitService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _unitService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
