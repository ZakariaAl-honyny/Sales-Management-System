using MediatR;
using Microsoft.EntityFrameworkCore;
using SalesSystem.Application.Interfaces;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

/// <summary>
/// Query to get product price by unit type
/// </summary>
public record GetProductPriceByUnitQuery(int ProductId, UnitType UnitType) : IRequest<decimal>;

/// <summary>
/// Handler for GetProductPriceByUnitQuery
/// </summary>
public class GetProductPriceByUnitHandler : IRequestHandler<GetProductPriceByUnitQuery, decimal>
{
    private readonly IUnitOfWork _uow;

    public GetProductPriceByUnitHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<decimal> Handle(GetProductPriceByUnitQuery request, CancellationToken ct)
    {
        var product = await _uow.Products.Query()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {request.ProductId} not found.");
        }

        return product.GetPriceByUnit(request.UnitType);
    }
}