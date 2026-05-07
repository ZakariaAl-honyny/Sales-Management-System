using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Customers;
using SalesSystem.Domain.Entities;

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
        var customer = await _uow.Customers.GetByIdAsync(id, ct);
        if (customer == null)
            return Result<CustomerDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<PagedResult<CustomerDto>>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = _uow.Customers.Query();

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
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            if (await _uow.Customers.Query().AnyAsync(c => c.Code == request.Code, ct))
                return Result<CustomerDto>.Failure("كود العميل مستخدم بالفعل", ErrorCodes.DuplicateCode);
        }

        var customer = Customer.Create(
            name: request.Name,
            openingBalance: request.OpeningBalance,
            code: request.Code,
            phone: request.Phone,
            email: request.Email,
            address: request.Address,
            createdByUserId: null
        );

        await _uow.Customers.AddAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Customer created: {CustomerName} (ID: {CustomerId})", customer.Name, customer.Id);

        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct);
        if (customer == null)
            return Result<CustomerDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != customer.Code)
        {
            if (await _uow.Customers.Query().AnyAsync(c => c.Code == request.Code && c.Id != id, ct))
                return Result<CustomerDto>.Failure("كود العميل مستخدم بالفعل", ErrorCodes.DuplicateCode);
        }

        customer.Update(
            request.Name,
            request.Phone,
            request.Email,
            request.Address,
            request.Code,
            null
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

    private static CustomerDto MapToDto(Customer c)
    {
        return new CustomerDto(
            c.Id,
            c.Code,
            c.Name,
            c.Phone,
            c.Email,
            c.Address,
            c.OpeningBalance,
            c.CurrentBalance,
            c.IsActive
        );
    }
}
