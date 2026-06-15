using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface IAttachmentService
{
    Task<Result<List<AttachmentDto>>> GetAllAsync(string referenceType, int? referenceId, CancellationToken ct);
    Task<Result<AttachmentDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<AttachmentDto>> CreateAsync(CreateAttachmentRequest request, CancellationToken ct);
    Task<Result<AttachmentDto>> UpdateAsync(int id, UpdateAttachmentRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
