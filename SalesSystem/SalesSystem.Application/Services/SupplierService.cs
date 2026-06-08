using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SupplierService> _logger;
    private readonly IAccountingIntegrationService _accountingService;

    public SupplierService(IUnitOfWork uow, ILogger<SupplierService> logger, IAccountingIntegrationService accountingService)
    {
        _uow = uow;
        _logger = logger;
        _accountingService = accountingService;
    }

    public async Task<Result<SupplierDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var supplier = await _uow.Suppliers.FirstOrDefaultAsync(
            s => s.Id == id, ct, "Account");
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
            predicate = sup => sup.Name.Contains(s) || (sup.Phone != null && sup.Phone.Contains(s));
        }

        var (items, total) = await _uow.Suppliers.GetPagedAsync(
            predicate, q => q.OrderBy(s => s.Name), page, pageSize, ct, includeInactive, "Account");

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<SupplierDto>>.Success(PagedResult<SupplierDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<SupplierDto>> CreateAsync(CreateSupplierRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Step 1: Auto-create account under AP parent if AccountId not provided
            int? accountId = request.AccountId;
            if (accountId == null)
            {
                var accountResult = await AutoCreateSupplierAccountAsync(request.Name, userId, ct);
                if (!accountResult.IsSuccess)
                    return Result<SupplierDto>.Failure(accountResult.Error!, accountResult.ErrorCode);
                accountId = accountResult.Value;
            }

            // Step 2: Create supplier with the account ID
            var supplier = Supplier.Create(
                name: request.Name,
                openingBalance: request.OpeningBalance,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                creditLimit: request.CreditLimit,
                createdByUserId: userId,
                accountId: accountId
            );

            if (request.OpeningBalance > 0)
            {
                // Use transaction for atomicity — supplier + journal entry
                await _uow.ExecuteTransactionAsync(async () =>
                {
                    await _uow.Suppliers.AddAsync(supplier, ct);
                    await _uow.SaveChangesAsync(ct);

                    var entryResult = await _accountingService.CreateSupplierOpeningEntryAsync(
                        supplier.Id,
                        supplier.Name,
                        request.OpeningBalance,
                        createdByUserId: userId,
                        DateTime.UtcNow,
                        ct);

                    if (!entryResult.IsSuccess)
                        throw new DomainException(entryResult.Error!);
                }, ct);
            }
            else
            {
                // No opening balance — simple save without transaction
                await _uow.Suppliers.AddAsync(supplier, ct);
                await _uow.SaveChangesAsync(ct);
            }

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

    public async Task<Result<SupplierDto>> UpdateAsync(int id, UpdateSupplierRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var supplier = await _uow.Suppliers.FirstOrDefaultIgnoreFiltersAsync(
                s => s.Id == id, ct, "Account");
            if (supplier == null)
                return Result<SupplierDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

            supplier.Update(
                name: request.Name,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                creditLimit: request.CreditLimit,
                updatedByUserId: userId,
                accountId: request.AccountId
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
    /// </summary>
    private async Task<Result<int>> AutoCreateSupplierAccountAsync(string supplierName, int userId, CancellationToken ct)
    {
        try
        {
            // Get SystemAccountMappings to find AP parent account
            var mappings = await _uow.SystemAccountMappings.FirstOrDefaultAsync(_ => true, ct);
            if (mappings == null)
                return Result<int>.Failure("لم يتم تهيئة دليل الحسابات بعد", ErrorCodes.NotFound);

            // Get the AP account (Level 4 detail account, e.g. 1321)
            var apAccount = await _uow.Accounts.GetByIdAsync(mappings.AccountsPayableAccountId, ct);
            if (apAccount == null || apAccount.ParentAccountId == null)
                return Result<int>.Failure("لم يتم العثور على حساب الموردين", ErrorCodes.NotFound);

            // Get the parent account (Level 3, e.g. 1320 - الموردون)
            var apParentAccount = await _uow.Accounts.GetByIdAsync(apAccount.ParentAccountId.Value, ct);
            if (apParentAccount == null)
                return Result<int>.Failure("لم يتم العثور على حساب الموردين الرئيسي", ErrorCodes.NotFound);

            // Generate next account code under this parent
            var nextCode = await GenerateNextAccountCodeAsync(apParentAccount.Id, apParentAccount.AccountCode, ct);

            // Create the new account
            var newAccount = Account.Create(
                accountCode: nextCode,
                nameAr: supplierName,
                nameEn: supplierName,
                accountType: AccountType.Liability,
                level: 4,
                parentAccountId: apParentAccount.Id,
                isSystemAccount: false,
                description: $"حساب مورد: {supplierName}",
                colorCode: "#F44336",
                allowTransactions: true,
                openingBalance: 0,
                explanation: $"حساب تلقائي للمورد {supplierName}",
                createdByUserId: userId
            );

            await _uow.Accounts.AddAsync(newAccount, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Auto-created supplier account: {Code} - {Name} under parent {ParentCode}", nextCode, supplierName, apParentAccount.AccountCode);
            return Result<int>.Success(newAccount.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-create supplier account for {SupplierName}", supplierName);
            return Result<int>.Failure("فشل إنشاء الحساب المحاسبي للمورد");
        }
    }

    /// <summary>
    /// Generates the next available account code under a parent account.
    /// For example, under parent "1130", existing children "1131","1132" produce "1133".
    /// </summary>
    private async Task<string> GenerateNextAccountCodeAsync(int parentAccountId, string parentCode, CancellationToken ct)
    {
        // Get all child accounts under this parent
        var childAccounts = await _uow.Accounts.ToListAsync(
            predicate: a => a.ParentAccountId == parentAccountId,
            ct: ct);

        // Get the max existing child code as integer
        int maxSuffix = 0;
        foreach (var child in childAccounts)
        {
            if (int.TryParse(child.AccountCode, out var code))
            {
                if (code > maxSuffix)
                    maxSuffix = code;
            }
        }

        // Generate next code: increment max code, or append "1" to parent code if no children yet
        return maxSuffix > 0
            ? (maxSuffix + 1).ToString()
            : parentCode + "1";
    }

    private static SupplierDto MapToDto(Supplier s)
    {
        return new SupplierDto(
            s.Id,
            s.Name,
            s.Phone,
            s.Email,
            s.Address,
            s.TaxNumber,
            s.OpeningBalance,
            s.CurrentBalance,
            s.CreditLimit,
            s.IsActive,
            AccountId: s.AccountId,
            AccountName: s.Account?.NameAr
        );
    }
}
