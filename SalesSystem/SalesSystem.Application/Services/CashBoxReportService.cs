using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public class CashBoxReportService : ICashBoxReportService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CashBoxReportService> _logger;

    public CashBoxReportService(IUnitOfWork uow, ILogger<CashBoxReportService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<CashBoxSummaryDto>>> GetCashBoxSummaryAsync(DateTime? asOfDate, CancellationToken ct)
    {
        try
        {
            var effectiveDate = asOfDate ?? DateTime.Today;
            _logger.LogInformation("Getting cash box summary as of {Date}", effectiveDate);

            var cashBoxes = await _uow.CashBoxes.ToListAsync(cb => cb.IsActive, ct: ct);
            if (cashBoxes.Count == 0)
                return Result<List<CashBoxSummaryDto>>.Success(new List<CashBoxSummaryDto>());

            var result = new List<CashBoxSummaryDto>();

            foreach (var box in cashBoxes)
            {
                // Get all transactions for this box up to effectiveDate
                var transactions = await _uow.CashTransactions.ToListAsync(
                    tx => tx.CashBoxId == box.Id && tx.CreatedAt <= effectiveDate,
                    q => q.OrderBy(tx => tx.CreatedAt),
                    ct);

                // Opening balance: RunningBalance of first transaction minus its Amount
                decimal openingBalance = 0;
                if (transactions.Count > 0)
                {
                    var firstTx = transactions.First();
                    openingBalance = firstTx.RunningBalance - firstTx.Amount;
                }

                // Income: SalesIncome, CustomerPayment, TransferIn
                var totalIncome = transactions
                    .Where(tx => tx.TransactionType == CashTransactionType.SalesIncome
                              || tx.TransactionType == CashTransactionType.CustomerPayment
                              || tx.TransactionType == CashTransactionType.TransferIn)
                    .Sum(tx => Math.Abs(tx.Amount));

                // Expense: Expense, SupplierPayment, RefundOut, TransferOut
                var totalExpense = transactions
                    .Where(tx => tx.TransactionType == CashTransactionType.Expense
                              || tx.TransactionType == CashTransactionType.SupplierPayment
                              || tx.TransactionType == CashTransactionType.RefundOut
                              || tx.TransactionType == CashTransactionType.TransferOut)
                    .Sum(tx => Math.Abs(tx.Amount));

                var closingBalance = transactions.Count > 0 ? transactions.Last().RunningBalance : 0;

                result.Add(new CashBoxSummaryDto(
                    box.Id, box.BoxName,
                    openingBalance, totalIncome, totalExpense, closingBalance
                ));
            }

            return Result<List<CashBoxSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cash box summary");
            return Result<List<CashBoxSummaryDto>>.Failure("حدث خطأ أثناء إنشاء ملخص الصناديق");
        }
    }

    public async Task<Result<List<DailyClosureReportDto>>> GetDailyClosureReportAsync(DateTime from, DateTime to, int? cashBoxId, CancellationToken ct)
    {
        try
        {
            if (from > to)
                return Result<List<DailyClosureReportDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting daily closure report from {From} to {To}", from, to);

            var closures = await _uow.DailyClosures.ToListAsync(
                dc => dc.ClosureDate >= DateOnly.FromDateTime(from)
                   && dc.ClosureDate <= DateOnly.FromDateTime(to)
                   && (!cashBoxId.HasValue || dc.CashBoxId == cashBoxId.Value),
                q => q.Include(dc => dc.CashBox).OrderByDescending(dc => dc.ClosureDate),
                ct);

            var result = closures.Select(dc => new DailyClosureReportDto(
                dc.CashBoxId,
                dc.CashBox?.BoxName ?? "غير معروف",
                dc.ClosureDate.ToDateTime(TimeOnly.MinValue),
                dc.OpeningBalance,
                dc.TotalIncome,
                dc.TotalExpense,
                dc.ExpectedClosingBalance,
                dc.ActualCashCount,
                dc.Difference,
                dc.IsReconciled,
                dc.Notes
            )).ToList();

            return Result<List<DailyClosureReportDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily closure report");
            return Result<List<DailyClosureReportDto>>.Failure("حدث خطأ أثناء إنشاء تقرير الإغلاق اليومي");
        }
    }

    public async Task<Result<List<CashTransactionDetailDto>>> GetCashTransactionDetailsAsync(int cashBoxId, DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            if (cashBoxId <= 0)
                return Result<List<CashTransactionDetailDto>>.Failure("معرف الصندوق غير صالح");
            if (from > to)
                return Result<List<CashTransactionDetailDto>>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            _logger.LogInformation("Getting cash transaction details for box {BoxId} from {From} to {To}", cashBoxId, from, to);

            var cashBox = await _uow.CashBoxes.GetByIdAsync(cashBoxId, ct);
            if (cashBox == null)
                return Result<List<CashTransactionDetailDto>>.Failure("الصندوق غير موجود");

            var transactions = await _uow.CashTransactions.ToListAsync(
                tx => tx.CashBoxId == cashBoxId && tx.CreatedAt >= from && tx.CreatedAt <= to,
                q => q.OrderBy(tx => tx.CreatedAt),
                ct);

            var result = transactions.Select(tx =>
            {
                var typeDisplay = tx.TransactionType switch
                {
                    CashTransactionType.OpeningBalance => "رصيد افتتاحي",
                    CashTransactionType.SalesIncome => "إيراد مبيعات",
                    CashTransactionType.Expense => "مصروف",
                    CashTransactionType.TransferOut => "تحويل خارج",
                    CashTransactionType.TransferIn => "تحويل داخل",
                    CashTransactionType.RefundOut => "مرتجع",
                    CashTransactionType.SupplierPayment => "دفعة مورد",
                    CashTransactionType.CustomerPayment => "دفعة عميل",
                    _ => tx.TransactionType.ToString()
                };

                return new CashTransactionDetailDto(
                    tx.CashBoxId,
                    cashBox.BoxName,
                    tx.CreatedAt,
                    typeDisplay,
                    tx.Notes,
                    tx.Amount,
                    tx.RunningBalance,
                    tx.ReferenceType,
                    tx.ReferenceId
                );
            }).ToList();

            return Result<List<CashTransactionDetailDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cash transaction details");
            return Result<List<CashTransactionDetailDto>>.Failure("حدث خطأ أثناء إنشاء تفاصيل الحركات النقدية");
        }
    }
}
