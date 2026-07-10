using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Application.Services;

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AccountService> _logger;
    private readonly IAccountCodeGeneratorService _codeGenerator;

    public AccountService(
        IUnitOfWork uow,
        ILogger<AccountService> logger,
        IAccountCodeGeneratorService codeGenerator)
    {
        _uow = uow;
        _logger = logger;
        _codeGenerator = codeGenerator;
    }

    public async Task<Result<List<AccountTreeNodeDto>>> GetTreeAsync(CancellationToken ct)
    {
        var all = await _uow.Accounts.ToListAsync(ct);
        var roots = all.Where(a => a.ParentId == null && a.IsActive)
            .OrderBy(a => a.AccountCode)
            .Select(a => BuildTreeNode(a, all))
            .ToList();
        return Result<List<AccountTreeNodeDto>>.Success(roots);
    }

    private static AccountTreeNodeDto BuildTreeNode(Account account, List<Account> all)
    {
        return new AccountTreeNodeDto(
            account.Id,
            account.AccountCode,
            account.NameAr,
            account.Nature,
            account.IsLeaf,
            account.CategoryId,
            account.Level,
            account.ColorCode,
            account.Description,
            all.Where(c => c.ParentId == account.Id && c.IsActive)
                .OrderBy(c => c.AccountCode)
                .Select(c => BuildTreeNode(c, all))
                .ToList()
        );
    }

    public async Task<Result<List<AccountDto>>> GetAllAsync(CancellationToken ct)
    {
        var accounts = await _uow.Accounts.ToListAsync(ct, "ParentAccount");
        var dtos = accounts.Where(a => a.IsActive)
            .OrderBy(a => a.AccountCode)
            .Select(MapToDto)
            .ToList();
        return Result<List<AccountDto>>.Success(dtos);
    }

    public async Task<Result<AccountDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var account = await _uow.Accounts.FirstOrDefaultAsync(
            a => a.Id == id, ct: ct, "ParentAccount");
        if (account == null)
            return Result<AccountDto>.Failure("الحساب غير موجود", ErrorCodes.NotFound);
        return Result<AccountDto>.Success(MapToDto(account));
    }

    public async Task<Result<List<AccountDto>>> GetByTypeAsync(AccountType type, CancellationToken ct)
    {
        byte nature = (byte)type;
        var accounts = await _uow.Accounts.ToListAsync(
            a => a.Nature == nature && a.IsActive,
            ct: ct,
            includePaths: new[] { "ParentAccount" });
        var dtos = accounts.OrderBy(a => a.AccountCode).Select(MapToDto).ToList();
        return Result<List<AccountDto>>.Success(dtos);
    }

    public async Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, int userId, CancellationToken ct)
    {
        // Step 1: Validate parent if provided
        Account? parent = null;
        if (request.ParentId.HasValue)
        {
            parent = await _uow.Accounts.GetByIdAsync(request.ParentId.Value, ct);
            if (parent == null)
                return Result<AccountDto>.Failure("الحساب الأب غير موجود", ErrorCodes.NotFound);
            if (!parent.IsActive)
                return Result<AccountDto>.Failure("الحساب الأب غير نشط", ErrorCodes.InvalidOperation);
            if (parent.IsLeaf)
                return Result<AccountDto>.Failure(
                    "لا يمكن إضافة حساب فرعي لحساب تفصيلي — الحساب الأب يجب أن يكون مجموعة",
                    ErrorCodes.ValidationError);
        }

        // Step 2: Check for duplicate account by parent + nameAr
        if (request.ParentId.HasValue)
        {
            var duplicateName = await _uow.Accounts.AnyAsync(
                a => a.ParentId == request.ParentId.Value && a.NameAr == request.NameAr.Trim() && a.IsActive, ct);
            if (duplicateName)
                return Result<AccountDto>.Failure(
                    $"يوجد حساب بنفس الاسم '{request.NameAr}' تحت نفس الحساب الأب",
                    ErrorCodes.DuplicateEntry);
        }

        // Step 3: Compute level
        byte level;
        if (request.ParentId.HasValue && parent != null)
        {
            level = (byte)(parent.Level + 1);
        }
        else
        {
            level = 1;
        }

        // Leaf accounts must be detail level (4)
        if (request.IsLeaf && level < 4)
            level = 4;

        // Ensure non-leaf accounts don't exceed level 3
        if (!request.IsLeaf && level > 3)
            level = 3;

        // Step 4: Generate account code
        var codeResult = await _codeGenerator.GenerateCodeAsync(request.ParentId, level, ct);
        if (!codeResult.IsSuccess)
            return Result<AccountDto>.Failure(codeResult.Error!, codeResult.ErrorCode);

        // Step 5: Auto-generate color code from nature
        var colorCode = IAccountCodeGeneratorService.GetColorCode(request.Nature);

        // Step 6: Create the account entity
        var account = Account.Create(
            accountCode: codeResult.Value!,
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            nature: request.Nature,
            isLeaf: request.IsLeaf,
            parentId: request.ParentId,
            isSystem: request.IsSystem,
            categoryId: request.CategoryId,
            level: level,
            description: request.Description,
            colorCode: colorCode,
            notes: request.Notes,
            createdByUserId: userId);

        // Step 7: Persist atomically — account + optional opening balance journal entry
        await _uow.ExecuteTransactionAsync(async () =>
        {
            await _uow.Accounts.AddAsync(account, ct);
            await _uow.SaveChangesAsync(ct);

            // If OpeningBalance > 0, create a Journal Entry
            if (request.OpeningBalance.HasValue && request.OpeningBalance.Value > 0)
            {
                var systemMapping = await _uow.SystemAccountMappings.FirstOrDefaultAsync(
                    m => m.MappingKey == SystemAccountKey.OpeningBalanceEquity.ToString(), ct);

                // Resolve fiscal year from the current date
                var fiscalYear = await _uow.FiscalYears.FirstOrDefaultAsync(
                    fy => fy.IsActive, ct);
                short fiscalYearId = fiscalYear?.Id != null && fiscalYear.Id > 0
                    ? fiscalYear.Id
                    : (short)DateTime.UtcNow.Year;

                var entryNumber = $"OB-{DateTime.UtcNow:yyyyMMdd}-{account.Id:D4}";

                var je = JournalEntry.Create(
                    entryNumber: entryNumber,
                    entryNo: account.Id,
                    entryDate: DateTime.UtcNow,
                    description: $"الرصيد الافتتاحي للحساب {account.NameAr}",
                    entryType: JournalEntryType.OpeningBalance,
                    fiscalYearId: fiscalYearId,
                    createdBy: userId,
                    referenceType: "Account",
                    referenceId: account.Id);

                // Asset/Expense accounts are debit-normal: Dr the account, Cr OpeningBalanceEquity
                if (account.IsDebitNormal())
                {
                    je.AddDebitLine(account.Id, request.OpeningBalance.Value, "الرصيد الافتتاحي");
                    if (systemMapping != null)
                        je.AddCreditLine(systemMapping.AccountId, request.OpeningBalance.Value, "الرصيد الافتتاحي");
                }
                else
                {
                    // Liability/Equity/Revenue are credit-normal: Cr the account, Dr OpeningBalanceEquity
                    je.AddCreditLine(account.Id, request.OpeningBalance.Value, "الرصيد الافتتاحي");
                    if (systemMapping != null)
                        je.AddDebitLine(systemMapping.AccountId, request.OpeningBalance.Value, "الرصيد الافتتاحي");
                }

                await _uow.JournalEntries.AddAsync(je, ct);
                await _uow.SaveChangesAsync(ct);
            }
        }, ct);

        _logger.LogInformation(
            "Account {AccountCode} ({NameAr}) created — Level {Level}, Nature {Nature}, IsLeaf {IsLeaf}",
            account.AccountCode, account.NameAr, account.Level, account.Nature, account.IsLeaf);

        // Reload to get parent name
        var saved = await _uow.Accounts.FirstOrDefaultAsync(
            a => a.Id == account.Id, ct: ct, "ParentAccount");
        return Result<AccountDto>.Success(MapToDto(saved!));
    }

    public async Task<Result<AccountDto>> UpdateAsync(int id, UpdateAccountRequest request, int userId, CancellationToken ct)
    {
        var account = await _uow.Accounts.GetByIdAsync(id, ct);
        if (account == null)
            return Result<AccountDto>.Failure("الحساب غير موجود", ErrorCodes.NotFound);

        if (account.IsSystem)
            return Result<AccountDto>.Failure("لا يمكن تعديل حساب نظامي", ErrorCodes.InvalidOperation);

        // Compute level from parent
        Account? parent = null;
        byte level;
        if (request.ParentId.HasValue)
        {
            parent = await _uow.Accounts.GetByIdAsync(request.ParentId.Value, ct);
            if (parent == null)
                return Result<AccountDto>.Failure("الحساب الأب غير موجود", ErrorCodes.NotFound);
            if (parent.IsLeaf)
                return Result<AccountDto>.Failure(
                    "لا يمكن إضافة حساب فرعي لحساب تفصيلي",
                    ErrorCodes.ValidationError);

            level = (byte)(parent.Level + 1);
        }
        else
        {
            level = 1;
        }

        // Leaf accounts must be detail level (4)
        if (request.IsLeaf && level < 4)
            level = 4;

        // Non-leaf accounts must not exceed level 3
        if (!request.IsLeaf && level > 3)
            level = 3;

        // Auto-generate color code from nature
        var colorCode = IAccountCodeGeneratorService.GetColorCode(request.Nature);

        account.Update(
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            nature: request.Nature,
            isLeaf: request.IsLeaf,
            parentId: request.ParentId,
            categoryId: request.CategoryId,
            level: level,
            description: request.Description,
            colorCode: colorCode,
            notes: request.Notes,
            updatedByUserId: userId);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Account {Id} ({AccountCode}) updated — Level {Level}, Nature {Nature}",
            id, account.AccountCode, account.Level, account.Nature);

        // Reload to get parent name
        var updated = await _uow.Accounts.FirstOrDefaultAsync(
            a => a.Id == id, ct: ct, "ParentAccount");
        return Result<AccountDto>.Success(MapToDto(updated!));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var account = await _uow.Accounts.GetByIdAsync(id, ct);
        if (account == null)
            return Result.Failure("الحساب غير موجود", ErrorCodes.NotFound);

        if (account.IsSystem)
            return Result.Failure("لا يمكن حذف حساب نظامي", ErrorCodes.InvalidOperation);

        // Use database query instead of HasChildren() — navigation property is not eagerly loaded
        var hasChildren = await _uow.Accounts.AnyAsync(
            a => a.ParentId == id, ct);
        if (hasChildren)
            return Result.Failure(
                "لا يمكن حذف حساب رئيسي لديه حسابات فرعية — احذف الحسابات الفرعية أولاً",
                ErrorCodes.ReferencedByOtherEntities);

        var hasTransactions = await _uow.JournalEntryLines.AnyAsync(l => l.AccountId == id, ct);
        if (hasTransactions)
            return Result.Failure(
                "لا يمكن إلغاء تنشيط حساب مسجل عليه حركات مالية أو أرصدة",
                ErrorCodes.ReferencedByOtherEntities);

        account.MarkAsDeleted();
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Account {Id} ({AccountCode}) soft-deleted", id, account.AccountCode);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        try
        {
            var account = await _uow.Accounts.GetByIdAsync(id, ct);
            if (account == null)
                return Result.Failure("الحساب غير موجود", ErrorCodes.NotFound);

            if (account.IsSystem)
                return Result.Failure("لا يمكن حذف حساب نظامي", ErrorCodes.InvalidOperation);

            var hasChildren = await _uow.Accounts.AnyAsync(
                a => a.ParentId == id, ct);
            if (hasChildren)
                return Result.Failure(
                    "لا يمكن حذف حساب رئيسي لديه حسابات فرعية",
                    ErrorCodes.ReferencedByOtherEntities);

            var hasTransactions = await _uow.JournalEntryLines.AnyAsync(l => l.AccountId == id, ct);
            if (hasTransactions)
                return Result.Failure(
                    "لا يمكن حذف حساب مسجل عليه حركات مالية نهائياً",
                    ErrorCodes.ReferencedByOtherEntities);

            _uow.Accounts.DeleteRange(new[] { account });
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Account {Id} permanently deleted", id);
            return Result.Success();
        }
        catch (Exception ex) when (ex is System.InvalidOperationException || IsDbUpdateException(ex))
        {
            _logger.LogError(ex, "Cannot permanently delete Account {Id}: {Message}", id, ex.InnerException?.Message ?? ex.Message);
            return Result.Failure("لا يمكن حذف هذا الحساب لأنه مرتبط بمعاملات أخرى", ErrorCodes.ReferencedByOtherEntities);
        }
    }

    private static AccountDto MapToDto(Account a)
    {
        return new AccountDto(
            a.Id,
            a.AccountCode,
            a.NameAr,
            a.NameEn,
            a.Nature,
            a.IsLeaf,
            a.ParentId,
            a.ParentAccount?.NameAr,
            a.IsSystem,
            a.IsActive,
            a.CategoryId,
            a.Level,
            a.Description,
            a.ColorCode,
            a.Notes);
    }

    /// <summary>
    /// Checks if the exception is a database update exception by type name.
    /// Avoids direct dependency on EF Core in the Application layer.
    /// </summary>
    private static bool IsDbUpdateException(Exception ex)
    {
        var typeName = ex.GetType().FullName ?? "";
        return typeName.Contains("DbUpdateException", StringComparison.Ordinal);
    }
}
