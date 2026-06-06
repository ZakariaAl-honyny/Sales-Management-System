using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface ICurrencyService
{
    Task<Result<List<CurrencyDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<CurrencyDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<CurrencyDto>> CreateAsync(CreateCurrencyRequest request, int userId, CancellationToken ct = default);
    Task<Result<CurrencyDto>> UpdateAsync(int id, UpdateCurrencyRequest request, int userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, int userId, CancellationToken ct = default);
    Task<Result> DeletePermanentlyAsync(int id, int userId, CancellationToken ct = default);
    Task<Result> UpdateExchangeRateAsync(int id, decimal newRate, int userId, CancellationToken ct = default);
    Task<Result<List<ExchangeRateHistoryDto>>> GetRateHistoryAsync(int currencyId, CancellationToken ct = default);
}
