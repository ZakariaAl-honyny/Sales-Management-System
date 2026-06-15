using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/system-account-mappings")]
[Authorize(Policy = "ManagerAndAbove")]
public class SystemAccountMappingsController : ControllerBase
{
    private readonly ISystemAccountService _service;

    public SystemAccountMappingsController(ISystemAccountService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets all system account mappings.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] short? branchId, CancellationToken ct)
    {
        var result = await _service.GetAllMappingsAsync(branchId, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a system account mapping by key.
    /// </summary>
    [HttpGet("by-key/{key}")]
    public async Task<IActionResult> GetByKey(string key, [FromQuery] short? branchId, CancellationToken ct)
    {
        if (!Enum.TryParse<SalesSystem.Domain.Accounting.Enums.SystemAccountKey>(key, out var mappingKey))
            return BadRequest(new { error = "مفتاح الربط غير صالح" });

        var result = await _service.GetMappingAsync(mappingKey, branchId, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new system account mapping.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSystemAccountMappingRequest request, CancellationToken ct)
    {
        var result = await _service.CreateMappingAsync(request, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetByKey), new { key = result.Value!.MappingKey.ToString() }, result.Value);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing system account mapping.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSystemAccountMappingRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateMappingAsync(id, request, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes (soft-deletes) a system account mapping.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteMappingAsync(id, ct);
        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
