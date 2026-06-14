using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Attachment API service implementation
/// </summary>
public class AttachmentApiService : ApiServiceBase, IAttachmentApiService
{
    public AttachmentApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<AttachmentDto>>> GetAllAsync(string? referenceType = null, int? referenceId = null)
    {
        var queryParams = "pageSize=1000";
        if (!string.IsNullOrEmpty(referenceType))
            queryParams += $"&referenceType={referenceType}";
        if (referenceId.HasValue)
            queryParams += $"&referenceId={referenceId.Value}";

        return await ExecutePagedAsync<AttachmentDto>(
            () => _httpClient.GetAsync($"api/v1/attachments?{queryParams}"),
            "AttachmentApiService.GetAllAsync");
    }

    public async Task<Result<AttachmentDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<AttachmentDto>(
            () => _httpClient.GetAsync($"api/v1/attachments/{id}"),
            "AttachmentApiService.GetByIdAsync");
    }

    public async Task<Result<AttachmentDto>> CreateAsync(CreateAttachmentRequest request)
    {
        return await ExecuteAsync<AttachmentDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/attachments", request),
            "AttachmentApiService.CreateAsync");
    }

    public async Task<Result<AttachmentDto>> UpdateAsync(int id, UpdateAttachmentRequest request)
    {
        return await ExecuteAsync<AttachmentDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/attachments/{id}", request),
            "AttachmentApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/attachments/{id}"),
            "AttachmentApiService.DeleteAsync");
    }
}
