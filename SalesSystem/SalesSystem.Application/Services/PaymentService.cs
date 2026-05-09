using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IUnitOfWork uow, IDocumentSequenceService sequenceService, ILogger<PaymentService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<CustomerPaymentDto>> CreateCustomerPaymentAsync(CreateCustomerPaymentRequest request, int userId, CancellationToken ct)
    {
        if (request.Amount <= 0) return Result<CustomerPaymentDto>.Failure("مبلغ الدفع يجب أن يكون أكبر من صفر");

        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, ct);
        if (customer == null) return Result<CustomerPaymentDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            var paymentNoResult = await _sequenceService.GetNextNumberAsync("CP", ct);
            if (!paymentNoResult.IsSuccess) return Result<CustomerPaymentDto>.Failure(paymentNoResult.Error!);

            var payment = CustomerPayment.Create(
                paymentNoResult.Value!,
                request.CustomerId,
                request.Amount,
                (byte)request.PaymentMethod,
                request.SalesInvoiceId,
                null, // ReferenceNo
                request.Notes,
                userId,
                request.PaymentDate
            );

            await _uow.CustomerPayments.AddAsync(payment, ct);
            customer.DecreaseBalance(request.Amount); // Reduce balance

            await _uow.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Customer Payment recorded: {PaymentNo} for Customer {CustomerId}, Amount {Amount}", payment.PaymentNo, customer.Id, request.Amount);

            return Result<CustomerPaymentDto>.Success(MapToDto(payment));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error recording customer payment");
            return Result<CustomerPaymentDto>.Failure("حدث خطأ أثناء حفظ عملية الدفع");
        }
    }

    public async Task<Result<SupplierPaymentDto>> CreateSupplierPaymentAsync(CreateSupplierPaymentRequest request, int userId, CancellationToken ct)
    {
        if (request.Amount <= 0) return Result<SupplierPaymentDto>.Failure("مبلغ الدفع يجب أن يكون أكبر من صفر");

        var supplier = await _uow.Suppliers.GetByIdAsync(request.SupplierId, ct);
        if (supplier == null) return Result<SupplierPaymentDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            var paymentNoResult = await _sequenceService.GetNextNumberAsync("SP", ct);
            if (!paymentNoResult.IsSuccess) return Result<SupplierPaymentDto>.Failure(paymentNoResult.Error!);

            var payment = SupplierPayment.Create(
                paymentNoResult.Value!,
                request.SupplierId,
                request.Amount,
                (byte)request.PaymentMethod,
                request.PurchaseInvoiceId,
                null, // ReferenceNo
                request.Notes,
                userId,
                request.PaymentDate
            );

            await _uow.SupplierPayments.AddAsync(payment, ct);
            supplier.DecreaseBalance(request.Amount); // Reduce balance (what we owe them)

            await _uow.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Supplier Payment recorded: {PaymentNo} for Supplier {SupplierId}, Amount {Amount}", payment.PaymentNo, supplier.Id, request.Amount);

            return Result<SupplierPaymentDto>.Success(MapToDto(payment));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error recording supplier payment");
            return Result<SupplierPaymentDto>.Failure("حدث خطأ أثناء حفظ عملية الدفع");
        }
    }

    public async Task<Result<PagedResult<CustomerPaymentDto>>> GetCustomerPaymentsAsync(int? customerId, int page, int pageSize, CancellationToken ct)
    {
        var query = _uow.CustomerPayments.Query()
            .Include(p => p.Customer)
            .AsQueryable();

        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<CustomerPaymentDto>>.Success(PagedResult<CustomerPaymentDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<PagedResult<SupplierPaymentDto>>> GetSupplierPaymentsAsync(int? supplierId, int page, int pageSize, CancellationToken ct)
    {
        var query = _uow.SupplierPayments.Query()
            .Include(p => p.Supplier)
            .AsQueryable();

        if (supplierId.HasValue) query = query.Where(p => p.SupplierId == supplierId.Value);

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<SupplierPaymentDto>>.Success(PagedResult<SupplierPaymentDto>.Create(dtos, totalItems, page, pageSize));
    }

    private static CustomerPaymentDto MapToDto(CustomerPayment p)
    {
        return new CustomerPaymentDto(
            p.Id,
            p.PaymentNo,
            p.CustomerId,
            p.Customer?.Name ?? "Unknown",
            p.Amount,
            p.PaymentMethod,
            p.PaymentDate,
            p.SalesInvoiceId,
            p.Notes
        );
    }

    private static SupplierPaymentDto MapToDto(SupplierPayment p)
    {
        return new SupplierPaymentDto(
            p.Id,
            p.PaymentNo,
            p.SupplierId,
            p.Supplier?.Name ?? "Unknown",
            p.Amount,
            p.PaymentMethod,
            p.PaymentDate,
            p.PurchaseInvoiceId,
            p.Notes
        );
    }
}
