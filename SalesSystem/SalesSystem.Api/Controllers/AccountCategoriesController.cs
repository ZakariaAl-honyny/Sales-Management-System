using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/account-categories")]
[Authorize(Policy = "AdminOnly")]
public class AccountCategoriesController : ControllerBase
{
    private readonly IAccountCategoryService _service;

    public AccountCategoriesController(IAccountCategoryService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets all account categories.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _service.GetAllAsync(ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets an account category by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new account category.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountCategoryRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing account category.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountCategoryRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        if (result.IsSuccess) return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deactivates (soft-deletes) an account category.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var result = await _service.DeactivateAsync(id, ct);
        if (result.IsSuccess) return Ok();
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
