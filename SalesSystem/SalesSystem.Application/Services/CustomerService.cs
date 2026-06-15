using System.Linq.Expressions;
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

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IUnitOfWork uow, ILogger<CustomerService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var customer = await _uow.Customers.FirstOrDefaultAsync(
            c => c.Id == id, ct, "Party");
        if (customer == null)
            return Result<CustomerDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<PagedResult<CustomerDto>>> GetAllAsync(string? search, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        Expression<Func<Customer, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search;
            predicate = c => c.Party.Name.Contains(s) || (c.Party.Phone != null && c.Party.Phone.Contains(s));
        }

        var (items, total) = await _uow.Customers.GetPagedAsync(
            predicate, q => q.OrderByDescending(c => c.Id), page, pageSize, ct, includeInactive, "Party");

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<CustomerDto>>.Success(PagedResult<CustomerDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Step 1: Auto-create account under AR parent (1130 — العملاء)
            var accountResult = await AutoCreateCustomerAccountAsync(request.Name, userId, ct);
            if (!accountResult.IsSuccess)
                return Result<CustomerDto>.Failure(accountResult.Error!, accountResult.ErrorCode);
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

            // Step 3: Create Customer record with PartyId FK (separate from Customer.Id)
            var customer = Customer.Create(
                partyId: party.Id,
                accountId: accountId,
                creditLimit: request.CreditLimit,
                createdByUserId: userId);
            await _uow.Customers.AddAsync(customer, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Customer created: {CustomerName} (ID: {CustomerId})", party.Name, customer.Id);

            // Re-fetch with navigation properties for DTO mapping
            var saved = await _uow.Customers.FirstOrDefaultAsync(
                c => c.Id == customer.Id, ct, "Party");
            return Result<CustomerDto>.Success(MapToDto(saved!));
        }
        catch (DomainException ex)
        {
            return Result<CustomerDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating customer");
            return Result<CustomerDto>.Failure("حدث خطأ أثناء إضافة العميل.");
        }
    }

    public async Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var customer = await _uow.Customers.FirstOrDefaultIgnoreFiltersAsync(
                c => c.Id == id, ct, "Party");
            if (customer == null)
                return Result<CustomerDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

            // Update Party record (contact data)
            customer.Party.Update(
                name: request.Name,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                updatedByUserId: userId);

            // Update customer-specific fields
            customer.Update(
                creditLimit: request.CreditLimit,
                updatedByUserId: userId);

            if (request.IsActive != customer.IsActive)
            {
                if (request.IsActive) customer.Restore();
                else customer.MarkAsDeleted();
            }

            await _uow.Customers.UpdateAsync(customer, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Customer updated: {CustomerName} (ID: {CustomerId})", customer.Party.Name, customer.Id);

            var updated = await _uow.Customers.FirstOrDefaultAsync(
                c => c.Id == customer.Id, ct, "Party");
            return Result<CustomerDto>.Success(MapToDto(updated!));
        }
        catch (DomainException ex)
        {
            return Result<CustomerDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating customer {Id}", id);
            return Result<CustomerDto>.Failure("حدث خطأ أثناء تحديث بيانات العميل.");
        }
    }

    public async Task<Result> DeleteAsync(int id, int userId, CancellationToken ct)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct);
        if (customer == null)
            return Result.Failure("العميل غير موجود", ErrorCodes.NotFound);

        customer.MarkAsDeleted();
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Customer soft-deleted: {CustomerId} by user {UserId}", id, userId);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, int userId, CancellationToken ct)
    {
        var customer = await _uow.Customers.FirstOrDefaultIgnoreFiltersAsync(c => c.Id == id, ct);
        if (customer == null)
            return Result.Failure("العميل غير موجود", ErrorCodes.NotFound);

        if (await _uow.SalesInvoices.AnyAsync(si => si.CustomerId == id, ct))
            return Result.Failure("لا يمكن حذف العميل نهائياً لأنه مرتبط بفواتير بيع");

        try
        {
            _uow.Customers.DeleteRange(new[] { customer });
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Customer permanently deleted: {CustomerId} by user {UserId}", id, userId);
            return Result.Success();
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("DbUpdate") || ex.GetType().Name.Contains("Sql"))
        {
            _logger.LogError(ex, "Failed to permanently delete customer {CustomerId} due to database constraint", id);
            return Result.Failure("لا يمكن حذف العميل نهائياً. قد يكون مرتبطاً ببيانات أخرى في النظام.");
        }
    }

    public async Task<Result<PagedResult<CustomerBalanceReportDto>>> GetCustomerBalanceReportAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        try
        {
            Expression<Func<Customer, bool>>? predicate = null;
            if (!string.IsNullOrWhiteSpace(search))
                predicate = c => c.Party.Name.Contains(search);

            var (items, total) = await _uow.Customers.GetPagedAsync(
                predicate,
                q => q.OrderByDescending(c => c.Id),
                page, pageSize, ct,
                includePaths: new[] { "Party" });

            var dtos = items.Select(c =>
            {
                return new CustomerBalanceReportDto(
                    c.Id, c.Party.Name, c.Party.Phone, null,
                    CreditLimit: c.CreditLimit,
                    CurrentBalance: 0,
                    BalanceStatus: c.CreditLimit > 0 ? "له حد ائتماني" : "بدون حد ائتماني"
                );
            }).ToList();

            return Result<PagedResult<CustomerBalanceReportDto>>.Success(
                PagedResult<CustomerBalanceReportDto>.Create(dtos, total, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer balance report");
            return Result<PagedResult<CustomerBalanceReportDto>>.Failure("حدث خطأ أثناء تحميل تقرير أرصدة العملاء");
        }
    }

    public async Task<Result<PagedResult<CustomerAgingReportDto>>> GetCustomerAgingReportAsync(int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var (items, total) = await _uow.Customers.GetPagedAsync(
                null,
                q => q.OrderByDescending(c => c.Id),
                page, pageSize, ct, false, "Party");

            var dtos = items.Select(c =>
            {
                var agingBucket = c.CreditLimit > 0 ? "له حد ائتماني" : "بدون حد ائتماني";
                return new CustomerAgingReportDto(
                    c.Id, c.Party.Name, c.Party.Phone, 0,
                    agingBucket, DateTime.UtcNow);
            }).ToList();

            return Result<PagedResult<CustomerAgingReportDto>>.Success(
                PagedResult<CustomerAgingReportDto>.Create(dtos, total, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer aging report");
            return Result<PagedResult<CustomerAgingReportDto>>.Failure("حدث خطأ أثناء تحميل تقرير أعمار العملاء");
        }
    }

    /// <summary>
    /// Auto-creates a Level 4 detail account under the AR parent account for this customer.
    /// Uses the parent account at code "1130 — العملاء" (Accounts Receivable).
    /// Falls back to SystemAccountMappings.AccountsReceivableAccountId if 1130 not found.
    /// </summary>
    private async Task<Result<int>> AutoCreateCustomerAccountAsync(string customerName, int userId, CancellationToken ct)
    {
        try
        {
            var arParentAccount = await _uow.Accounts.FirstOrDefaultAsync(
                a => a.AccountCode == "1130" && a.IsActive, ct);

            if (arParentAccount == null)
            {
                var arMapping = await _uow.SystemAccountMappings.FirstOrDefaultAsync(
                    m => m.MappingKey == SystemAccountKey.AccountsReceivable, ct);
                if (arMapping == null)
                    return Result<int>.Failure("لم يتم تهيئة دليل الحسابات بعد", ErrorCodes.NotFound);

                var arAccount = await _uow.Accounts.GetByIdAsync(arMapping.AccountId, ct);
                if (arAccount == null || arAccount.ParentId == null)
                    return Result<int>.Failure("لم يتم العثور على حساب العملاء", ErrorCodes.NotFound);

                arParentAccount = await _uow.Accounts.GetByIdAsync(arAccount.ParentId.Value, ct);
                if (arParentAccount == null)
                    return Result<int>.Failure("لم يتم العثور على حساب العملاء الرئيسي", ErrorCodes.NotFound);
            }

            var nextCode = await GenerateNextAccountCodeAsync(arParentAccount.Id, arParentAccount.AccountCode, ct);

            var newAccount = Account.Create(
                accountCode: nextCode,
                nameAr: customerName,
                nameEn: customerName,
                nature: (byte)AccountType.Asset,
                isLeaf: true,
                parentId: arParentAccount.Id,
                isSystem: false,
                categoryId: null,
                createdByUserId: userId
            );

            await _uow.Accounts.AddAsync(newAccount, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Auto-created customer account: {Code} - {Name} under parent {ParentCode}",
                nextCode, customerName, arParentAccount.AccountCode);
            return Result<int>.Success(newAccount.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-create customer account for {CustomerName}", customerName);
            return Result<int>.Failure("فشل إنشاء الحساب المحاسبي للعميل");
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

    private static CustomerDto MapToDto(Customer c)
    {
        return new CustomerDto(
            c.Id,
            c.Party.Name,
            c.Party.Phone,
            c.Party.Email,
            c.Party.Address,
            c.Party.TaxNumber,
            c.CreditLimit,
            c.IsActive,
            PartyId: c.PartyId,
            AccountId: c.AccountId,
            CategoryId: c.CategoryId
        );
    }
}
