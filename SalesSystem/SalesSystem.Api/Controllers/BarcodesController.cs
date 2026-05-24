using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesSystem.Application.Interfaces.Services;

namespace SalesSystem.Api.Controllers;

[ApiController]
[Route("api/v1/barcodes")]
[Authorize]
public class BarcodesController : ControllerBase
{
    private readonly IProductUnitService _productUnitService;

    public BarcodesController(IProductUnitService productUnitService)
    {
        _productUnitService = productUnitService;
    }

    [HttpGet("{barcode}")]
    [Authorize(Policy = "AllStaff")]
    public async Task<IActionResult> ResolveBarcode(string barcode, CancellationToken ct)
    {
        var result = await _productUnitService.ResolveBarcodeAsync(barcode, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}