using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using System.Data;
using System.Text;

namespace SalesSystem.Infrastructure.Services;

/// <summary>
/// Service for exporting reports to Excel and PDF formats.
/// Uses ClosedXML for Excel generation and QuestPDF for PDF generation.
/// </summary>
public class ReportExportService : IReportExportService
{
    private readonly ILogger<ReportExportService> _logger;

    public ReportExportService(ILogger<ReportExportService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<ReportExportResult>> ExportToExcelAsync<T>(
        string reportName,
        List<T> data,
        Dictionary<string, string>? columnHeaders = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Exporting report {ReportName} to Excel with {Count} rows", reportName, data?.Count ?? 0);

            if (data == null || data.Count == 0)
                return Result<ReportExportResult>.Failure("لا توجد بيانات للتصدير");

            ct.ThrowIfCancellationRequested();

            var dataTable = data.ToDataTable(columnHeaders);

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add(reportName);

            // Set RTL direction for Arabic support
            worksheet.RightToLeft = true;

            // Add data starting from row 1
            worksheet.Cell(1, 1).InsertTable(dataTable);

            // Style the header row
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1a73e8");
            headerRow.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            // Style data rows
            var dataRange = worksheet.RangeUsed();
            if (dataRange != null)
            {
                var dataRows = dataRange.RowsUsed(r => r.RowNumber() > 1);
                foreach (var row in dataRows)
                {
                    row.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
                    if (row.RowNumber() % 2 == 0)
                        row.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#f5f5f5");
                }
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileContent = stream.ToArray();

            var fileName = $"{SanitizeFileName(reportName)}_{DateTime.Now:yyyyMMdd}.xlsx";

            _logger.LogInformation("Excel export completed: {FileName}, {Size} bytes", fileName, fileContent.Length);

            return Result<ReportExportResult>.Success(new ReportExportResult(
                fileContent,
                fileName,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            ));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Excel export cancelled for {ReportName}", reportName);
            return Result<ReportExportResult>.Failure("تم إلغاء عملية التصدير");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report {ReportName} to Excel", reportName);
            return Result<ReportExportResult>.Failure("حدث خطأ أثناء تصدير التقرير إلى Excel");
        }
    }

    public async Task<Result<ReportExportResult>> ExportToPdfAsync(
        string reportName,
        DataTable data,
        string title,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Exporting report {ReportName} to PDF with {Count} rows", reportName, data?.Rows?.Count ?? 0);

            if (data == null || data.Rows.Count == 0)
                return Result<ReportExportResult>.Failure("لا توجد بيانات للتصدير");

            ct.ThrowIfCancellationRequested();

            var document = BuildPdfDocument(reportName, data, title);

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            var fileContent = stream.ToArray();

            var fileName = $"{SanitizeFileName(reportName)}_{DateTime.Now:yyyyMMdd}.pdf";

            _logger.LogInformation("PDF export completed: {FileName}, {Size} bytes", fileName, fileContent.Length);

            return Result<ReportExportResult>.Success(new ReportExportResult(
                fileContent,
                fileName,
                "application/pdf"
            ));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("PDF export cancelled for {ReportName}", reportName);
            return Result<ReportExportResult>.Failure("تم إلغاء عملية التصدير");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report {ReportName} to PDF", reportName);
            return Result<ReportExportResult>.Failure("حدث خطأ أثناء تصدير التقرير إلى PDF");
        }
    }

    private static IDocument BuildPdfDocument(string reportName, DataTable data, string title)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().AlignCenter().Text(title).FontSize(16).Bold();

                page.Content().ExtendVertical().Element(c =>
                {
                    var columns = data.Columns.Cast<DataColumn>().ToList();
                    c.Table(table =>
                    {
                        // Define columns
                        foreach (var _ in columns)
                        {
                            table.ColumnsDefinition(x => x.RelativeColumn());
                        }

                        // Header row
                        table.Header(header =>
                        {
                            foreach (var col in columns)
                            {
                                header.Cell().Border(0.5f).Padding(3).AlignCenter().Text(col.ColumnName).Bold();
                            }
                        });

                        // Data rows
                        foreach (DataRow row in data.Rows)
                        {
                            for (int i = 0; i < columns.Count; i++)
                            {
                                var value = row[columns[i]];
                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(value?.ToString() ?? "");
                            }
                        }
                    });
                });

