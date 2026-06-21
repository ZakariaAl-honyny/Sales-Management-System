using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

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
        var supplier = await _uow.Suppliers.FirstOrDefaultAsync(
            s => s.Id == id, ct, "Party");
        if (supplier == null)
            return Result<SupplierDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

        return Result<SupplierDto>.Success(MapToDto(supplier));
    }

    public async Task<Result<PagedResult<SupplierDto>>> GetAllAsync(string? search, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        System.Linq.Expressions.Expression<System.Func<Supplier, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search;
            predicate = sup => sup.Party.Name.Contains(s) || (sup.Party.Phone != null && sup.Party.Phone.Contains(s));
        }

        var (items, total) = await _uow.Suppliers.GetPagedAsync(
            predicate, q => q.OrderByDescending(s => s.Id), page, pageSize, ct, includeInactive, "Party");

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<SupplierDto>>.Success(PagedResult<SupplierDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<SupplierDto>> CreateAsync(CreateSupplierRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Step 1: Auto-create account under AP parent (1320 — الموردون)
            var accountResult = await AutoCreateSupplierAccountAsync(request.Name, userId, ct);
            if (!accountResult.IsSuccess)
                return Result<SupplierDto>.Failure(accountResult.Error!, accountResult.ErrorCode);
            var accountId = accountResult.Value;

            // Step 2: Create Party record (Name, Phone, Email, Address, TaxNumber)
            var party = Party.Create(
                name: request.Name,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                createdByUserId: userId);
            await _uow.Parties.AddAsync(party, ct);
            await _uow.SaveChangesAsync(ct);

            // Step 3: Create Supplier record with PartyId FK (separate from Supplier.Id)
            var supplier = Supplier.Create(
                partyId: party.Id,
                accountId: accountId,
                createdByUserId: userId);
            await _uow.Suppliers.AddAsync(supplier, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Supplier created: {SupplierName} (ID: {SupplierId})", party.Name, supplier.Id);

            var saved = await _uow.Suppliers.FirstOrDefaultAsync(
                s => s.Id == supplier.Id, ct, "Party");
            return Result<SupplierDto>.Success(MapToDto(saved!));
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

    public async Task<Result<SupplierDto>> UpdateAsync(int id, UpdateSupplierRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var supplier = await _uow.Suppliers.FirstOrDefaultIgnoreFiltersAsync(
                s => s.Id == id, ct, "Party");
            if (supplier == null)
                return Result<SupplierDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

            // Update Party record (contact data)
            supplier.Party.Update(
                name: request.Name,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                updatedByUserId: userId);

            // Supplier has no domain-specific fields beyond Party contact data.
            // CategoryId is managed through a separate endpoint.
            supplier.Update(updatedByUserId: userId);

            if (request.IsActive != supplier.IsActive)
            {
                if (request.IsActive) supplier.Restore();
                else supplier.MarkAsDeleted();
            }

            await _uow.Suppliers.UpdateAsync(supplier, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Supplier updated: {SupplierName} (ID: {SupplierId})", supplier.Party.Name, supplier.Id);

            var updated = await _uow.Suppliers.FirstOrDefaultAsync(
                s => s.Id == supplier.Id, ct, "Party");
            return Result<SupplierDto>.Success(MapToDto(updated!));
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

    public async Task<Result> DeleteAsync(int id, int userId, CancellationToken ct)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(id, ct);
        if (supplier == null)
            return Result.Failure("المورد غير موجود", ErrorCodes.NotFound);

        await _uow.Suppliers.SoftDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Supplier soft-deleted: {SupplierId} by user {UserId}", id, userId);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, int userId, CancellationToken ct)
    {
        var supplier = await _uow.Suppliers.FirstOrDefaultIgnoreFiltersAsync(s => s.Id == id, ct);
        if (supplier == null)
            return Result.Failure("المورد غير موجود", ErrorCodes.NotFound);

        if (await _uow.PurchaseInvoices.AnyAsync(pi => pi.SupplierId == id, ct))
            return Result.Failure("لا يمكن حذف المورد نهائياً لأنه مرتبط بفواتير شراء");

        if (await _uow.SupplierPayments.AnyAsync(sp => sp.SupplierId == id, ct))
            return Result.Failure("لا يمكن حذف المورد نهائياً لأنه مرتبط بسندات صرف");

        try
        {
            await _uow.Suppliers.HardDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Supplier permanently deleted: {SupplierId} by user {UserId}", id, userId);
            return Result.Success();
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("DbUpdate") || ex.GetType().Name.Contains("Sql"))
        {
            _logger.LogError(ex, "Failed to permanently delete supplier {SupplierId} due to database constraint", id);
            return Result.Failure("لا يمكن حذف المورد نهائياً. قد يكون مرتبطاً ببيانات أخرى في النظام.");
        }
    }

    /// <summary>
    /// Auto-creates a Level 4 Liability account under the AP parent account for this supplier.
    /// Uses the parent account at code "1320 — الموردون" (Accounts Payable).
    /// Falls back to SystemAccountMappings.AccountsPayableAccountId if 1320 not found.
    /// </summary>
    private async Task<Result<int>> AutoCreateSupplierAccountAsync(string supplierName, int userId, CancellationToken ct)
    {
        try
        {
            var apParentAccount = await _uow.Accounts.FirstOrDefaultAsync(
                a => a.AccountCode == "1320" && a.IsActive, ct);

            if (apParentAccount == null)
            {
                var apMapping = await _uow.SystemAccountMappings.FirstOrDefaultAsync(
                    m => m.MappingKey == nameof(SystemAccountKey.AccountsPayable), ct);
                if (apMapping == null)
                    return Result<int>.Failure("لم يتم تهيئة دليل الحسابات بعد", ErrorCodes.NotFound);

                var apAccount = await _uow.Accounts.GetByIdAsync(apMapping.AccountId, ct);
                if (apAccount == null || apAccount.ParentId == null)
                    return Result<int>.Failure("لم يتم العثور على حساب الموردين", ErrorCodes.NotFound);

                apParentAccount = await _uow.Accounts.GetByIdAsync(apAccount.ParentId.Value, ct);
                if (apParentAccount == null)
                    return Result<int>.Failure("لم يتم العثور على حساب الموردين الرئيسي", ErrorCodes.NotFound);
            }

            var nextCode = await GenerateNextAccountCodeAsync(apParentAccount.Id, apParentAccount.AccountCode, ct);

            var newAccount = Account.Create(
                accountCode: nextCode,
                nameAr: supplierName,
                nameEn: supplierName,
                nature: (byte)AccountType.Liability,
                isLeaf: true,
                parentId: apParentAccount.Id,
                isSystem: false,
                categoryId: null,
                level: 4,
                createdByUserId: userId
            );

            await _uow.Accounts.AddAsync(newAccount, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Auto-created supplier account: {Code} - {Name} under parent {ParentCode}",
                nextCode, supplierName, apParentAccount.AccountCode);
            return Result<int>.Success(newAccount.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-create supplier account for {SupplierName}", supplierName);
            return Result<int>.Failure("فشل إنشاء الحساب المحاسبي للمورد");
        }
    }

    private async Task<string> GenerateNextAccountCodeAsync(int parentAccountId, string parentCode, CancellationToken ct)
    {
        var childAccounts = await _uow.Accounts.ToListAsync(
            predicate: a => a.ParentId == parentAccountId,
            ct: ct);

        int maxSuffix = 0;
        foreach (var child in childAccounts)
        {
            if (int.TryParse(child.AccountCode, out var code))
            {
                if (code > maxSuffix)
                    maxSuffix = code;
            }
        }

        return maxSuffix > 0
            ? (maxSuffix + 1).ToString()
            : parentCode + "1";
    }

    private static SupplierDto MapToDto(Supplier s)
    {
        return new SupplierDto(
            s.Id,
            s.Party.Name,
            s.Party.Phone,
            s.Party.Email,
            s.Party.Address,
            s.Party.TaxNumber,
            s.IsActive,
            PartyId: s.PartyId,
            AccountId: s.AccountId,
            CategoryId: s.CategoryId
        );
    }
}
