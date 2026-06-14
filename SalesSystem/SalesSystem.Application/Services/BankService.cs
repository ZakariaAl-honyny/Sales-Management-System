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
    private readonly ILogger<BankService> _logger;

    /// <summary>
    /// Parent account code for Bank sub-accounts (Level 3 under Assets).
    /// </summary>
    private const string BankParentAccountCode = "1120";

    public BankService(IUnitOfWork uow, ILogger<BankService> logger)
    {
        _uow = uow;
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
        try
        {
            // Validate currency
            var currency = await _uow.Currencies.FirstOrDefaultAsync(
                c => c.Id == (short)request.CurrencyId, ct);
            if (currency == null)
                return Result<BankDto>.Failure("العملة المحددة غير موجودة", ErrorCodes.NotFound);

            // Create the bank domain entity (AccountId can be null — the service
            // auto-creates a sub-account under parent "1120 — البنوك")
            var bank = Bank.Create(
                request.AccountId,
                request.Name,
                (short)request.CurrencyId,
                createdByUserId: userId);

            // Auto-create Chart of Accounts sub-account under "1120 — البنوك" if no AccountId provided
            if (!request.AccountId.HasValue || request.AccountId.Value <= 0)
            {
                var accountResult = await AutoCreateBankAccountAsync(request.Name, userId, ct);
                if (!accountResult.IsSuccess || accountResult.Value == null)
                    return Result<BankDto>.Failure(
                        accountResult.Error ?? "فشل إنشاء الحساب المحاسبي للبنك");
                bank.SetAccountId(accountResult.Value.Id);
            }
            else
            {
                // Validate the specified account exists
                var accountExists = await _uow.Accounts.AnyAsync(a => a.Id == request.AccountId.Value, ct);
                if (!accountExists)
                    return Result<BankDto>.Failure("الحساب المحاسبي المحدد غير موجود", ErrorCodes.NotFound);
            }

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
    }

    public async Task<Result<BankDto>> UpdateAsync(int id, UpdateBankRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var bank = await _uow.Banks.FirstOrDefaultAsync(b => b.Id == id, ct, "Account");
            if (bank == null)
                return Result<BankDto>.Failure("البنك غير موجود", ErrorCodes.NotFound);

            bank.Update(request.Name, (short)request.CurrencyId, updatedByUserId: userId);
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
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var bank = await _uow.Banks.GetByIdAsync(id, ct);
            if (bank == null)
                return Result.Failure("البنك غير موجود", ErrorCodes.NotFound);

            bank.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Bank deactivated: {Name} (ID: {Id})", bank.Name, id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating bank {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط البنك");
        }
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
                a => a.ParentAccountId == parent.Id,
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
                accountType: AccountType.Asset,
                level: 4,
                parentAccountId: parent.Id,
                isSystemAccount: false,
                description: $"الحساب الجاري للبنك: {bankName}",
                colorCode: "#2196F3",
                allowTransactions: true,
                openingBalance: 0,
                explanation: null,
                notes: null,
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
            bank.Name,
            bank.IsActive
        );
    }
}
