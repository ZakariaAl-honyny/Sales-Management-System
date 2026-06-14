using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class DocumentSequenceApiService : ApiServiceBase, IDocumentSequenceApiService
{
    private const string BasePath = "api/v1/document-sequences";

    public DocumentSequenceApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<DocumentSequenceDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<List<DocumentSequenceDto>>(
            () => _httpClient.GetAsync(BasePath, ct),
            "DocumentSequenceApiService.GetAllAsync");
    }

    public async Task<Result<DocumentSequenceDto>> UpdateAsync(int id, UpdateDocumentSequenceRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<DocumentSequenceDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "DocumentSequenceApiService.UpdateAsync");
    }
}
