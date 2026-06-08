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

namespace SalesSystem.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CustomerService> _logger;
    private readonly IAccountingIntegrationService _accountingService;

    public CustomerService(IUnitOfWork uow, ILogger<CustomerService> logger, IAccountingIntegrationService accountingService)
    {
        _uow = uow;
        _logger = logger;
        _accountingService = accountingService;
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var customer = await _uow.Customers.FirstOrDefaultAsync(
            c => c.Id == id, ct, "Account", "CustomerGroup");
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
            predicate = c => c.Name.Contains(s) || (c.Phone != null && c.Phone.Contains(s));
        }

        var (items, total) = await _uow.Customers.GetPagedAsync(
            predicate, q => q.OrderByDescending(c => c.Id), page, pageSize, ct, includeInactive, "Account", "CustomerGroup");

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<CustomerDto>>.Success(PagedResult<CustomerDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Step 1: Auto-create account under AR parent if AccountId not provided
            int? accountId = request.AccountId;
            if (accountId == null)
            {
                var accountResult = await AutoCreateCustomerAccountAsync(request.Name, userId, ct);
                if (!accountResult.IsSuccess)
                    return Result<CustomerDto>.Failure(accountResult.Error!, accountResult.ErrorCode);
                accountId = accountResult.Value;
            }

            // Step 2: Create customer with the account ID
            var customer = Customer.Create(
                name: request.Name,
                openingBalance: request.OpeningBalance,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                creditLimit: request.CreditLimit,
                createdByUserId: userId,
                accountId: accountId,
                customerGroupId: request.CustomerGroupId
            );

            if (request.OpeningBalance > 0)
            {
                // Use transaction for atomicity — customer + journal entry
                await _uow.ExecuteTransactionAsync(async () =>
                {
                    await _uow.Customers.AddAsync(customer, ct);
                    await _uow.SaveChangesAsync(ct);

                    var entryResult = await _accountingService.CreateCustomerOpeningEntryAsync(
                        customer.Id,
                        customer.Name,
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
                await _uow.Customers.AddAsync(customer, ct);
                await _uow.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Customer created: {CustomerName} (ID: {CustomerId})", customer.Name, customer.Id);

            return Result<CustomerDto>.Success(MapToDto(customer));
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
                c => c.Id == id, ct, "Account", "CustomerGroup");
            if (customer == null)
                return Result<CustomerDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

            customer.Update(
                name: request.Name,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                creditLimit: request.CreditLimit,
                updatedByUserId: userId,
                accountId: request.AccountId,
                customerGroupId: request.CustomerGroupId
            );

            if (request.IsActive != customer.IsActive)
            {
                if (request.IsActive) customer.Restore();
                else customer.MarkAsDeleted();
            }

            await _uow.Customers.UpdateAsync(customer, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Customer updated: {CustomerName} (ID: {CustomerId})", customer.Name, customer.Id);

            return Result<CustomerDto>.Success(MapToDto(customer));
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

        if (await _uow.CustomerPayments.AnyAsync(cp => cp.CustomerId == id, ct))
            return Result.Failure("لا يمكن حذف العميل نهائياً لأنه مرتبط بسندات قبض");

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

    public async Task<Result<List<CustomerGroupDto>>> GetAllGroupsAsync(CancellationToken ct = default)
    {
        try
        {
            var groups = await _uow.CustomerGroups.ToListAsync(ct);
            var dtos = groups.Select(g => new CustomerGroupDto(g.Id, g.Name, g.Description, g.IsActive)).ToList();
            return Result<List<CustomerGroupDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching customer groups");
            return Result<List<CustomerGroupDto>>.Failure("حدث خطأ أثناء تحميل مجموعات العملاء.");
        }
    }

    public async Task<Result<List<CustomerDto>>> GetByGroupAsync(int groupId, CancellationToken ct = default)
    {
        try
        {
            var customers = await _uow.Customers.ToListAsync(
                c => c.CustomerGroupId == groupId,
                q => q.OrderByDescending(c => c.Id),
                ct,
                includePaths: new[] { "Account", "CustomerGroup" });
            var dtos = customers.Select(MapToDto).ToList();
            return Result<List<CustomerDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers by group {GroupId}", groupId);
            return Result<List<CustomerDto>>.Failure("حدث خطأ أثناء تحميل العملاء حسب المجموعة");
        }
    }

    public async Task<Result<PagedResult<CustomerBalanceReportDto>>> GetCustomerBalanceReportAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        try
        {
            Expression<Func<Customer, bool>>? predicate = null;
            if (!string.IsNullOrWhiteSpace(search))
                predicate = c => c.Name.Contains(search) && c.CurrentBalance != 0;

            var (items, total) = await _uow.Customers.GetPagedAsync(
                predicate,
                q => q.OrderByDescending(c => c.CurrentBalance),
                page, pageSize, ct,
                includePaths: new[] { "CustomerGroup" });

            var dtos = items.Select(c => new CustomerBalanceReportDto(
                c.Id, c.Name, c.Phone, c.CustomerGroup?.Name,
                c.CurrentBalance, c.CreditLimit,
                c.CurrentBalance > 0 ? "مدين" : (c.CurrentBalance < 0 ? "دائن" : "متوازن")
            )).ToList();

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
            // Customers with positive balance grouped by aging buckets
            var (items, total) = await _uow.Customers.GetPagedAsync(
                c => c.CurrentBalance > 0,
                q => q.OrderByDescending(c => c.CurrentBalance),
                page, pageSize, ct);

            var dtos = items.Select(c =>
            {
                var agingBucket = c.CurrentBalance > 10000 ? "أكثر من 30 يوم" : "0-30 يوم";
                return new CustomerAgingReportDto(
                    c.Id, c.Name, c.Phone, c.CurrentBalance,
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
    /// Auto-creates a Level 4 Asset account under the AR parent account for this customer.
    /// </summary>
    private async Task<Result<int>> AutoCreateCustomerAccountAsync(string customerName, int userId, CancellationToken ct)
    {
        try
        {
            // Get SystemAccountMappings to find AR parent account
            var mappings = await _uow.SystemAccountMappings.FirstOrDefaultAsync(_ => true, ct);
            if (mappings == null)
                return Result<int>.Failure("لم يتم تهيئة دليل الحسابات بعد", ErrorCodes.NotFound);

            // Get the AR account (Level 4 detail account, e.g. 1131)
            var arAccount = await _uow.Accounts.GetByIdAsync(mappings.AccountsReceivableAccountId, ct);
            if (arAccount == null || arAccount.ParentAccountId == null)
                return Result<int>.Failure("لم يتم العثور على حساب العملاء", ErrorCodes.NotFound);

            // Get the parent account (Level 3, e.g. 1130 - العملاء)
            var arParentAccount = await _uow.Accounts.GetByIdAsync(arAccount.ParentAccountId.Value, ct);
            if (arParentAccount == null)
                return Result<int>.Failure("لم يتم العثور على حساب العملاء الرئيسي", ErrorCodes.NotFound);

            // Generate next account code under this parent
            var nextCode = await GenerateNextAccountCodeAsync(arParentAccount.Id, arParentAccount.AccountCode, ct);

            // Create the new account
            var newAccount = Account.Create(
                accountCode: nextCode,
                nameAr: customerName,
                nameEn: customerName,
                accountType: AccountType.Asset,
                level: 4,
                parentAccountId: arParentAccount.Id,
                isSystemAccount: false,
                description: $"حساب عميل: {customerName}",
                colorCode: "#2196F3",
                allowTransactions: true,
                openingBalance: 0,
                explanation: $"حساب تلقائي للعميل {customerName}",
                createdByUserId: userId
            );

            await _uow.Accounts.AddAsync(newAccount, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Auto-created customer account: {Code} - {Name} under parent {ParentCode}", nextCode, customerName, arParentAccount.AccountCode);
            return Result<int>.Success(newAccount.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-create customer account for {CustomerName}", customerName);
            return Result<int>.Failure("فشل إنشاء الحساب المحاسبي للعميل");
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

    private static CustomerDto MapToDto(Customer c)
    {
        return new CustomerDto(
            c.Id,
            c.Name,
            c.Phone,
            c.Email,
            c.Address,
            c.TaxNumber,
            c.OpeningBalance,
            c.CurrentBalance,
            c.CreditLimit,
            c.IsActive,
            AccountId: c.AccountId,
            AccountName: c.Account?.NameAr,
            CustomerGroupId: c.CustomerGroupId,
            CustomerGroupName: c.CustomerGroup?.Name
        );
    }
}
