using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface ISupplierPaymentApplicationService
{
    Task<Result<List<SupplierPaymentApplicationDto>>> GetAllAsync(int? supplierPaymentId, int? purchaseInvoiceId, CancellationToken ct);
    Task<Result<SupplierPaymentApplicationDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<SupplierPaymentApplicationDto>> CreateAsync(CreateSupplierPaymentApplicationRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
