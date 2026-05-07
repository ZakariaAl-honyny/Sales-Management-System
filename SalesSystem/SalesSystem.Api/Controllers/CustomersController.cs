using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Customers;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _customerService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _customerService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await _customerService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var result = await _customerService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _customerService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
