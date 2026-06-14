using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class PartyService : IPartyService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PartyService> _logger;
    private readonly IAccountService _accountService;

    public PartyService(IUnitOfWork uow, ILogger<PartyService> logger, IAccountService accountService)
    {
        _uow = uow;
        _logger = logger;
        _accountService = accountService;
    }

    public async Task<Result<List<PartyDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var parties = await _uow.Parties.ToListAsync(ct, "Account");
            var dtos = parties.Select(MapToDto).ToList();
            return Result<List<PartyDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all parties");
            return Result<List<PartyDto>>.Failure("حدث خطأ أثناء استرجاع قائمة الأطراف");
        }
    }

    public async Task<Result<PartyDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var party = await _uow.Parties.FirstOrDefaultAsync(p => p.Id == id, ct, "Account");
            if (party == null)
                return Result<PartyDto>.Failure("الطرف غير موجود", ErrorCodes.NotFound);

            return Result<PartyDto>.Success(MapToDto(party));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving party {Id}", id);
            return Result<PartyDto>.Failure("حدث خطأ أثناء استرجاع بيانات الطرف");
        }
    }

    public async Task<Result<PartyDto>> CreateAsync(CreatePartyRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Auto-create account if needed (delegate to AccountService)
            var partyType = (PartyType)request.PartyType;
            var accountResult = await AutoCreateAccountAsync(request.Name, partyType, userId, ct);
            if (!accountResult.IsSuccess)
                return Result<PartyDto>.Failure(accountResult.Error!, accountResult.ErrorCode);

            var party = Party.Create(
                name: request.Name,
                partyType: partyType,
                accountId: accountResult.Value,
                nameAr: request.NameAr,
                phone: request.Phone,
                mobile: request.Mobile,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                createdByUserId: userId);

            await _uow.Parties.AddAsync(party, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Party created: {Name} (ID: {Id}, Type: {PartyType}) by User {UserId}",
                party.Name, party.Id, party.PartyType, userId);

            var saved = await _uow.Parties.FirstOrDefaultAsync(p => p.Id == party.Id, ct, "Account");
            return Result<PartyDto>.Success(MapToDto(saved!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating party: {Message}", ex.Message);
            return Result<PartyDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating party");
            return Result<PartyDto>.Failure("حدث خطأ أثناء إنشاء الطرف");
        }
    }

    public async Task<Result<PartyDto>> UpdateAsync(int id, UpdatePartyRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var party = await _uow.Parties.FirstOrDefaultAsync(p => p.Id == id, ct, "Account");
            if (party == null)
                return Result<PartyDto>.Failure("الطرف غير موجود", ErrorCodes.NotFound);

            party.Update(
                name: request.Name,
                accountId: party.AccountId,
                nameAr: request.NameAr,
                phone: request.Phone,
                mobile: request.Mobile,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                updatedByUserId: userId);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Party updated: {Name} (ID: {Id}) by User {UserId}",
                party.Name, id, userId);

            var updated = await _uow.Parties.FirstOrDefaultAsync(p => p.Id == party.Id, ct, "Account");
            return Result<PartyDto>.Success(MapToDto(updated!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating party {Id}: {Message}", id, ex.Message);
            return Result<PartyDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating party {Id}", id);
            return Result<PartyDto>.Failure("حدث خطأ أثناء تحديث بيانات الطرف");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var party = await _uow.Parties.GetByIdAsync(id, ct);
            if (party == null)
                return Result.Failure("الطرف غير موجود", ErrorCodes.NotFound);

            party.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Party deactivated: {Name} (ID: {Id})", party.Name, id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating party {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط الطرف");
        }
    }

    /// <summary>
    /// Auto-creates an account under the appropriate parent for this party type.
    /// </summary>
    private async Task<Result<int>> AutoCreateAccountAsync(string partyName, PartyType partyType, int userId, CancellationToken ct)
    {
        var parentCode = partyType == PartyType.Customer ? "1210" : "2100";
        var color = partyType == PartyType.Customer ? "#2196F3" : "#F44336";

        var parentAccount = await _uow.Accounts.FirstOrDefaultAsync(
            a => a.AccountCode == parentCode && a.IsActive, ct);

        if (parentAccount == null)
            return Result<int>.Failure($"لم يتم العثور على حساب {(partyType == PartyType.Customer ? "العملاء" : "الموردين")} الرئيسي",
                ErrorCodes.NotFound);

        // Generate next account code under this parent
        var childAccounts = await _uow.Accounts.ToListAsync(
            predicate: a => a.ParentAccountId == parentAccount.Id, ct: ct);

        int maxSuffix = 0;
        foreach (var child in childAccounts)
        {
            if (int.TryParse(child.AccountCode, out var code) && code > maxSuffix)
                maxSuffix = code;
        }

        var nextCode = maxSuffix > 0
            ? (maxSuffix + 1).ToString()
            : parentCode + "1";

        var accountType = partyType == PartyType.Customer ? Domain.Accounting.Enums.AccountType.Asset : Domain.Accounting.Enums.AccountType.Liability;
        var description = partyType == PartyType.Customer
            ? $"حساب عميل: {partyName}"
            : $"حساب مورد: {partyName}";
        var explanation = partyType == PartyType.Customer
            ? $"حساب تلقائي للعميل {partyName}"
            : $"حساب تلقائي للمورد {partyName}";

        var newAccount = Domain.Accounting.Entities.Account.Create(
            accountCode: nextCode,
            nameAr: partyName,
            nameEn: partyName,
            accountType: accountType,
            level: 4,
            parentAccountId: parentAccount.Id,
            isSystemAccount: false,
            description: description,
            colorCode: color,
            allowTransactions: true,
            openingBalance: 0,
            explanation: explanation,
            createdByUserId: userId
        );

        await _uow.Accounts.AddAsync(newAccount, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Auto-created party account: {Code} - {Name} under parent {ParentCode}",
            nextCode, partyName, parentAccount.AccountCode);
        return Result<int>.Success(newAccount.Id);
    }

    private static PartyDto MapToDto(Party party)
    {
        return new PartyDto(
            party.Id,
            (byte)party.PartyType,
            party.PartyType == PartyType.Customer ? "عميل" : "مورد",
            party.Name,
            party.NameAr,
            party.Phone,
            party.Mobile,
            party.Email,
            party.Address,
            party.TaxNumber,
            party.AccountId,
            party.Account?.NameAr ?? party.Account?.NameEn,
            party.IsActive
        );
    }
}
