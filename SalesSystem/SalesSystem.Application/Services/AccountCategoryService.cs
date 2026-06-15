using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class AccountCategoryService : IAccountCategoryService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AccountCategoryService> _logger;

    public AccountCategoryService(IUnitOfWork uow, ILogger<AccountCategoryService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<AccountCategoryDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var categories = await _uow.AccountCategories.ToListAsync(ct);
            var dtos = categories.Select(MapToDto).ToList();
            return Result<List<AccountCategoryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all account categories");
            return Result<List<AccountCategoryDto>>.Failure("حدث خطأ أثناء استرجاع قائمة التصنيفات المحاسبية");
        }
    }

    public async Task<Result<AccountCategoryDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var category = await _uow.AccountCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category == null)
                return Result<AccountCategoryDto>.Failure("التصنيف المحاسبي غير موجود", ErrorCodes.NotFound);

            return Result<AccountCategoryDto>.Success(MapToDto(category));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account category {Id}", id);
            return Result<AccountCategoryDto>.Failure("حدث خطأ أثناء استرجاع بيانات التصنيف المحاسبي");
        }
    }

    public async Task<Result<AccountCategoryDto>> CreateAsync(CreateAccountCategoryRequest request, CancellationToken ct)
    {
        try
        {
            var category = AccountCategory.Create(request.Name, request.Description);

            await _uow.AccountCategories.AddAsync(category, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Account category created: {Name} (ID: {Id})",
                category.Name, category.Id);

            return Result<AccountCategoryDto>.Success(MapToDto(category));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating account category: {Message}", ex.Message);
            return Result<AccountCategoryDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account category");
            return Result<AccountCategoryDto>.Failure("حدث خطأ أثناء إنشاء التصنيف المحاسبي");
        }
    }

    public async Task<Result<AccountCategoryDto>> UpdateAsync(int id, UpdateAccountCategoryRequest request, CancellationToken ct)
    {
        try
        {
            var category = await _uow.AccountCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category == null)
                return Result<AccountCategoryDto>.Failure("التصنيف المحاسبي غير موجود", ErrorCodes.NotFound);

            category.Update(request.Name, request.Description);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Account category updated: {Name} (ID: {Id})", category.Name, id);

            return Result<AccountCategoryDto>.Success(MapToDto(category));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating account category {Id}: {Message}", id, ex.Message);
            return Result<AccountCategoryDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account category {Id}", id);
            return Result<AccountCategoryDto>.Failure("حدث خطأ أثناء تحديث بيانات التصنيف المحاسبي");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var category = await _uow.AccountCategories.GetByIdAsync(id, ct);
            if (category == null)
                return Result.Failure("التصنيف المحاسبي غير موجود", ErrorCodes.NotFound);

            category.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Account category deactivated: {Name} (ID: {Id})", category.Name, id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating account category {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط التصنيف المحاسبي");
        }
    }

    private static AccountCategoryDto MapToDto(AccountCategory category)
    {
        return new AccountCategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.IsActive
        );
    }
}
