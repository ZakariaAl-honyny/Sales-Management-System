using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(IUnitOfWork uow, ILogger<SupplierService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<SupplierDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(id, ct);
        if (supplier == null)
            return Result<SupplierDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

        return Result<SupplierDto>.Success(MapToDto(supplier));
    }

    public async Task<Result<PagedResult<SupplierDto>>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = _uow.Suppliers.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Name.Contains(search) || 
                                    (s.Code != null && s.Code.Contains(search)) || 
                                    (s.Phone != null && s.Phone.Contains(search)));
        }

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<SupplierDto>>.Success(PagedResult<SupplierDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<SupplierDto>> CreateAsync(CreateSupplierRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            if (await _uow.Suppliers.Query().AnyAsync(s => s.Code == request.Code, ct))
                return Result<SupplierDto>.Failure("كود المورد مستخدم بالفعل", ErrorCodes.DuplicateCode);
        }

        var supplier = Supplier.Create(
            name: request.Name,
            openingBalance: request.OpeningBalance,
            code: request.Code,
            phone: request.Phone,
            email: request.Email,
            address: request.Address,
            createdByUserId: null
        );

        await _uow.Suppliers.AddAsync(supplier, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Supplier created: {SupplierName} (ID: {SupplierId})", supplier.Name, supplier.Id);

        return Result<SupplierDto>.Success(MapToDto(supplier));
    }

    public async Task<Result<SupplierDto>> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken ct)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(id, ct);
        if (supplier == null)
            return Result<SupplierDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != supplier.Code)
        {
            if (await _uow.Suppliers.Query().AnyAsync(s => s.Code == request.Code && s.Id != id, ct))
                return Result<SupplierDto>.Failure("كود المورد مستخدم بالفعل", ErrorCodes.DuplicateCode);
        }

        supplier.Update(
            request.Name,
            request.Phone,
            request.Email,
            request.Address,
            request.Code,
            null
        );

        if (request.IsActive != supplier.IsActive)
        {
            if (request.IsActive) supplier.Restore();
            else supplier.MarkAsDeleted();
        }

        await _uow.Suppliers.UpdateAsync(supplier, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Supplier updated: {SupplierName} (ID: {SupplierId})", supplier.Name, supplier.Id);

        return Result<SupplierDto>.Success(MapToDto(supplier));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(id, ct);
        if (supplier == null)
            return Result.Failure("المورد غير موجود", ErrorCodes.NotFound);

        await _uow.Suppliers.SoftDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Supplier soft-deleted: {SupplierId}", id);
        return Result.Success();
    }

    private static SupplierDto MapToDto(Supplier s)
    {
        return new SupplierDto(
            s.Id,
            s.Code,
            s.Name,
            s.Phone,
            s.Email,
            s.Address,
            s.OpeningBalance,
            s.CurrentBalance,
            s.IsActive
        );
    }
}
