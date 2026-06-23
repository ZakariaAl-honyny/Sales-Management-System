using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class SupplierContactService : ISupplierContactService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SupplierContactService> _logger;

    public SupplierContactService(IUnitOfWork uow, ILogger<SupplierContactService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<SupplierContactDto>>> GetAllAsync(int supplierId, CancellationToken ct)
    {
        try
        {
            var contacts = await _uow.SupplierContacts
                .ToListAsync(c => c.SupplierId == supplierId, null, ct, false, "Supplier");
            var dtos = contacts.Select(MapToDto).ToList();
            return Result<List<SupplierContactDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier contacts for SupplierId={SupplierId}", supplierId);
            return Result<List<SupplierContactDto>>.Failure("حدث خطأ أثناء استرجاع قائمة جهات الاتصال");
        }
    }

    public async Task<Result<SupplierContactDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var contact = await _uow.SupplierContacts
                .FirstOrDefaultAsync(c => c.Id == id, ct, "Supplier");
            if (contact == null)
                return Result<SupplierContactDto>.Failure("جهة الاتصال غير موجودة", ErrorCodes.NotFound);

            return Result<SupplierContactDto>.Success(MapToDto(contact));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier contact {Id}", id);
            return Result<SupplierContactDto>.Failure("حدث خطأ أثناء استرجاع بيانات جهة الاتصال");
        }
    }

    public async Task<Result<SupplierContactDto>> CreateAsync(CreateSupplierContactRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var supplierExists = await _uow.Suppliers.AnyAsync(s => s.Id == request.SupplierId, ct);
            if (!supplierExists)
                return Result<SupplierContactDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

            var contact = SupplierContact.Create(
                request.SupplierId,
                request.Name,
                request.Phone,
                request.Email,
                request.Position,
                request.Notes,
                createdByUserId: userId);

            await _uow.SupplierContacts.AddAsync(contact, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Supplier contact created: {Name} (ID: {Id}, SupplierId: {SupplierId}) by User {UserId}",
                contact.Name, contact.Id, request.SupplierId, userId);

            return Result<SupplierContactDto>.Success(MapToDto(contact));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating supplier contact: {Message}", ex.Message);
            return Result<SupplierContactDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier contact");
            return Result<SupplierContactDto>.Failure("حدث خطأ أثناء إنشاء جهة الاتصال");
        }
    }

    public async Task<Result<SupplierContactDto>> UpdateAsync(int id, UpdateSupplierContactRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var contact = await _uow.SupplierContacts.FirstOrDefaultAsync(c => c.Id == id, ct, "Supplier");
            if (contact == null)
                return Result<SupplierContactDto>.Failure("جهة الاتصال غير موجودة", ErrorCodes.NotFound);

            contact.Update(
                request.Name,
                request.Phone,
                request.Email,
                request.Position,
                request.Notes,
                updatedByUserId: userId);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Supplier contact updated: {Name} (ID: {Id}) by User {UserId}",
                contact.Name, id, userId);

            return Result<SupplierContactDto>.Success(MapToDto(contact));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating supplier contact {Id}: {Message}", id, ex.Message);
            return Result<SupplierContactDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier contact {Id}", id);
            return Result<SupplierContactDto>.Failure("حدث خطأ أثناء تحديث بيانات جهة الاتصال");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var contact = await _uow.SupplierContacts.GetByIdAsync(id, ct);
            if (contact == null)
                return Result.Failure("جهة الاتصال غير موجودة", ErrorCodes.NotFound);

            contact.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Supplier contact deactivated: {Name} (ID: {Id})", contact.Name, id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating supplier contact {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط جهة الاتصال");
        }
    }

    private static SupplierContactDto MapToDto(SupplierContact contact)
    {
        return new SupplierContactDto(
            contact.Id,
            contact.SupplierId,
            contact.Supplier?.Name,
            contact.Name,
            contact.Phone,
            contact.Email,
            contact.Position,
            contact.Notes,
            contact.IsActive
        );
    }
}
