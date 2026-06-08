using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public interface ICashBoxApiService
{
    Task<Result<List<CashBoxDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<CashBoxDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<CashBoxDto>> CreateAsync(CreateCashBoxRequest request, CancellationToken ct = default);
    Task<Result<CashBoxDto>> UpdateAsync(int id, UpdateCashBoxRequest request, CancellationToken ct = default);
    Task<Result> DeactivateAsync(int id, CancellationToken ct = default);
    Task<Result<List<CashTransactionDto>>> GetTransactionsAsync(int cashBoxId, DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<Result<CashTransactionDto>> RecordExpenseAsync(int cashBoxId, AddCashTransactionRequest request, CancellationToken ct = default);
    Task<Result> TransferAsync(CashTransferRequest request, CancellationToken ct = default);
    Task<Result<DailyClosureDto>> PerformDailyClosureAsync(int cashBoxId, CancellationToken ct = default);
    Task<Result<List<DailyClosureDto>>> GetDailyClosuresAsync(int cashBoxId, CancellationToken ct = default);
}
