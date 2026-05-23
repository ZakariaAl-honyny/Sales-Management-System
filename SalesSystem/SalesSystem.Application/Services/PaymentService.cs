using System.Linq.Expressions;
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
        if (request.Amount <= 0)
        {
            _logger.LogWarning("Customer payment failed: Invalid amount {Amount}", request.Amount);
            return Result<CustomerPaymentDto>.Failure("مبلغ الدفع يجب أن يكون أكبر من صفر");
        }

        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, ct);
        if (customer == null)
        {
            _logger.LogWarning("Customer payment failed: Customer {CustomerId} not found", request.CustomerId);
            return Result<CustomerPaymentDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);
        }

        return await _uow.ExecuteAsync(async () =>
        {
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
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                return Result<CustomerPaymentDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error recording customer payment");
                return Result<CustomerPaymentDto>.Failure("حدث خطأ أثناء حفظ عملية الدفع");
            }
        }, ct);
    }

    public async Task<Result<SupplierPaymentDto>> CreateSupplierPaymentAsync(CreateSupplierPaymentRequest request, int userId, CancellationToken ct)
    {
        if (request.Amount <= 0)
        {
            _logger.LogWarning("Supplier payment failed: Invalid amount {Amount}", request.Amount);
            return Result<SupplierPaymentDto>.Failure("مبلغ الدفع يجب أن يكون أكبر من صفر");
        }

        var supplier = await _uow.Suppliers.GetByIdAsync(request.SupplierId, ct);
        if (supplier == null)
        {
            _logger.LogWarning("Supplier payment failed: Supplier {SupplierId} not found", request.SupplierId);
            return Result<SupplierPaymentDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);
        }

        return await _uow.ExecuteAsync(async () =>
        {
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
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                return Result<SupplierPaymentDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error recording supplier payment");
                return Result<SupplierPaymentDto>.Failure("حدث خطأ أثناء حفظ عملية الدفع");
            }
        }, ct);
    }

    public async Task<Result<PagedResult<CustomerPaymentDto>>> GetCustomerPaymentsAsync(string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        Expression<Func<CustomerPayment, bool>> predicate = p =>
            (string.IsNullOrEmpty(search) || p.PaymentNo.Contains(search) || p.Customer!.Name.Contains(search)) &&
            (!from.HasValue || p.PaymentDate >= from.Value) &&
            (!to.HasValue || p.PaymentDate <= to.Value);

        var totalItems = await _uow.CustomerPayments.CountAsync(predicate, ct);
        var items = await _uow.CustomerPayments.ToListAsync(
            predicate,
            q => q.OrderByDescending(p => p.PaymentDate).Skip((page - 1) * pageSize).Take(pageSize),
            ct,
            false,
            "Customer");

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<CustomerPaymentDto>>.Success(PagedResult<CustomerPaymentDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<CustomerPaymentDto>> GetCustomerPaymentByIdAsync(int id, CancellationToken ct)
    {
        var payment = await _uow.CustomerPayments.FirstOrDefaultAsync(p => p.Id == id, ct, "Customer");

        if (payment == null) return Result<CustomerPaymentDto>.Failure("عملية الدفع غير موجودة", ErrorCodes.NotFound);

        return Result<CustomerPaymentDto>.Success(MapToDto(payment));
    }

    public async Task<Result<CustomerPaymentDto>> UpdateCustomerPaymentAsync(int id, UpdateCustomerPaymentRequest request, CancellationToken ct)
    {
        var payment = await _uow.CustomerPayments.GetByIdAsync(id, ct);
        if (payment == null) return Result<CustomerPaymentDto>.Failure("عملية الدفع غير موجودة", ErrorCodes.NotFound);

        var customer = await _uow.Customers.GetByIdAsync(payment.CustomerId, ct);
        if (customer == null) return Result<CustomerPaymentDto>.Failure("العميل غير موجود", ErrorCodes.NotFound);

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                // Reverse old amount
                customer.IncreaseBalance(payment.Amount);

                // Apply new values
                payment.Update(
                    request.Amount,
                    (byte)request.PaymentMethod,
                    request.PaymentDate,
                    request.Notes
                );

                // Apply new amount
                customer.DecreaseBalance(request.Amount);

                await _uow.CustomerPayments.UpdateAsync(payment, ct);
                await _uow.Customers.UpdateAsync(customer, ct);

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Customer Payment updated: {PaymentNo}, New Amount {Amount}", payment.PaymentNo, payment.Amount);
                return Result<CustomerPaymentDto>.Success(MapToDto(payment));
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                return Result<CustomerPaymentDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error updating customer payment");
                return Result<CustomerPaymentDto>.Failure("حدث خطأ أثناء تحديث عملية الدفع");
            }
        }, ct);
    }

    public async Task<Result> DeleteCustomerPaymentAsync(int id, CancellationToken ct)
    {
        var payment = await _uow.CustomerPayments.GetByIdAsync(id, ct);
        if (payment == null) return Result.Failure("عملية الدفع غير موجودة", ErrorCodes.NotFound);

        var customer = await _uow.Customers.GetByIdAsync(payment.CustomerId, ct);
        if (customer == null) return Result.Failure("العميل غير موجود", ErrorCodes.NotFound);

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                // Reverse the balance impact
                customer.IncreaseBalance(payment.Amount);

                await _uow.CustomerPayments.SoftDeleteAsync(payment.Id, ct);
                await _uow.Customers.UpdateAsync(customer, ct);

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Customer Payment deleted and balance reversed: {PaymentNo}, Amount {Amount}", payment.PaymentNo, payment.Amount);
                return Result.Success();
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error deleting customer payment");
                return Result.Failure("حدث خطأ أثناء حذف عملية الدفع");
            }
        }, ct);
    }

    public async Task<Result<PagedResult<SupplierPaymentDto>>> GetSupplierPaymentsAsync(string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        Expression<Func<SupplierPayment, bool>> predicate = p =>
            (string.IsNullOrEmpty(search) || p.PaymentNo.Contains(search) || p.Supplier!.Name.Contains(search)) &&
            (!from.HasValue || p.PaymentDate >= from.Value) &&
            (!to.HasValue || p.PaymentDate <= to.Value);

        var totalItems = await _uow.SupplierPayments.CountAsync(predicate, ct);
        var items = await _uow.SupplierPayments.ToListAsync(
            predicate,
            q => q.OrderByDescending(p => p.PaymentDate).Skip((page - 1) * pageSize).Take(pageSize),
            ct,
            false,
            "Supplier");

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<SupplierPaymentDto>>.Success(PagedResult<SupplierPaymentDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<SupplierPaymentDto>> GetSupplierPaymentByIdAsync(int id, CancellationToken ct)
    {
        var payment = await _uow.SupplierPayments.FirstOrDefaultAsync(p => p.Id == id, ct, "Supplier");

        if (payment == null) return Result<SupplierPaymentDto>.Failure("عملية الدفع غير موجودة", ErrorCodes.NotFound);

        return Result<SupplierPaymentDto>.Success(MapToDto(payment));
    }

    public async Task<Result<SupplierPaymentDto>> UpdateSupplierPaymentAsync(int id, UpdateSupplierPaymentRequest request, CancellationToken ct)
    {
        var payment = await _uow.SupplierPayments.GetByIdAsync(id, ct);
        if (payment == null) return Result<SupplierPaymentDto>.Failure("عملية الدفع غير موجودة", ErrorCodes.NotFound);

        var supplier = await _uow.Suppliers.GetByIdAsync(payment.SupplierId, ct);
        if (supplier == null) return Result<SupplierPaymentDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                // Reverse old amount
                supplier.IncreaseBalance(payment.Amount); // Owe them more

                // Apply new values
                payment.Update(
                    request.Amount,
                    (byte)request.PaymentMethod,
                    request.PaymentDate,
                    request.Notes
                );

                // Apply new amount
                supplier.DecreaseBalance(request.Amount); // Owe them less

                await _uow.SupplierPayments.UpdateAsync(payment, ct);
                await _uow.Suppliers.UpdateAsync(supplier, ct);

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Supplier Payment updated: {PaymentNo}, New Amount {Amount}", payment.PaymentNo, payment.Amount);
                return Result<SupplierPaymentDto>.Success(MapToDto(payment));
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                return Result<SupplierPaymentDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error updating supplier payment");
                return Result<SupplierPaymentDto>.Failure("حدث خطأ أثناء تحديث عملية الدفع");
            }
        }, ct);
    }

    public async Task<Result> DeleteSupplierPaymentAsync(int id, CancellationToken ct)
    {
        var payment = await _uow.SupplierPayments.GetByIdAsync(id, ct);
        if (payment == null) return Result.Failure("عملية الدفع غير موجودة", ErrorCodes.NotFound);

        var supplier = await _uow.Suppliers.GetByIdAsync(payment.SupplierId, ct);
        if (supplier == null) return Result.Failure("المورد غير موجود", ErrorCodes.NotFound);

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                // Reverse the balance impact
                supplier.IncreaseBalance(payment.Amount); // What we owe them increases back

                await _uow.SupplierPayments.SoftDeleteAsync(payment.Id, ct);
                await _uow.Suppliers.UpdateAsync(supplier, ct);

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Supplier Payment deleted and balance reversed: {PaymentNo}, Amount {Amount}", payment.PaymentNo, payment.Amount);
                return Result.Success();
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                return Result.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error deleting supplier payment");
                return Result.Failure("حدث خطأ أثناء حذف عملية الدفع");
            }
        }, ct);
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
