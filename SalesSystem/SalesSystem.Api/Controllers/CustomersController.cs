using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Customers;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Customers management API
/// </summary>
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

    /// <summary>
    /// Gets all customers with optional search and pagination
    /// </summary>
    /// <param name="search">Search by name, code, phone, or email</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of customers</returns>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(PagedResult<CustomerDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _customerService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Customer details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _customerService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new customer
    /// </summary>
    /// <param name="request">Customer creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created customer</returns>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(CustomerDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await _customerService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="request">Customer update request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated customer</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(CustomerDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var result = await _customerService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a customer (soft delete)
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _customerService.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok("Customer deleted successfully") : BadRequest(new { error = result.Error });
    }
}