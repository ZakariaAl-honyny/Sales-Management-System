using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class CustomerContactService : ICustomerContactService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CustomerContactService> _logger;

    public CustomerContactService(IUnitOfWork uow, ILogger<CustomerContactService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<CustomerContactDto>>> GetAllAsync(int customerId, CancellationToken ct)
    {
        try
        {
            var contacts = await _uow.CustomerContacts
                .ToListAsync(c => c.CustomerId == customerId, null, ct, false, "Customer");
            var dtos = contacts.Select(MapToDto).ToList();
            return Result<List<CustomerContactDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer contacts for CustomerId={CustomerId}", customerId);
            return Result<List<CustomerContactDto>>.Failure("حدث خطأ أثناء استرجاع قائمة جهات الاتصال");
        }
    }

    public async Task<Result<CustomerContactDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var contact = await _uow.CustomerContacts
                .FirstOrDefaultAsync(c => c.Id == id, ct, "Customer");
            if (contact == null)
                return Result<CustomerContactDto>.Failure("جهة الاتصال غير موجودة", ErrorCodes.NotFound);

            return Result<CustomerContactDto>.Success(MapToDto(contact));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer contact {Id}", id);
            return Result<CustomerContactDto>.Failure("حدث خطأ أثناء استرجاع بيانات جهة الاتصال");
        }
    }

    public async Task<Result<CustomerContactDto>> CreateAsync(CreateCustomerContactRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var customerExists = await _uow.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
            if (!customerExists)
                return Result<CustomerContactDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

            var contact = CustomerContact.Create(
                request.CustomerId,
                request.Name,
                request.Phone,
                request.Email,
                request.Position,
                request.Notes,
                createdByUserId: userId);

            await _uow.CustomerContacts.AddAsync(contact, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Customer contact created: {Name} (ID: {Id}, CustomerId: {CustomerId}) by User {UserId}",
                contact.Name, contact.Id, request.CustomerId, userId);

            return Result<CustomerContactDto>.Success(MapToDto(contact));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating customer contact: {Message}", ex.Message);
            return Result<CustomerContactDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer contact");
            return Result<CustomerContactDto>.Failure("حدث خطأ أثناء إنشاء جهة الاتصال");
        }
    }

    public async Task<Result<CustomerContactDto>> UpdateAsync(int id, UpdateCustomerContactRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var contact = await _uow.CustomerContacts.FirstOrDefaultAsync(c => c.Id == id, ct, "Customer");
            if (contact == null)
                return Result<CustomerContactDto>.Failure("جهة الاتصال غير موجودة", ErrorCodes.NotFound);

            contact.Update(
                request.Name,
                request.Phone,
                request.Email,
                request.Position,
                request.Notes,
                updatedByUserId: userId);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Customer contact updated: {Name} (ID: {Id}) by User {UserId}",
                contact.Name, id, userId);

            return Result<CustomerContactDto>.Success(MapToDto(contact));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating customer contact {Id}: {Message}", id, ex.Message);
            return Result<CustomerContactDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer contact {Id}", id);
            return Result<CustomerContactDto>.Failure("حدث خطأ أثناء تحديث بيانات جهة الاتصال");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var contact = await _uow.CustomerContacts.GetByIdAsync(id, ct);
            if (contact == null)
                return Result.Failure("جهة الاتصال غير موجودة", ErrorCodes.NotFound);

            contact.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Customer contact deactivated: {Name} (ID: {Id})", contact.Name, id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating customer contact {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط جهة الاتصال");
        }
    }

    private static CustomerContactDto MapToDto(CustomerContact contact)
    {
        return new CustomerContactDto(
            contact.Id,
            contact.CustomerId,
            contact.Customer?.Name,
            contact.Name,
            contact.Phone,
            contact.Email,
            contact.Position,
            contact.Notes,
            contact.IsActive
        );
    }
}
