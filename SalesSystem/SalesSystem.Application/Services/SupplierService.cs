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
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(IUnitOfWork uow, IDocumentSequenceService sequenceService, ILogger<SupplierService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<SupplierDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(id, ct);
        if (supplier == null)
            return Result<SupplierDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

        return Result<SupplierDto>.Success(MapToDto(supplier));
    }

    public async Task<Result<PagedResult<SupplierDto>>> GetAllAsync(string? search, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _uow.Suppliers.Query();
        
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

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
        try
        {
            string? code = request.Code;
            if (string.IsNullOrWhiteSpace(code))
            {
                var codeResult = await _sequenceService.GetNextNumberAsync("SUP", ct);
                if (!codeResult.IsSuccess)
                    return Result<SupplierDto>.Failure(codeResult.Error ?? "حدث خطأ أثناء توليد الكود");
                code = codeResult.Value;
            }

            // Validate code uniqueness (Regardless of source, including inactive)
            if (await _uow.Suppliers.Query().IgnoreQueryFilters().AnyAsync(s => s.Code == code, ct))
            {
                return Result<SupplierDto>.Failure("كود المورد مستخدم بالفعل (موجود في الأرشيف)", ErrorCodes.DuplicateCode);
            }

            var supplier = Supplier.Create(
                name: request.Name,
                openingBalance: request.OpeningBalance,
                code: code,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                creditLimit: request.CreditLimit,
                createdByUserId: null
            );

            await _uow.Suppliers.AddAsync(supplier, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Supplier created: {SupplierName} (ID: {SupplierId})", supplier.Name, supplier.Id);

            return Result<SupplierDto>.Success(MapToDto(supplier));
        }
        catch (DomainException ex)
        {
            return Result<SupplierDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating supplier");
            return Result<SupplierDto>.Failure("حدث خطأ أثناء إضافة المورد.");
        }
    }

    public async Task<Result<SupplierDto>> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken ct)
    {
        try
        {
            var supplier = await _uow.Suppliers.Query().IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id, ct);
            if (supplier == null)
                return Result<SupplierDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

            if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != supplier.Code)
            {
                if (await _uow.Suppliers.Query().IgnoreQueryFilters().AnyAsync(s => s.Code == request.Code && s.Id != id, ct))
                    return Result<SupplierDto>.Failure("كود المورد مستخدم بالفعل (موجود في الأرشيف)", ErrorCodes.DuplicateCode);
            }

            supplier.Update(
                name: request.Name,
                code: request.Code,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                creditLimit: request.CreditLimit,
                updatedByUserId: null
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
        catch (DomainException ex)
        {
            return Result<SupplierDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating supplier {Id}", id);
            return Result<SupplierDto>.Failure("حدث خطأ أثناء تحديث بيانات المورد.");
        }
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

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        var supplier = await _uow.Suppliers.Query().IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier == null)
            return Result.Failure("المورد غير موجود", ErrorCodes.NotFound);

        if (await _uow.PurchaseInvoices.Query().AnyAsync(pi => pi.SupplierId == id, ct))
            return Result.Failure("لا يمكن حذف المورد نهائياً لأنه مرتبط بفواتير شراء");

        if (await _uow.SupplierPayments.Query().AnyAsync(sp => sp.SupplierId == id, ct))
            return Result.Failure("لا يمكن حذف المورد نهائياً لأنه مرتبط بسندات صرف");

        await _uow.Suppliers.HardDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Supplier permanently deleted: {SupplierId}", id);
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
            s.TaxNumber,
            s.OpeningBalance,
            s.CurrentBalance,
            s.CreditLimit,
            s.IsActive
        );
    }
}