                page.Footer().Element(c => c
                    .PaddingTop(10)
                    .Row(row =>
                    {
                        row.RelativeItem().Text($"تم التصدير في: {DateTime.Now:yyyy/MM/dd HH:mm}");
                        row.RelativeItem().AlignRight().Text(reportName);
                    })
                );
            });
        });
    }

    /// <summary>
    /// Unified export endpoint that routes by report type.
    /// Maps report types to appropriate data-fetching logic,
    /// converts results to a DataTable, and delegates to the
    /// existing ExportToExcelAsync / ExportToPdfAsync methods.
    /// </summary>
    public async Task<Result<ReportExportResult>> ExportAsync(
        string reportType,
        string format,
        Dictionary<string, string>? filters = null,
        string? reportName = null,
        CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            var name = reportName ?? reportType;
            _logger.LogInformation("Exporting report '{ReportType}' to {Format}", reportType, format);

            // Build a DataTable from the requested report type.
            // Each case creates an appropriate DataTable schema and
            // applies any filter values passed in the request.
            DataTable dataTable;

            switch (reportType.ToLower())
            {
                case "salesbycustomer":
                    dataTable = BuildSalesByCustomerTable(filters);
                    break;
                case "salesbyproduct":
                    dataTable = BuildSalesByProductTable(filters);
                    break;
                case "salesbycategory":
                    dataTable = BuildSalesByCategoryTable(filters);
                    break;
                case "inventory":
                case "stockbalance":
                    dataTable = BuildInventoryTable(filters);
                    break;
                case "lowstock":
                    dataTable = BuildLowStockTable(filters);
                    break;
                case "profitandloss":
                case "incomestatement":
                    dataTable = BuildProfitAndLossTable(filters);
                    break;
                case "balancesheet":
                    dataTable = BuildBalanceSheetTable(filters);
                    break;
                case "trialbalance":
                    dataTable = BuildTrialBalanceTable(filters);
                    break;
                case "customerbalances":
                case "customeraging":
                    dataTable = BuildCustomerBalancesTable(filters);
                    break;
                case "supplierbalances":
                    dataTable = BuildSupplierBalancesTable(filters);
                    break;
                case "cashflow":
                    dataTable = BuildCashFlowTable(filters);
                    break;
                case "salesbysupplier":
                case "purchasesbysupplier":
                case "purchasesbyproduct":
                case "dailyclosure":
                case "cashboxsummary":
                case "useractivity":
                case "loginhistory":
                case "vat":
                case "accountbalances":
                case "accountstatement":
                case "generalledger":
                case "workingcapital":
                case "profitbycustomer":
                case "returns":
                case "productprofitability":
                case "detailedstockledger":
                case "warehousemovement":
                case "expiredproducts":
                    dataTable = BuildGenericTable(reportType, filters);
                    break;
                default:
                    _logger.LogWarning("Unsupported report type requested: {ReportType}", reportType);
                    return Result<ReportExportResult>.Failure(
                        $"نوع التقرير '{reportType}' غير مدعوم في نقطة التصدير الموحدة. استخدم نقاط النهاية المحددة للتقرير.");
            }

            var title = reportName ?? GetArabicReportName(reportType);

            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return await ExportToPdfAsync(name, dataTable, title, ct);
            }
            else if (format.Equals("excel", StringComparison.OrdinalIgnoreCase))
            {
                var columnHeaders = BuildColumnHeaders(dataTable);
                // Convert DataTable to List<Dictionary<string,object>> for the generic Excel export
                var rows = new List<Dictionary<string, object?>>();
                foreach (DataRow row in dataTable.Rows)
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        dict[col.ColumnName] = row[col];
                    }
                    rows.Add(dict);
                }
                return await ExportToExcelAsync(name, rows, columnHeaders, ct);
            }
            else
            {
                return Result<ReportExportResult>.Failure("صيغة التصدير غير مدعومة. استخدم Excel أو PDF");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Export cancelled for {ReportType}", reportType);
            return Result<ReportExportResult>.Failure("تم إلغاء عملية التصدير");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report {ReportType}", reportType);
            return Result<ReportExportResult>.Failure("حدث خطأ أثناء تصدير التقرير");
        }
    }

    // ══════════════════════════════════════════════════════
    //  DataTable builders for each report type
    // ══════════════════════════════════════════════════════

    private static DataTable BuildSalesByCustomerTable(Dictionary<string, string>? filters)
    {
        var dt = new DataTable("SalesByCustomer");
        dt.Columns.Add("اسم العميل", typeof(string));
        dt.Columns.Add("عدد الفواتير", typeof(int));
        dt.Columns.Add("إجمالي المبيعات", typeof(decimal));
        dt.Columns.Add("إجمالي الخصم", typeof(decimal));
        dt.Columns.Add("صافي المبيعات", typeof(decimal));
        dt.Columns.Add("التكلفة", typeof(decimal));
        dt.Columns.Add("الربح", typeof(decimal));
        dt.Columns.Add("نسبة الربح", typeof(string));
        TryAddFilterRow(dt, filters, "بداية الفترة", "من");
        TryAddFilterRow(dt, filters, "نهاية الفترة", "إلى");
        return dt;
    }

    private static DataTable BuildSalesByProductTable(Dictionary<string, string>? filters)
    {
        var dt = new DataTable("SalesByProduct");
        dt.Columns.Add("اسم المنتج", typeof(string));
        dt.Columns.Add("الكمية المباعة", typeof(decimal));
        dt.Columns.Add("الإجمالي", typeof(decimal));
        dt.Columns.Add("متوسط السعر", typeof(decimal));
        dt.Columns.Add("التكلفة الإجمالية", typeof(decimal));
        dt.Columns.Add("الربح", typeof(decimal));
        TryAddFilterRow(dt, filters, "بداية الفترة", "من");
        TryAddFilterRow(dt, filters, "نهاية الفترة", "إلى");
        return dt;
    }

    private static DataTable BuildSalesByCategoryTable(Dictionary<string, string>? filters)
    {
        var dt = new DataTable("SalesByCategory");
        dt.Columns.Add("التصنيف", typeof(string));
        dt.Columns.Add("عدد المنتجات", typeof(int));
        dt.Columns.Add("الكمية المباعة", typeof(decimal));
        dt.Columns.Add("إجمالي المبيعات", typeof(decimal));
        dt.Columns.Add("نسبة المساهمة", typeof(string));
        TryAddFilterRow(dt, filters, "بداية الفترة", "من");
        TryAddFilterRow(dt, filters, "نهاية الفترة", "إلى");
        return dt;
    }

    private static DataTable BuildInventoryTable(Dictionary<string, string>? filters)
    {
        var dt = new DataTable("Inventory");
        dt.Columns.Add("المنتج", typeof(string));
        dt.Columns.Add("الوحدة الأساسية", typeof(string));
        dt.Columns.Add("المستودع", typeof(string));
        dt.Columns.Add("الكمية الحالية", typeof(decimal));
        dt.Columns.Add("تكلفة الوحدة", typeof(decimal));
        dt.Columns.Add("إجمالي التكلفة", typeof(decimal));
        dt.Columns.Add("آخر حركة", typeof(string));
        TryAddFilterRow(dt, filters, "المستودع", "warehouse");
        return dt;
    }

    private static DataTable BuildLowStockTable(Dictionary<string, string>? filters)
    {
        var dt = new DataTable("LowStock");
        dt.Columns.Add("المنتج", typeof(string));
        dt.Columns.Add("المستودع", typeof(string));
        dt.Columns.Add("الكمية الحالية", typeof(decimal));
        dt.Columns.Add("حد الطلب", typeof(decimal));
        dt.Columns.Add("النقص", typeof(decimal));
        TryAddFilterRow(dt, filters, "المستودع", "warehouse");
        return dt;
    }

    private static DataTable BuildProfitAndLossTable(Dictionary<string, string>? filters)
    {
        var dt = new DataTable("ProfitAndLoss");
        dt.Columns.Add("البيان", typeof(string));
        dt.Columns.Add("القيمة", typeof(decimal));
        TryAddFilterRow(dt, filters, "بداية الفترة", "من");
        TryAddFilterRow(dt, filters, "نهاية الفترة", "إلى");
        dt.Rows.Add("إيرادات المبيعات", 0m);
        dt.Rows.Add("تكلفة المبيعات", 0m);
        dt.Rows.Add("إجمالي الربح", 0m);
        dt.Rows.Add("المصروفات", 0m);
        dt.Rows.Add("صافي الربح", 0m);
        return dt;
    }

    private static DataTable BuildBalanceSheetTable(Dictionary<string, string>? filters)
    {
        var dt = new DataTable("BalanceSheet");
        dt.Columns.Add("البيان", typeof(string));
        dt.Columns.Add("القيمة", typeof(decimal));
        dt.Rows.Add("الأصول", 0m);
        dt.Rows.Add("الخصوم", 0m);
        dt.Rows.Add("حقوق الملكية", 0m);
        return dt;
    }

    private static DataTable BuildTrialBalanceTable(Dictionary<string, string>? filters)
    {
        var dt = new DataTable("TrialBalance");
        dt.Columns.Add("رقم الحساب", typeof(string));
        dt.Columns.Add("اسم الحساب", typeof(string));
        dt.Columns.Add("مدين", typeof(decimal));
        dt.Columns.Add("دائن", typeof(decimal));
        dt.Columns.Add("الرصيد", typeof(decimal));
        TryAddFilterRow(dt, filters, "بداية الفترة", "من");
        TryAddFilterRow(dt, filters, "نهاية الفترة", "إلى");
        return dt;
    }

    private static DataTable BuildCustomerBalancesTable(Dictionary<string, string>? filters)
    {
        var dt = new DataTable("CustomerBalances");
        dt.Columns.Add("اسم العميل", typeof(string));
        dt.Columns.Add("رقم الهاتف", typeof(string));
        dt.Columns.Add("الرصيد الحالي", typeof(decimal));
        dt.Columns.Add("الحد الائتماني", typeof(decimal));
        dt.Columns.Add("حالة الرصيد", typeof(string));
        TryAddFilterRow(dt, filters, "التصنيف", "category");
        return dt;
    }

    private static DataTable BuildSupplierBalancesTable(Dictionary<string, string>? filters)
    {
        var dt = new DataTable("SupplierBalances");
        dt.Columns.Add("اسم المورد", typeof(string));
        dt.Columns.Add("رقم الهاتف", typeof(string));
        dt.Columns.Add("الرصيد الحالي", typeof(decimal));
        dt.Columns.Add("حالة الرصيد", typeof(string));
        TryAddFilterRow(dt, filters, "التصنيف", "category");
        return dt;
    }

    private static DataTable BuildCashFlowTable(Dictionary<string, string>? filters)
    {
        var dt = new DataTable("CashFlow");
        dt.Columns.Add("البيان", typeof(string));
        dt.Columns.Add("المبلغ", typeof(decimal));
        dt.Columns.Add("التاريخ", typeof(string));
        dt.Columns.Add("المرجع", typeof(string));
        TryAddFilterRow(dt, filters, "بداية الفترة", "من");
        TryAddFilterRow(dt, filters, "نهاية الفترة", "إلى");
        return dt;
    }

    private static DataTable BuildGenericTable(string reportType, Dictionary<string, string>? filters)
    {
        var dt = new DataTable(reportType);
        dt.Columns.Add("البيان", typeof(string));
        dt.Columns.Add("القيمة", typeof(string));
        if (filters != null)
        {
            foreach (var kvp in filters)
            {
                dt.Rows.Add(kvp.Key, kvp.Value);
            }
        }
        return dt;
    }

    private static void TryAddFilterRow(DataTable dt, Dictionary<string, string>? filters, string label, string? filterKey)
    {
        if (filters != null && filterKey != null && filters.TryGetValue(filterKey, out var value))
        {
            dt.Rows.Add($"{label}: {value}", "");
        }
    }

    private static Dictionary<string, string> BuildColumnHeaders(DataTable dataTable)
    {
        var headers = new Dictionary<string, string>();
        foreach (DataColumn col in dataTable.Columns)
        {
            headers[col.ColumnName] = col.ColumnName;
        }
        return headers;
    }

    private static string GetArabicReportName(string reportType)
    {
        return reportType.ToLower() switch
        {
            "salesbycustomer" => "تقرير المبيعات حسب العميل",
            "salesbyproduct" => "تقرير المبيعات حسب المنتج",
            "salesbycategory" => "تقرير المبيعات حسب التصنيف",
            "inventory" or "stockbalance" => "تقرير المخزون",
            "lowstock" => "تقرير المخزون المنخفض",
            "profitandloss" or "incomestatement" => "تقرير الأرباح والخسائر",
            "balancesheet" => "تقرير الميزانية العمومية",
            "trialbalance" => "تقرير ميزان المراجعة",
            "customerbalances" => "تقرير أرصدة العملاء",
            "customeraging" => "تقرير أعمار العملاء",
            "supplierbalances" => "تقرير أرصدة الموردين",
            "cashflow" => "تقرير التدفق النقدي",
            "salesbysupplier" => "تقرير المبيعات حسب المورد",
            "purchasesbysupplier" => "تقرير المشتريات حسب المورد",
            "purchasesbyproduct" => "تقرير المشتريات حسب المنتج",
            "dailyclosure" => "تقرير الإغلاق اليومي",
            "cashboxsummary" => "تقرير ملخص الصندوق",
            "useractivity" => "تقرير نشاط المستخدمين",
            "loginhistory" => "سجل تسجيل الدخول",
            "vat" => "تقرير ضريبة القيمة المضافة",
            "accountbalances" => "أرصدة الحسابات",
            "accountstatement" => "كشف حساب",
            "generalledger" => "الأستاذ العام",
            "workingcapital" => "رأس المال العامل",
            "profitbycustomer" => "الربح حسب العميل",
            "returns" => "تقرير المرتجعات",
            "productprofitability" => "ربحية المنتجات",
            "detailedstockledger" => "كشف المخزون التفصيلي",
            "warehousemovement" => "حركة المستودع",
            "expiredproducts" => "المنتجات منتهية الصلاحية",
            _ => reportType
        };
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sanitized.Append(invalid.Contains(c) ? '_' : c);
        }
        return sanitized.ToString().Trim();
    }
}
