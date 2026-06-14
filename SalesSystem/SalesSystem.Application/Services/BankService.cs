using Microsoft.Extensions.Logging;
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
            // Validate account exists
            var accountExists = await _uow.Accounts.AnyAsync(a => a.Id == request.AccountId, ct);
            if (!accountExists)
                return Result<BankDto>.Failure("الحساب المحاسبي المحدد غير موجود", ErrorCodes.NotFound);

            var bank = Bank.Create(request.AccountId, request.Name, (short)request.CurrencyId, createdByUserId: userId);

            await _uow.Banks.AddAsync(bank, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Bank created: {Name} (ID: {Id}, AccountId: {AccountId}) by User {UserId}",
                bank.Name, bank.Id, request.AccountId, userId);

            return Result<BankDto>.Success(MapToDto(bank));
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
