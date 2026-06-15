using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AttachmentService> _logger;

    public AttachmentService(IUnitOfWork uow, ILogger<AttachmentService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<AttachmentDto>>> GetAllAsync(string referenceType, int? referenceId, CancellationToken ct)
    {
        try
        {
            List<Attachment> attachments;
            if (referenceId.HasValue)
            {
                attachments = await _uow.Attachments
                    .ToListAsync(a => a.ReferenceType == referenceType && a.ReferenceId == referenceId.Value, ct: ct);
            }
            else
            {
                attachments = await _uow.Attachments
                    .ToListAsync(a => a.ReferenceType == referenceType, ct: ct);
            }
            var dtos = attachments.Select(MapToDto).ToList();
            return Result<List<AttachmentDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attachments for {ReferenceType}/{ReferenceId}", referenceType, referenceId);
            return Result<List<AttachmentDto>>.Failure("حدث خطأ أثناء استرجاع المرفقات");
        }
    }

    public async Task<Result<AttachmentDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var attachment = await _uow.Attachments.GetByIdAsync(id, ct);
            if (attachment == null)
                return Result<AttachmentDto>.Failure("المرفق غير موجود", ErrorCodes.NotFound);

            return Result<AttachmentDto>.Success(MapToDto(attachment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attachment {Id}", id);
            return Result<AttachmentDto>.Failure("حدث خطأ أثناء استرجاع بيانات المرفق");
        }
    }

    public async Task<Result<AttachmentDto>> CreateAsync(CreateAttachmentRequest request, CancellationToken ct)
    {
        try
        {
            var attachment = Attachment.Create(
                request.ReferenceId,
                request.FileName,
                request.FilePath,
                request.FileSize,
                referenceType: request.ReferenceType,
                contentType: request.ContentType);

            await _uow.Attachments.AddAsync(attachment, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Attachment created: {FileName} for {ReferenceType}/{ReferenceId}",
                attachment.FileName, attachment.ReferenceType, attachment.ReferenceId);

            return Result<AttachmentDto>.Success(MapToDto(attachment));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating attachment: {Message}", ex.Message);
            return Result<AttachmentDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attachment");
            return Result<AttachmentDto>.Failure("حدث خطأ أثناء إنشاء المرفق");
        }
    }

    public async Task<Result<AttachmentDto>> UpdateAsync(int id, UpdateAttachmentRequest request, CancellationToken ct)
    {
        try
        {
            var attachment = await _uow.Attachments.GetByIdAsync(id, ct);
            if (attachment == null)
                return Result<AttachmentDto>.Failure("المرفق غير موجود", ErrorCodes.NotFound);

            attachment.UpdateFile(request.FileName, request.FilePath, request.FileSize, request.ContentType);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Attachment updated: {FileName} (ID: {Id})", attachment.FileName, id);

            return Result<AttachmentDto>.Success(MapToDto(attachment));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating attachment {Id}: {Message}", id, ex.Message);
            return Result<AttachmentDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attachment {Id}", id);
            return Result<AttachmentDto>.Failure("حدث خطأ أثناء تحديث بيانات المرفق");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        try
        {
            var attachment = await _uow.Attachments.GetByIdAsync(id, ct);
            if (attachment == null)
                return Result.Failure("المرفق غير موجود", ErrorCodes.NotFound);

            await _uow.Attachments.HardDeleteAsync(id, ct);

            _logger.LogInformation("Attachment permanently deleted: (ID: {Id})", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف المرفق");
        }
    }

    private static AttachmentDto MapToDto(Attachment attachment)
    {
        return new AttachmentDto(
            attachment.Id,
            attachment.ReferenceType,
            attachment.ReferenceId,
            attachment.FileName,
            attachment.FilePath,
            attachment.FileSize,
            attachment.ContentType,
            attachment.CreatedAt
        );
    }
}
