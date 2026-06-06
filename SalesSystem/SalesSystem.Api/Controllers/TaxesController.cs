using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/taxes")]
public class TaxesController : ControllerBase
{
    private readonly ITaxService _taxService;

    public TaxesController(ITaxService taxService)
    {
        _taxService = taxService;
    }

    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(List<TaxDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _taxService.GetAllAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _taxService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaxRequest request, CancellationToken ct)
    {
        var result = await _taxService.CreateAsync(request, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        return BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaxRequest request, CancellationToken ct)
    {
        var result = await _taxService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _taxService.DeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف بنجاح", id });
        return BadRequest(new { error = result.Error });
    }

    [HttpDelete("permanent/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PermanentDelete(int id, CancellationToken ct)
    {
        var result = await _taxService.PermanentDeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف النهائي بنجاح", id });
        return BadRequest(new { error = result.Error });
    }
}
