using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/products/{productId:int}/units")]
[Authorize]
public class ProductUnitsController : ControllerBase
{
    private readonly IProductUnitService _productUnitService;

    public ProductUnitsController(IProductUnitService productUnitService)
    {
        _productUnitService = productUnitService;
    }

    [HttpGet]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> GetByProductId(int productId, CancellationToken ct)
    {
        var result = await _productUnitService.GetByProductIdAsync(productId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> AddUnit(int productId, [FromBody] AddProductUnitRequest request, CancellationToken ct)
    {
        var result = await _productUnitService.AddUnitAsync(productId, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{unitId:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> UpdateUnit(int productId, int unitId, [FromBody] UpdateProductUnitRequest request, CancellationToken ct)
    {
        var result = await _productUnitService.UpdateUnitAsync(productId, unitId, request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{unitId:int}")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> DeleteUnit(int productId, int unitId, [FromBody] DeleteUnitRequest body, CancellationToken ct)
    {
        var result = await _productUnitService.DeleteUnitAsync(productId, unitId, (DeleteStrategy)body.Strategy, ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [HttpGet("price-history")]
    [Authorize(Policy = "ManagerAndAbove")]
    public async Task<IActionResult> GetPriceHistory(int productId, CancellationToken ct)
    {
        var result = await _productUnitService.GetPriceHistoryAsync(productId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}