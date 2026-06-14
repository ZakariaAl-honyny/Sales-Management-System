using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface ICustomerReceiptService
{
    Task<Result<List<CustomerReceiptDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<CustomerReceiptDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<CustomerReceiptDto>> CreateAsync(CreateCustomerReceiptRequest request, int userId, CancellationToken ct);
    Task<Result<CustomerReceiptDto>> UpdateAsync(int id, UpdateCustomerReceiptRequest request, int userId, CancellationToken ct);
    Task<Result> PostAsync(int id, int userId, CancellationToken ct);
    Task<Result> CancelAsync(int id, int userId, CancellationToken ct);
    Task<Result<CustomerReceiptDto>> AddApplicationAsync(int receiptId, AddReceiptApplicationRequest request, CancellationToken ct);
}
