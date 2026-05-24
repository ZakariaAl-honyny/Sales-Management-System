using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Interfaces.Services;

public interface ICashBoxService
{
    Task<Result<List<CashBoxDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<CashBoxDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<CashBoxDto>> CreateAsync(CreateCashBoxRequest request, int userId, CancellationToken ct);
    Task<Result> DeactivateAsync(int id, CancellationToken ct);
    Task<Result<List<CashTransactionDto>>> GetTransactionsAsync(int cashBoxId, DateOnly? from, DateOnly? to, CancellationToken ct);
    Task<Result<CashTransactionDto>> RecordExpenseAsync(int cashBoxId, AddCashTransactionRequest request, int userId, CancellationToken ct);
    Task<Result> TransferAsync(CashTransferRequest request, int userId, CancellationToken ct);
    Task<Result<DailyClosureDto>> PerformDailyClosureAsync(int cashBoxId, int userId, CancellationToken ct);
    Task<Result<List<DailyClosureDto>>> GetDailyClosuresAsync(int cashBoxId, CancellationToken ct);
    Task<Result<CashTransactionDto>> RecordInvoicePaymentAsync(int cashBoxId, decimal amount, CashTransactionType type, string referenceType, int referenceId, int userId, CancellationToken ct);
}
