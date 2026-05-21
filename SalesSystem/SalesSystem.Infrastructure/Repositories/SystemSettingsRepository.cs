using Microsoft.EntityFrameworkCore;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Domain.Enums;
using SalesSystem.Infrastructure.Data;

namespace SalesSystem.Infrastructure.Repositories;

public class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly SalesDbContext _context;

    public SystemSettingsRepository(SalesDbContext context)
    {
        _context = context;
    }

    public async Task<CostingMethod> GetCostingMethodAsync(CancellationToken ct = default)
    {
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == "CostingMethod", ct);

        if (setting == null) 
            return CostingMethod.WeightedAverage;

        return Enum.TryParse<CostingMethod>(setting.SettingValue, out var method)
            ? method
            : CostingMethod.WeightedAverage;
    }

    public async Task SetCostingMethodAsync(CostingMethod method, CancellationToken ct = default)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == "CostingMethod", ct);

        if (setting != null)
            setting.UpdateValue(method.ToString());

        await _context.SaveChangesAsync(ct);
    }
}