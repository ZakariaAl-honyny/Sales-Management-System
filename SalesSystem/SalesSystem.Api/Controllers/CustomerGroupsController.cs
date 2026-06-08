using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/customer-groups")]
[Authorize]
public class CustomerGroupsController : ControllerBase
{
    private readonly ICustomerGroupService _customerGroupService;

    public CustomerGroupsController(ICustomerGroupService customerGroupService)
    {
        _customerGroupService = customerGroupService;
    }

    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(List<CustomerGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<CustomerGroupDto>>> GetAll(CancellationToken ct)
    {
        var result = await _customerGroupService.GetAllAsync(ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        return BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int:min(1)}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(CustomerGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerGroupDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _customerGroupService.GetByIdAsync(id, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(CustomerGroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerGroupDto>> Create(
        [FromBody] CreateCustomerGroupRequest request,
        [FromServices] IValidator<CreateCustomerGroupRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return BadRequest(new { error = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)) });

        var result = await _customerGroupService.CreateAsync(request, ct);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        return BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int:min(1)}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(CustomerGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCustomerGroupRequest request,
        [FromServices] IValidator<UpdateCustomerGroupRequest> validator,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return BadRequest(new { error = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)) });

        var result = await _customerGroupService.UpdateAsync(id, request, ct);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int:min(1)}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _customerGroupService.DeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم حذف المجموعة بنجاح", id });
        if (result.ErrorCode == ErrorCodes.NotFound)
            return NotFound(new { error = result.Error });
        return BadRequest(new { error = result.Error });
    }
}
