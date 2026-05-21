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
    private readonly ILogger<SalesReturnsController> _logger;

    public SalesReturnsController(ISalesReturnService returnService, ILogger<SalesReturnsController> logger)
    {
        _returnService = returnService;
        _logger = logger;
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
    public async Task<IActionResult> GetAll(
        [FromQuery] int? customerId, 
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("GetAll sales returns requested. CustomerId={CustomerId}, IncludeInactive={IncludeInactive}, Page={Page}", customerId, includeInactive, page);
        var result = await _returnService.GetAllAsync(customerId, page, pageSize, includeInactive, ct);
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
        _logger.LogInformation("GetById sales return requested. Id={Id}", id);
        var result = await _returnService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new sales return (Draft by default)
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
        if (!int.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("Create sales return failed: Invalid user token");
            return Unauthorized();
        }

        _logger.LogInformation("Create sales return requested. UserId={UserId}, SalesInvoiceId={SalesInvoiceId}, ItemCount={ItemCount}", userId, request.SalesInvoiceId, request.Items.Count);
        var result = await _returnService.CreateAsync(request, userId, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Sales return created successfully. Id={Id}", result.Value!.Id);
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }
        _logger.LogWarning("Sales return creation failed. UserId={UserId}, Error={Error}", userId, result.Error);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Posts a draft sales return (increases stock, decreases customer balance)
    /// </summary>
    [HttpPost("{id:int}/post")]
    [ProducesResponseType(typeof(SalesReturnDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Post(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdStr, out var userId);
        var result = await _returnService.PostAsync(id, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Cancels a sales return (reverses stock and balance if it was posted)
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(SalesReturnDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdStr, out var userId);
        var result = await _returnService.CancelAsync(id, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
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
    private readonly ILogger<PurchaseReturnsController> _logger;

    public PurchaseReturnsController(IPurchaseReturnService returnService, ILogger<PurchaseReturnsController> logger)
    {
        _returnService = returnService;
        _logger = logger;
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
    public async Task<IActionResult> GetAll(
        [FromQuery] int? supplierId, 
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("GetAll purchase returns requested. SupplierId={SupplierId}, IncludeInactive={IncludeInactive}, Page={Page}", supplierId, includeInactive, page);
        var result = await _returnService.GetAllAsync(supplierId, page, pageSize, includeInactive, ct);
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
        _logger.LogInformation("GetById purchase return requested. Id={Id}", id);
        var result = await _returnService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Creates a new purchase return (Draft by default)
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
        if (!int.TryParse(userIdStr, out var userId))
        {
            _logger.LogWarning("Create purchase return failed: Invalid user token");
            return Unauthorized();
        }

        _logger.LogInformation("Create purchase return requested. UserId={UserId}, PurchaseInvoiceId={PurchaseInvoiceId}, ItemCount={ItemCount}", userId, request.PurchaseInvoiceId, request.Items.Count);
        var result = await _returnService.CreateAsync(request, userId, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Purchase return created successfully. Id={Id}", result.Value!.Id);
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }
        _logger.LogWarning("Purchase return creation failed. UserId={UserId}, Error={Error}", userId, result.Error);
        return BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Posts a draft purchase return (decreases stock, decreases supplier balance)
    /// </summary>
    [HttpPost("{id:int}/post")]
    [ProducesResponseType(typeof(PurchaseReturnDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Post(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdStr, out var userId);
        var result = await _returnService.PostAsync(id, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Cancels a purchase return (reverses stock and balance if it was posted)
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(PurchaseReturnDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdStr, out var userId);
        var result = await _returnService.CancelAsync(id, userId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}