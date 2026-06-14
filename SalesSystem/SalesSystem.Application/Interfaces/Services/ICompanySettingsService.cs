using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing company-wide settings (name, contact, logo, default currency).
/// CompanySettings is a singleton row (Id = 1) enforced at the database level.
/// </summary>
public interface ICompanySettingsService
{
    /// <summary>
    /// Gets the current company settings.
    /// </summary>
    Task<Result<CompanySettingsDto>> GetAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the company settings.
    /// </summary>
    Task<Result<CompanySettingsDto>> UpdateAsync(UpdateCompanySettingsRequest request, int? userId = null, CancellationToken ct = default);
}
