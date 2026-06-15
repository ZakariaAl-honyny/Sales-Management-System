using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class PartyService : IPartyService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PartyService> _logger;

    public PartyService(IUnitOfWork uow, ILogger<PartyService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<PartyDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var parties = await _uow.Parties.ToListAsync(ct);
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
            var party = await _uow.Parties.FirstOrDefaultAsync(p => p.Id == id, ct);
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
            var party = Party.Create(
                name: request.Name,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                notes: request.Notes,
                createdByUserId: userId);

            await _uow.Parties.AddAsync(party, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Party created: {Name} (ID: {Id}) by User {UserId}",
                party.Name, party.Id, userId);

            var saved = await _uow.Parties.FirstOrDefaultAsync(p => p.Id == party.Id, ct);
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
            var party = await _uow.Parties.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (party == null)
                return Result<PartyDto>.Failure("الطرف غير موجود", ErrorCodes.NotFound);

            party.Update(
                name: request.Name,
                phone: request.Phone,
                email: request.Email,
                address: request.Address,
                taxNumber: request.TaxNumber,
                notes: request.Notes,
                updatedByUserId: userId);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Party updated: {Name} (ID: {Id}) by User {UserId}",
                party.Name, id, userId);

            var updated = await _uow.Parties.FirstOrDefaultAsync(p => p.Id == party.Id, ct);
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

    private static PartyDto MapToDto(Party party)
    {
        return new PartyDto(
            party.Id,
            party.Name,
            party.Phone,
            party.Email,
            party.Address,
            party.TaxNumber,
            party.Notes,
            party.IsActive
        );
    }
}
