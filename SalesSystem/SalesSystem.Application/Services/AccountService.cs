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

    public AccountService(IUnitOfWork uow, ILogger<AccountService> logger)
    {
        _uow = uow;
        _logger = logger;
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
        if (request.ParentId.HasValue)
        {
            var parent = await _uow.Accounts.GetByIdAsync(request.ParentId.Value, ct);
            if (parent == null)
                return Result<AccountDto>.Failure("الحساب الأب غير موجود", ErrorCodes.NotFound);
            if (!parent.IsActive)
                return Result<AccountDto>.Failure("الحساب الأب غير نشط", ErrorCodes.InvalidOperation);
            if (parent.IsLeaf)
                return Result<AccountDto>.Failure(
                    "لا يمكن إضافة حساب فرعي لحساب تفصيلي — الحساب الأب يجب أن يكون مجموعة",
                    ErrorCodes.ValidationError);
        }

        var existing = await _uow.Accounts.FirstOrDefaultAsync(
            a => a.AccountCode == request.AccountCode, ct);
        if (existing != null)
            return Result<AccountDto>.Failure("رمز الحساب موجود مسبقاً", ErrorCodes.DuplicateEntry);

        var account = Account.Create(
            request.AccountCode,
            request.NameAr,
            request.NameEn,
            request.Nature,
            request.IsLeaf,
            request.ParentId,
            request.IsSystem,
            request.CategoryId,
            userId);

        await _uow.Accounts.AddAsync(account, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Account {AccountCode} ({NameAr}) created — Nature {Nature}, IsLeaf {IsLeaf}",
            account.AccountCode, account.NameAr, account.Nature, account.IsLeaf);

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

        if (request.ParentId.HasValue)
        {
            var parent = await _uow.Accounts.GetByIdAsync(request.ParentId.Value, ct);
            if (parent == null)
                return Result<AccountDto>.Failure("الحساب الأب غير موجود", ErrorCodes.NotFound);
            if (parent.IsLeaf)
                return Result<AccountDto>.Failure(
                    "لا يمكن إضافة حساب فرعي لحساب تفصيلي",
                    ErrorCodes.ValidationError);
        }

        account.Update(
            request.NameAr,
            request.NameEn,
            request.Nature,
            request.IsLeaf,
            request.ParentId,
            request.CategoryId,
            userId);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Account {Id} ({AccountCode}) updated", id, account.AccountCode);

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
            a.CategoryId);
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
