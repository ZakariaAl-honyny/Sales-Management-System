using Microsoft.EntityFrameworkCore;
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
        var query = _uow.Customers.Query();
        
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name.Contains(search) ||
                                    (c.Code != null && c.Code.Contains(search)) ||
                                    (c.Phone != null && c.Phone.Contains(search)));
        }

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<CustomerDto>>.Success(PagedResult<CustomerDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct)
    {
        try
        {
            string? code = request.Code;
            if (string.IsNullOrWhiteSpace(code))
            {
                var codeResult = await _sequenceService.GetNextNumberAsync("CUST", ct);
                if (!codeResult.IsSuccess)
                    return Result<CustomerDto>.Failure(codeResult.Error ?? "حدث خطأ أثناء توليد الكود");
                code = codeResult.Value;
            }

            // Validate code uniqueness (Regardless of source, including inactive)
            if (await _uow.Customers.Query().IgnoreQueryFilters().AnyAsync(c => c.Code == code, ct))
            {
                _logger.LogWarning("Customer creation failed: Duplicate code {Code} (including inactive)", code);
                return Result<CustomerDto>.Failure("كود العميل مستخدم بالفعل (موجود في الأرشيف)", ErrorCodes.DuplicateCode);
            }

            var customer = Customer.Create(
                name: request.Name,
                openingBalance: request.OpeningBalance,
                code: code,
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
            var customer = await _uow.Customers.Query().IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id, ct);
            if (customer == null)
                return Result<CustomerDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

            if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != customer.Code)
            {
                if (await _uow.Customers.Query().IgnoreQueryFilters().AnyAsync(c => c.Code == request.Code && c.Id != id, ct))
                    return Result<CustomerDto>.Failure("كود العميل مستخدم بالفعل (موجود في الأرشيف)", ErrorCodes.DuplicateCode);
            }

            customer.Update(
                name: request.Name,
                code: request.Code,
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
        var customer = await _uow.Customers.Query().IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer == null)
            return Result.Failure("العميل غير موجود", ErrorCodes.NotFound);

        if (await _uow.SalesInvoices.Query().AnyAsync(si => si.CustomerId == id, ct))
            return Result.Failure("لا يمكن حذف العميل نهائياً لأنه مرتبط بفواتير بيع");

        if (await _uow.CustomerPayments.Query().AnyAsync(cp => cp.CustomerId == id, ct))
            return Result.Failure("لا يمكن حذف العميل نهائياً لأنه مرتبط بسندات قبض");

        await _uow.Customers.HardDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Customer permanently deleted: {CustomerId}", id);
        return Result.Success();
    }

    private static CustomerDto MapToDto(Customer c)
    {
        return new CustomerDto(
            c.Id,
            c.Code,
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
