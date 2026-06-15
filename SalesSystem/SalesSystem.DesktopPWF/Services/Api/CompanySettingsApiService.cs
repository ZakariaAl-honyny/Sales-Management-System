using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class CompanySettingsApiService : ApiServiceBase, ICompanySettingsApiService
{
    private const string BasePath = "api/v1/company-settings";

    public CompanySettingsApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<CompanySettingsDto>> GetAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<CompanySettingsDto>(
            () => _httpClient.GetAsync(BasePath, ct),
            "CompanySettingsApiService.GetAsync");
    }

    public async Task<Result<CompanySettingsDto>> UpdateAsync(UpdateCompanySettingsRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<CompanySettingsDto>(
            () => _httpClient.PutAsJsonAsync(BasePath, request, ct),
            "CompanySettingsApiService.UpdateAsync");
    }
}
