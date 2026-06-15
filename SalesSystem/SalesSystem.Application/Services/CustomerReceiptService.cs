using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class CustomerReceiptService : ICustomerReceiptService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly IAccountingIntegrationService _accountingService;
    private readonly ILogger<CustomerReceiptService> _logger;

    public CustomerReceiptService(
        IUnitOfWork uow,
        IDocumentSequenceService sequenceService,
        IAccountingIntegrationService accountingService,
        ILogger<CustomerReceiptService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _accountingService = accountingService;
        _logger = logger;
    }

    public async Task<Result<List<CustomerReceiptDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var receipts = await _uow.CustomerReceipts.ToListAsync(ct, "Customer", "Customer.Party", "CashBox", "Currency", "Applications");
            var dtos = receipts.Select(MapToDto).ToList();
            return Result<List<CustomerReceiptDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer receipts");
            return Result<List<CustomerReceiptDto>>.Failure("حدث خطأ أثناء استرجاع قائمة سندات القبض");
        }
    }

    public async Task<Result<CustomerReceiptDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var receipt = await _uow.CustomerReceipts.FirstOrDefaultAsync(
                r => r.Id == id, ct, "Customer", "Customer.Party", "CashBox", "Currency", "Applications");
            if (receipt == null)
                return Result<CustomerReceiptDto>.Failure("سند القبض غير موجود", ErrorCodes.NotFound);

            return Result<CustomerReceiptDto>.Success(MapToDto(receipt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer receipt {Id}", id);
            return Result<CustomerReceiptDto>.Failure("حدث خطأ أثناء استرجاع بيانات سند القبض");
        }
    }

    public async Task<Result<CustomerReceiptDto>> CreateAsync(CreateCustomerReceiptRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Generate receipt number via thread-safe DocumentSequenceService
            var seqResult = await _sequenceService.GetNextIntAsync("CustomerReceipt", ct);
            if (!seqResult.IsSuccess)
                return Result<CustomerReceiptDto>.Failure(seqResult.Error ?? "فشل في توليد رقم السند");

            var receipt = CustomerReceipt.Create(
                seqResult.Value,
                DateTime.UtcNow,
                request.CustomerId,
                request.CashBoxId,
                (short)request.CurrencyId,
                request.Amount,
                notes: request.Notes,
                createdByUserId: userId);

            await _uow.CustomerReceipts.AddAsync(receipt, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Customer receipt created (No: {ReceiptNo}, ID: {Id}) by User {UserId}",
                receipt.ReceiptNo, receipt.Id, userId);
            return Result<CustomerReceiptDto>.Success(MapToDto(receipt));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating customer receipt: {Message}", ex.Message);
            return Result<CustomerReceiptDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer receipt");
            return Result<CustomerReceiptDto>.Failure("حدث خطأ أثناء إنشاء سند القبض");
        }
    }

    public async Task<Result<CustomerReceiptDto>> UpdateAsync(int id, UpdateCustomerReceiptRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var receipt = await _uow.CustomerReceipts.FirstOrDefaultAsync(
                r => r.Id == id, ct, "Customer", "Customer.Party", "CashBox", "Currency", "Applications");
            if (receipt == null)
                return Result<CustomerReceiptDto>.Failure("سند القبض غير موجود", ErrorCodes.NotFound);

            // Only drafts can be updated — posted receipts must be cancelled and recreated
            if (receipt.Status != InvoiceStatus.Draft)
            {
                _logger.LogWarning(
                    "Attempt to update posted/cancelled customer receipt {Id} (Status: {Status}) by User {UserId}",
                    id, receipt.Status, userId);
                return Result<CustomerReceiptDto>.Failure(
                    "لا يمكن تعديل سند قبض مرحّل أو ملغي — قم بإلغاء السند وإنشاء واحد جديد",
                    ErrorCodes.InvalidOperation);
            }

            receipt.Update(
                cashBoxId: request.CashBoxId,
                currencyId: (short)request.CurrencyId,
                amount: request.Amount,
                notes: request.Notes,
                updatedByUserId: userId);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Customer receipt {Id} updated by User {UserId}", id, userId);
            return Result<CustomerReceiptDto>.Success(MapToDto(receipt));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating customer receipt {Id}: {Message}", id, ex.Message);
            return Result<CustomerReceiptDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer receipt {Id}", id);
            return Result<CustomerReceiptDto>.Failure("حدث خطأ أثناء تحديث سند القبض");
        }
    }

    public async Task<Result> PostAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var receipt = await _uow.CustomerReceipts.FirstOrDefaultAsync(
                r => r.Id == id, ct, "Applications", "Customer", "Customer.Party");
            if (receipt == null)
                return Result.Failure("سند القبض غير موجود", ErrorCodes.NotFound);

            receipt.Post();
            await _uow.SaveChangesAsync(ct);

            // Create journal entry: Dr Cash / Cr AR
            var customerName = receipt.Customer?.Party?.Name ?? "";
            var entryResult = await _accountingService.CreateCustomerPaymentEntryAsync(
                receipt, customerName, userId, ct);
            if (!entryResult.IsSuccess)
                return Result.Failure(entryResult.Error!);

            _logger.LogInformation("Customer receipt {Id} posted by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation posting customer receipt {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting customer receipt {Id}", id);
            return Result.Failure("حدث خطأ أثناء ترحيل سند القبض");
        }
    }

    public async Task<Result> CancelAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var receipt = await _uow.CustomerReceipts.FirstOrDefaultAsync(
                r => r.Id == id, ct, "Customer", "Customer.Party");
            if (receipt == null)
                return Result.Failure("سند القبض غير موجود", ErrorCodes.NotFound);

            // If already posted, reverse the journal entry first
            if (receipt.Status == InvoiceStatus.Posted)
            {
                var customerAccountId = receipt.Customer?.Party?.AccountId ?? 0;
                var customerName = receipt.Customer?.Party?.Name ?? "";
                var reverseResult = await _accountingService.ReverseCustomerPaymentEntryAsync(
                    receipt.Id, receipt.Amount, customerName, customerAccountId, userId, ct);
                if (!reverseResult.IsSuccess)
                    return Result.Failure(reverseResult.Error!);
            }

            receipt.Cancel();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Customer receipt {Id} cancelled by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation cancelling customer receipt {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling customer receipt {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء سند القبض");
        }
    }

    public async Task<Result<CustomerReceiptDto>> AddApplicationAsync(int receiptId, AddReceiptApplicationRequest request, CancellationToken ct)
    {
        try
        {
            var receipt = await _uow.CustomerReceipts.FirstOrDefaultAsync(
                r => r.Id == receiptId, ct, "Applications");
            if (receipt == null)
                return Result<CustomerReceiptDto>.Failure("سند القبض غير موجود", ErrorCodes.NotFound);

            // Validate that the sales invoice exists
            var invoiceExists = await _uow.SalesInvoices.AnyAsync(
                si => si.Id == request.SalesInvoiceId, ct);
            if (!invoiceExists)
                return Result<CustomerReceiptDto>.Failure("فاتورة المبيعات غير موجودة", ErrorCodes.NotFound);

            var application = CustomerReceiptApplication.Create(
                receiptId,
                request.SalesInvoiceId,
                request.AppliedAmount);

            receipt.AddApplication(application);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Application added to receipt {ReceiptId}: Invoice {InvoiceId}, Amount {AppliedAmount}",
                receiptId, request.SalesInvoiceId, request.AppliedAmount);

            // Reload with full includes for the response
            var updatedReceipt = await _uow.CustomerReceipts.FirstOrDefaultAsync(
                r => r.Id == receiptId, ct, "Customer", "Customer.Party", "CashBox", "Currency", "Applications");
            return Result<CustomerReceiptDto>.Success(MapToDto(updatedReceipt!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation adding application to receipt {ReceiptId}: {Message}", receiptId, ex.Message);
            return Result<CustomerReceiptDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding application to customer receipt {ReceiptId}", receiptId);
            return Result<CustomerReceiptDto>.Failure("حدث خطأ أثناء إضافة تخصيص السند");
        }
    }

    // ─── Private Helpers ─────────────────────────────────

    private static CustomerReceiptDto MapToDto(CustomerReceipt receipt)
    {
        return new CustomerReceiptDto(
            receipt.Id,
            receipt.ReceiptNo,
            receipt.ReceiptDate,
            receipt.CustomerId,
            receipt.Customer?.Party?.Name,
            receipt.CashBoxId,
            receipt.CashBox?.BoxName,
            receipt.CurrencyId,
            receipt.Currency?.Name,
            receipt.Amount,
            receipt.Notes,
            (byte)receipt.Status,
            GetStatusName(receipt.Status),
            receipt.PostedAt,
            receipt.Applications?.Select(a => new CustomerReceiptApplicationDto(
                a.Id,
                a.CustomerReceiptId,
                a.SalesInvoiceId,
                null, // InvoiceNo — not loaded
                a.AppliedAmount,
                false // Entity — no IsActive
            )).ToList(),
            false // DocumentEntity — no IsActive
        );
    }

    private static string? GetStatusName(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "مسودة",
        InvoiceStatus.Posted => "مرحّل",
        InvoiceStatus.Cancelled => "ملغي",
        _ => null
    };
}
