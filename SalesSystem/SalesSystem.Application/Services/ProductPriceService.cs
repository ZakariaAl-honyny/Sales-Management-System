using Microsoft.EntityFrameworkCore;
using SalesSystem.Application.Interfaces;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

/// <summary>
/// Service to get product price by unit type
/// </summary>
public interface IProductPriceService
{
    Task<Result<decimal>> GetPriceByUnitAsync(int productId, UnitType unitType, CancellationToken ct = default);
}

public class ProductPriceService : IProductPriceService
{
    private readonly IUnitOfWork _uow;

    public ProductPriceService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<decimal>> GetPriceByUnitAsync(int productId, UnitType unitType, CancellationToken ct = default)
    {
        var product = await _uow.Products.Query()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null)
        {
            return Result<decimal>.Failure("المنتج غير موجود", ErrorCodes.NotFound);
        }

        return Result<decimal>.Success(product.GetPriceByUnit(unitType));
    }
}
