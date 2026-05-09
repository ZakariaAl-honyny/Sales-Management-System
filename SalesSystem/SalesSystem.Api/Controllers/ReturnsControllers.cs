using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Common;
using System.Security.Claims;

namespace SalesSystem.Api.Controllers;

/// <summary>
/// Sales returns management API
/// </summary>
[ApiController]
[Route("api/v1/sales-returns")]
[Authorize(Policy = "AllStaff")]
public class SalesReturnsController : ControllerBase
{
    private readonly ISalesReturnService _returnService;

    public SalesReturnsController(ISalesReturnService returnService)
    {
        _returnService = returnService;
    }

    /// <summary>
    /// Gets all sales returns with optional filtering and pagination
    /// </summary>
    /// <param name="customerId">Filter by customer ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of sales returns</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SalesReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] int? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _returnService.GetAllAsync(customerId, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a sales return by ID
    /// </summary>
    /// <param name="id">Return ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Return details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SalesReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _returnService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new sales return (automatically posts - increases stock, decreases customer balance)
    /// </summary>
    /// <param name="request">Sales return creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created return</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SalesReturnDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSalesReturnRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _returnService.CreateAsync(request, userId, ct);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) 
            : BadRequest(new { error = result.Error });
    }
}

/// <summary>
/// Purchase returns management API
/// </summary>
[ApiController]
[Route("api/v1/purchase-returns")]
[Authorize(Policy = "ManagerAndAbove")]
public class PurchaseReturnsController : ControllerBase
{
    private readonly IPurchaseReturnService _returnService;

    public PurchaseReturnsController(IPurchaseReturnService returnService)
    {
        _returnService = returnService;
    }

    /// <summary>
    /// Gets all purchase returns with optional filtering and pagination
    /// </summary>
    /// <param name="supplierId">Filter by supplier ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of purchase returns</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PurchaseReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] int? supplierId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await _returnService.GetAllAsync(supplierId, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Gets a purchase return by ID
    /// </summary>
    /// <param name="id">Return ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Return details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PurchaseReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _returnService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new purchase return (automatically posts - decreases stock, decreases supplier balance)
    /// </summary>
    /// <param name="request">Purchase return creation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created return</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PurchaseReturnDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseReturnRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

        var result = await _returnService.CreateAsync(request, userId, ct);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) 
            : BadRequest(new { error = result.Error });
    }
}