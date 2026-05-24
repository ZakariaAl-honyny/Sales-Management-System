using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IUnitOfWork uow, IDocumentSequenceService sequenceService, ILogger<CustomerService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct);
        if (customer == null)
            return Result<CustomerDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<PagedResult<CustomerDto>>> GetAllAsync(string? search, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        System.Linq.Expressions.Expression<System.Func<Customer, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search;
            predicate = c => c.Name.Contains(s) || (c.Phone != null && c.Phone.Contains(s));
        }

        var (items, total) = await _uow.Customers.GetPagedAsync(
            predicate, q => q.OrderBy(c => c.Name), page, pageSize, ct, includeInactive);

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<CustomerDto>>.Success(PagedResult<CustomerDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct)
    {
        try
        {
            var customer = Customer.Create(
                name: request.Name,
                openingBalance: request.OpeningBalance,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                creditLimit: request.CreditLimit,
                createdByUserId: null
            );

            await _uow.Customers.AddAsync(customer, ct);
            await _uow.SaveChangesAsync(ct);

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

    public async Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct)
    {
        try
        {
            var customer = await _uow.Customers.FirstOrDefaultIgnoreFiltersAsync(c => c.Id == id, ct);
            if (customer == null)
                return Result<CustomerDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

            customer.Update(
                name: request.Name,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                creditLimit: request.CreditLimit,
                updatedByUserId: null
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

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct);
        if (customer == null)
            return Result.Failure("العميل غير موجود", ErrorCodes.NotFound);

        await _uow.Customers.SoftDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Customer soft-deleted: {CustomerId}", id);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
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
            await _uow.Customers.HardDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Customer permanently deleted: {CustomerId}", id);
            return Result.Success();
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("DbUpdate") || ex.GetType().Name.Contains("Sql"))
        {
            _logger.LogError(ex, "Failed to permanently delete customer {CustomerId} due to database constraint", id);
            return Result.Failure("لا يمكن حذف العميل نهائياً. قد يكون مرتبطاً ببيانات أخرى في النظام.");
        }
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
            c.IsActive
        );
    }
}
