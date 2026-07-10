using Microsoft.Extensions.Logging;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class BankService : IBankService
{
    private readonly IUnitOfWork _uow;
    private readonly IAccountLinkService _accountLink;
    private readonly ILogger<BankService> _logger;

    /// <summary>
    /// Parent account code for Bank sub-accounts (Level 3 under Current Assets).
    /// </summary>
    private const string BankParentAccountCode = "1102";

    public BankService(
        IUnitOfWork uow,
        IAccountLinkService accountLink,
        ILogger<BankService> logger)
    {
        _uow = uow;
        _accountLink = accountLink;
        _logger = logger;
    }

    public async Task<Result<List<BankDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var banks = await _uow.Banks.ToListAsync(ct, "Account");
            var dtos = banks.Select(MapToDto).ToList();
            return Result<List<BankDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all banks");
            return Result<List<BankDto>>.Failure("حدث خطأ أثناء استرجاع قائمة البنوك");
        }
    }

    public async Task<Result<BankDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var bank = await _uow.Banks.FirstOrDefaultAsync(b => b.Id == id, ct, "Account");
            if (bank == null)
                return Result<BankDto>.Failure("البنك غير موجود", ErrorCodes.NotFound);

            return Result<BankDto>.Success(MapToDto(bank));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bank {Id}", id);
            return Result<BankDto>.Failure("حدث خطأ أثناء استرجاع بيانات البنك");
        }
    }

    public async Task<Result<BankDto>> CreateAsync(CreateBankRequest request, int userId, CancellationToken ct)
    {
        return await _uow.ExecuteTransactionAsync<Result<BankDto>>(async () =>
        {
            try
            {
                int accountId;

                // Resolve the chart-of-accounts account: either use the provided AccountId
                // or auto-create a Level-4 detail account under parent "1120 — البنوك".
                if (request.AccountId.HasValue && request.AccountId.Value > 0)
                {
                    accountId = request.AccountId.Value;

                    var accountExists = await _uow.Accounts.AnyAsync(
                        a => a.Id == accountId, ct);
                    if (!accountExists)
                        return Result<BankDto>.Failure(
                            "الحساب المحاسبي المحدد غير موجود", ErrorCodes.NotFound);
                }
                else
                {
                    var accountResult = await AutoCreateBankAccountAsync(request.Name, userId, ct);
                    if (!accountResult.IsSuccess || accountResult.Value == null)
                        return Result<BankDto>.Failure(
                            accountResult.Error ?? "فشل إنشاء الحساب المحاسبي للبنك");

                    accountId = accountResult.Value.Id;
                }

                // Create the bank domain entity — AccountId is always resolved before this call
                var bank = Bank.Create(
                    request.Name,
                    accountId,
                    accountNumber: request.AccountNumber,
                    iban: request.Iban,
                    createdByUserId: userId);

                await _uow.Banks.AddAsync(bank, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Bank created: {Name} (ID: {Id}, AccountId: {AccountId}) by User {UserId}",
                    bank.Name, bank.Id, bank.AccountId, userId);

                // Reload with Account navigation property for the response
                var created = await _uow.Banks.FirstOrDefaultAsync(
                    b => b.Id == bank.Id, ct, "Account");

                return Result<BankDto>.Success(MapToDto(created!));
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation creating bank: {Message}", ex.Message);
                return Result<BankDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bank");
                return Result<BankDto>.Failure("حدث خطأ أثناء إنشاء البنك");
            }
        }, ct);
    }

    public async Task<Result<BankDto>> UpdateAsync(int id, UpdateBankRequest request, int userId, CancellationToken ct)
    {
        // ── Guard checks (OUTSIDE transaction) ──────────────────────────
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<BankDto>.Failure("اسم البنك مطلوب");

        var bank = await _uow.Banks.FirstOrDefaultAsync(b => b.Id == id, ct, "Account");
        if (bank == null)
            return Result<BankDto>.Failure("البنك غير موجود", ErrorCodes.NotFound);

        var nameChanged = !string.Equals(request.Name.Trim(), bank.Name, StringComparison.Ordinal);

        return await _uow.ExecuteTransactionAsync<Result<BankDto>>(async () =>
        {
            try
            {
                // 1. Update the Bank entity
                bank.Update(
                    request.Name,
                    accountNumber: request.AccountNumber,
                    iban: request.Iban,
                    updatedByUserId: userId);

                // 2. Sync the linked Account name when the bank name changes
                if (nameChanged)
                {
                    await _accountLink.SyncNameAsync(bank.AccountId, request.Name, ct);
                }

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Bank updated: {Name} (ID: {Id}) by User {UserId}",
                    bank.Name, id, userId);

                return Result<BankDto>.Success(MapToDto(bank));
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation updating bank {Id}: {Message}", id, ex.Message);
                return Result<BankDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bank {Id}", id);
                return Result<BankDto>.Failure("حدث خطأ أثناء تحديث بيانات البنك");
            }
        }, ct);
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        // ── Guard checks (OUTSIDE transaction) ──────────────────────────
        var bank = await _uow.Banks.GetByIdAsync(id, ct);
        if (bank == null)
            return Result.Failure("البنك غير موجود", ErrorCodes.NotFound);

        return await _uow.ExecuteTransactionAsync<Result>(async () =>
        {
            try
            {
                // 1. Deactivate the linked Account
                await _accountLink.DeactivateAsync(bank.AccountId, ct);

                // 2. Soft-delete the Bank entity
                bank.MarkAsDeleted();
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Bank deactivated: {Name} (ID: {Id})", bank.Name, id);
                return Result.Success();
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation deactivating bank {Id}: {Message}", id, ex.Message);
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating bank {Id}", id);
                return Result.Failure("حدث خطأ أثناء إلغاء تنشيط البنك");
            }
        }, ct);
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        // ── Guard checks (OUTSIDE transaction) ──────────────────────────
        var bank = await _uow.Banks.FirstOrDefaultIgnoreFiltersAsync(b => b.Id == id, ct, "Account");
        if (bank == null)
            return Result.Failure("البنك غير موجود", ErrorCodes.NotFound);

        return await _uow.ExecuteTransactionAsync<Result>(async () =>
        {
            try
            {
                // 1. Soft-delete the linked Account (permanent removal reference-safe)
                await _accountLink.MarkAsDeletedAsync(bank.AccountId, ct);

                // 2. Hard-delete the Bank entity
                _uow.Banks.DeleteRange(new[] { bank });
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Bank permanently deleted: {Name} (ID: {Id}), Account {AccountId}",
                    bank.Name, bank.Id, bank.AccountId);

                return Result.Success();
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation permanently deleting bank {Id}: {Message}", id, ex.Message);
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting bank {Id}", id);
                return Result.Failure("حدث خطأ أثناء الحذف النهائي للبنك");
            }
        }, ct);
    }

    public async Task<Result> RestoreAsync(int id, CancellationToken ct)
    {
        // ── Guard checks (OUTSIDE transaction) ──────────────────────────
        var bank = await _uow.Banks.FirstOrDefaultIgnoreFiltersAsync(b => b.Id == id, ct, "Account");
        if (bank == null)
            return Result.Failure("البنك غير موجود", ErrorCodes.NotFound);

        if (bank.IsActive)
            return Result.Failure("البنك نشط بالفعل");

        return await _uow.ExecuteTransactionAsync<Result>(async () =>
        {
            try
            {
                // 1. Reactivate the linked Account
                await _accountLink.ActivateAsync(bank.AccountId, ct);

                // 2. Restore the Bank entity
                bank.Restore();
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Bank restored: {Name} (ID: {Id}) by User",
                    bank.Name, bank.Id);

                return Result.Success();
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation restoring bank {Id}: {Message}", id, ex.Message);
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring bank {Id}", id);
                return Result.Failure("حدث خطأ أثناء استعادة البنك");
            }
        }, ct);
    }

    /// <summary>
    /// Auto-creates a Level-4 detail account under parent "1120 — البنوك"
    /// (Bank Accounts) for a new bank.
    /// Account code auto-increments from existing child codes.
    /// </summary>
    private async Task<Result<Account>> AutoCreateBankAccountAsync(
        string bankName, int userId, CancellationToken ct)
    {
        try
        {
            // Find the Bank Accounts parent account (1120)
            var parent = await _uow.Accounts.FirstOrDefaultAsync(
                a => a.AccountCode == BankParentAccountCode, ct);
            if (parent == null)
                return Result<Account>.Failure(
                    "الحساب الرئيسي للبنوك (1120) غير موجود في شجرة الحسابات");

            // Find max child code under parent to auto-increment
            var children = await _uow.Accounts.ToListAsync(
                a => a.ParentId == parent.Id,
                q => q.OrderByDescending(a => a.AccountCode),
                ct: ct);

            var maxCodeStr = children.FirstOrDefault()?.AccountCode
                             ?? parent.AccountCode;
            var newCode = (int.Parse(maxCodeStr) + 1).ToString();

            // Create Level-4 detail account with Asset type
            var account = Account.Create(
                accountCode: newCode,
                nameAr: $"بنك {bankName}",
                nameEn: $"Bank {bankName}",
                nature: (byte)AccountType.Asset,
                isLeaf: true,
                parentId: parent.Id,
                isSystem: false,
                categoryId: null,
                level: 4,
                createdByUserId: userId);

            await _uow.Accounts.AddAsync(account, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Auto-created account for bank '{BankName}': Code={AccountCode}, Id={AccountId}",
                bankName, account.AccountCode, account.Id);

            return Result<Account>.Success(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-create account for bank '{BankName}'", bankName);
            return Result<Account>.Failure("فشل إنشاء الحساب المحاسبي للبنك");
        }
    }

    private static BankDto MapToDto(Bank bank)
    {
        return new BankDto(
            bank.Id,
            bank.AccountId,
            bank.Account?.NameAr ?? bank.Account?.NameEn,
            bank.Account?.AccountCode,
            bank.Name,
            bank.AccountNumber,
            bank.IBAN,
            bank.IsActive
        );
    }
}
