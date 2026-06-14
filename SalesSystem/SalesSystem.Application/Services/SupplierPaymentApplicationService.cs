using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class SupplierPaymentApplicationService : ISupplierPaymentApplicationService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SupplierPaymentApplicationService> _logger;

    public SupplierPaymentApplicationService(IUnitOfWork uow, ILogger<SupplierPaymentApplicationService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<SupplierPaymentApplicationDto>>> GetAllAsync(int? supplierPaymentId, int? purchaseInvoiceId, CancellationToken ct)
    {
        try
        {
            List<SupplierPaymentApplication> applications;
            if (supplierPaymentId.HasValue)
            {
                applications = await _uow.SupplierPaymentApplications
                    .ToListAsync(a => a.SupplierPaymentId == supplierPaymentId.Value, ct: ct);
            }
            else if (purchaseInvoiceId.HasValue)
            {
                applications = await _uow.SupplierPaymentApplications
                    .ToListAsync(a => a.PurchaseInvoiceId == purchaseInvoiceId.Value, ct: ct);
            }
            else
            {
                applications = await _uow.SupplierPaymentApplications.ToListAsync(ct);
            }

            var dtos = applications.Select(MapToDto).ToList();
            return Result<List<SupplierPaymentApplicationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier payment applications");
            return Result<List<SupplierPaymentApplicationDto>>.Failure("حدث خطأ أثناء استرجاع تخصيصات المدفوعات");
        }
    }

    public async Task<Result<SupplierPaymentApplicationDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var application = await _uow.SupplierPaymentApplications.GetByIdAsync(id, ct);
            if (application == null)
                return Result<SupplierPaymentApplicationDto>.Failure("تخصيص الدفعة غير موجود", ErrorCodes.NotFound);

            return Result<SupplierPaymentApplicationDto>.Success(MapToDto(application));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier payment application {Id}", id);
            return Result<SupplierPaymentApplicationDto>.Failure("حدث خطأ أثناء استرجاع بيانات تخصيص الدفعة");
        }
    }

    public async Task<Result<SupplierPaymentApplicationDto>> CreateAsync(CreateSupplierPaymentApplicationRequest request, CancellationToken ct)
    {
        try
        {
            var application = SupplierPaymentApplication.Create(
                request.SupplierPaymentId,
                request.PurchaseInvoiceId,
                request.AppliedAmount);

            await _uow.SupplierPaymentApplications.AddAsync(application, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Supplier payment application created: Payment {PaymentId} → Invoice {InvoiceId}, Amount: {Amount}",
                request.SupplierPaymentId, request.PurchaseInvoiceId, request.AppliedAmount);

            return Result<SupplierPaymentApplicationDto>.Success(MapToDto(application));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating supplier payment application: {Message}", ex.Message);
            return Result<SupplierPaymentApplicationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier payment application");
            return Result<SupplierPaymentApplicationDto>.Failure("حدث خطأ أثناء إنشاء تخصيص الدفعة");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        try
        {
            var application = await _uow.SupplierPaymentApplications.GetByIdAsync(id, ct);
            if (application == null)
                return Result.Failure("تخصيص الدفعة غير موجود", ErrorCodes.NotFound);

            _uow.SupplierPaymentApplications.DeleteRange(new[] { application });
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Supplier payment application deleted (ID: {Id})", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier payment application {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف تخصيص الدفعة");
        }
    }

    private static SupplierPaymentApplicationDto MapToDto(SupplierPaymentApplication application)
    {
        return new SupplierPaymentApplicationDto(
            application.Id,
            application.SupplierPaymentId,
            application.PurchaseInvoiceId,
            null, // InvoiceNo — would need to load from PurchaseInvoice
            application.AppliedAmount,
            false // Entity — no IsActive
        );
    }
}
