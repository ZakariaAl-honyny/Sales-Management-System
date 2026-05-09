using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests.Customers;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Controller for managing customers.
/// </summary>
/// <remarks>
/// - GET endpoints: All Staff roles (Admin, Manager, Cashier)<br/>
/// - POST/PUT/DELETE endpoints: Manager and Admin only (Policy: ManagerAndAbove)
/// </remarks>
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
    /// Retrieves all customers with pagination.
    /// </summary>
    /// <param name="search">Optional search term by customer name or code.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Items per page (default: 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns paginated list of customers.</returns>
    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(PagedResult<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _customerService.GetAllAsync(search, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Retrieves a customer by its ID.
    /// </summary>
    /// <param name="id">Customer ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the customer if found.</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "AllStaff")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _customerService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="request">Create customer request with Name, Code, and optional fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the created customer with ID.</returns>
    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await _customerService.CreateAsync(request, ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    /// <param name="id">Customer ID to update.</param>
    /// <param name="request">Update customer request with all customer fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns the updated customer.</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")] 
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var result = await _customerService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a customer.
    /// </summary>
    /// <param name="id">Customer ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Returns success message with deleted ID.</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _customerService.DeleteAsync(id, ct);
        if (result.IsSuccess)
            return Ok(new { message = "تم الحذف بنجاح", id });
        return BadRequest(new { error = result.Error });
    }
}