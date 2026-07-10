using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

/// <summary>
/// Centralized Account linking service. All Account creation and sync for
/// Customer, Supplier, Employee, CashBox, Bank goes through this service.
/// Per accounts summry.md: entity is Source of Truth; Account is a reflection synced in ONE transaction.
/// </summary>
public class AccountLinkService : IAccountLinkService
{
    private readonly IUnitOfWork _uow;
    private readonly IAccountCodeGeneratorService _codeGenerator;
    private readonly ILogger<AccountLinkService> _logger;

    public AccountLinkService(
        IUnitOfWork uow,
        IAccountCodeGeneratorService codeGenerator,
        ILogger<AccountLinkService> logger)
    {
        _uow = uow;
        _codeGenerator = codeGenerator;
        _logger = logger;
    }

    public async Task<Result<int>> CreateCustomerAccountAsync(string customerName, int createdByUserId, CancellationToken ct)
    {
        try
        {
            // No transaction here — caller wraps in ExecuteTransactionAsync
            return await CreateDetailAccountAsync("1103", nameof(SystemAccountKey.AccountsReceivable),
                customerName, "حساب العميل", createdByUserId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create customer account for {Name}", customerName);
            return Result<int>.Failure("فشل إنشاء الحساب المحاسبي للعميل");
        }
    }

    public async Task<Result<int>> CreateSupplierAccountAsync(string supplierName, int createdByUserId, CancellationToken ct)
    {
        try
        {
            // No transaction here — caller wraps in ExecuteTransactionAsync
            return await CreateDetailAccountAsync("2101", nameof(SystemAccountKey.AccountsPayable),
                supplierName, "حساب المورد", createdByUserId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create supplier account for {Name}", supplierName);
            return Result<int>.Failure("فشل إنشاء الحساب المحاسبي للمورد");
        }
    }

    public async Task<Result<int>> CreateEmployeeAccountAsync(string employeeName, int? createdByUserId, CancellationToken ct)
    {
        try
        {
            // No transaction here — caller wraps in ExecuteTransactionAsync
            return await CreateDetailAccountAsync("1107", nameof(SystemAccountKey.EmployeeCustody),
                employeeName, "حساب عهدة الموظف", createdByUserId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create employee account for {Name}", employeeName);
            return Result<int>.Failure("فشل إنشاء الحساب المحاسبي للموظف");
        }
    }

    public async Task SyncNameAsync(int accountId, string newName, CancellationToken ct)
    {
        var account = await _uow.Accounts.GetByIdAsync(accountId, ct);
        if (account == null)
        {
            _logger.LogWarning("Linked Account {AccountId} not found for name sync", accountId);
            return;
        }

        var nameChanged = !string.Equals(newName, account.NameAr, StringComparison.Ordinal);
        if (!nameChanged) return;

        account.Update(
            nameAr: newName,
            nameEn: newName,
            nature: account.Nature,
            isLeaf: account.IsLeaf,
            parentId: account.ParentId,
            description: account.Description,
            colorCode: account.ColorCode,
            notes: account.Notes,
            categoryId: account.CategoryId,
            level: account.Level);

        await _uow.Accounts.UpdateAsync(account, ct);
    }

    public async Task ActivateAsync(int accountId, CancellationToken ct)
    {
        var account = await _uow.Accounts.GetByIdAsync(accountId, ct);
        if (account == null || account.IsActive) return;

        account.Activate();
        await _uow.Accounts.UpdateAsync(account, ct);
    }

    public async Task DeactivateAsync(int accountId, CancellationToken ct)
    {
        var account = await _uow.Accounts.GetByIdAsync(accountId, ct);
        if (account == null || !account.IsActive) return;

        account.Deactivate();
        await _uow.Accounts.UpdateAsync(account, ct);
    }

    public async Task MarkAsDeletedAsync(int accountId, CancellationToken ct)
    {
        var account = await _uow.Accounts.GetByIdAsync(accountId, ct);
        if (account == null) return;

        account.MarkAsDeleted();
        await _uow.Accounts.UpdateAsync(account, ct);
    }

    /// <summary>
    /// Shared helper: finds parent by code (or fallback via SystemAccountMappings),
    /// generates thread-safe code, auto-sets ColorCode, creates Level 4 detail account.
    /// </summary>
    private async Task<Result<int>> CreateDetailAccountAsync(
        string parentCode, string mappingKey, string entityName,
        string descriptionPrefix, int? createdByUserId, CancellationToken ct)
    {
        // 1. Find parent account
        var parentAccount = await _uow.Accounts.FirstOrDefaultAsync(
            a => a.AccountCode == parentCode && a.IsActive, ct);

        if (parentAccount == null)
        {
            // Fallback: SystemAccountMappings
            var mapping = await _uow.SystemAccountMappings.FirstOrDefaultAsync(
                m => m.MappingKey == mappingKey, ct);
            if (mapping == null)
                return Result<int>.Failure("لم يتم تهيئة دليل الحسابات بعد", ErrorCodes.NotFound);

            var mappedAccount = await _uow.Accounts.GetByIdAsync(mapping.AccountId, ct);
            if (mappedAccount == null || mappedAccount.ParentId == null)
                return Result<int>.Failure("لم يتم العثور على الحساب الرئيسي", ErrorCodes.NotFound);

            parentAccount = await _uow.Accounts.GetByIdAsync(mappedAccount.ParentId.Value, ct);
            if (parentAccount == null)
                return Result<int>.Failure("لم يتم العثور على الحساب الرئيسي", ErrorCodes.NotFound);
        }

        // 2. Generate thread-safe account code
        var codeResult = await _codeGenerator.GenerateCodeAsync(parentAccount.Id, level: 4, ct);
        if (!codeResult.IsSuccess)
            return Result<int>.Failure("فشل توليد رقم الحساب", codeResult.ErrorCode);

        // 3. Auto ColorCode from Nature
        var colorCode = IAccountCodeGeneratorService.GetColorCode(parentAccount.Nature);

        // 4. Create Account with full 13-param signature
        var account = Domain.Accounting.Entities.Account.Create(
            accountCode: codeResult.Value!,
            nameAr: entityName,
            nameEn: entityName,
            nature: parentAccount.Nature,
            isLeaf: true,
            parentId: parentAccount.Id,
            isSystem: false,
            categoryId: null,
            level: 4,
            description: $"{descriptionPrefix}: {entityName}",
            colorCode: colorCode,
            notes: null,
            createdByUserId: createdByUserId);

        await _uow.Accounts.AddAsync(account, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Auto-created account: {Code} - {Name} under parent {ParentCode}",
            codeResult.Value, entityName, parentAccount.AccountCode);

        return Result<int>.Success(account.Id);
    }
}
