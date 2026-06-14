using System.Data;
using System.IO;
using ClosedXML.Excel;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SalesSystem.Contracts.Common;
using Serilog;

namespace SalesSystem.DesktopPWF.Services.Export;

/// <summary>
/// Service for exporting financial reports to Excel (.xlsx) and PDF formats.
/// Uses ClosedXML for Excel generation and QuestPDF for real PDF generation
/// with RTL support, alternating row colors, page numbers, and proper Arabic layout.
/// </summary>
public class FinancialReportExportService : IFinancialReportExportService
{
    private const string PrimaryColor = "#1565C0";
    private const string AccentColor = "#E3F2FD";
    private const string HeaderBgColor = "#1a73e8";

    // ═══════════════════════════════════════════════
    // Excel Export — ClosedXML
    // ═══════════════════════════════════════════════

    public async Task ExportToExcelAsync(string reportName, DataTable data, decimal total, string fileName)
    {
        if (data.Rows.Count == 0)
        {
            Log.Warning("ExportToExcelAsync called with empty data for report: {ReportName}", reportName);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ? fileName : $"{fileName}.xlsx",
            Title = "تصدير إلى Excel"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(reportName);

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBgColor);
                headerRow.Style.Font.FontColor = XLColor.White;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                for (int col = 0; col < data.Columns.Count; col++)
                    worksheet.Cell(1, col + 1).Value = data.Columns[col].ColumnName;

                for (int row = 0; row < data.Rows.Count; row++)
                {
                    for (int col = 0; col < data.Columns.Count; col++)
                    {
                        var cell = worksheet.Cell(row + 2, col + 1);
                        var value = data.Rows[row][col];

                        if (value is decimal decimalValue)
                        {
                            cell.Value = decimalValue;
                            cell.Style.NumberFormat.Format = "#,##0.00";
                        }
                        else if (value is DateTime dateValue)
                        {
                            cell.Value = dateValue;
                            cell.Style.DateFormat.Format = "yyyy/MM/dd";
                        }
                        else
                        {
                            cell.Value = value?.ToString() ?? "";
                        }
                    }
                }

                worksheet.Columns().AdjustToContents();
                int lastRow = data.Rows.Count + 3;
                worksheet.Cell(lastRow, 1).Value = "الإجمالي";
                worksheet.Cell(lastRow, 1).Style.Font.Bold = true;
                worksheet.Cell(lastRow, data.Columns.Count).Value = total;
                worksheet.Cell(lastRow, data.Columns.Count).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(lastRow, data.Columns.Count).Style.Font.Bold = true;

                workbook.SaveAs(dialog.FileName);
            });

            Log.Information("Report exported to Excel: {FileName}", dialog.FileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export report to Excel: {FileName}", dialog.FileName);
            throw;
        }
    }

    // ═══════════════════════════════════════════════
    // PDF Export — QuestPDF (Real PDF, not HTML)
    // ═══════════════════════════════════════════════

    public async Task ExportToPdfAsync(string reportName, DataTable data, decimal total, string fileName)
    {
        if (data.Rows.Count == 0)
        {
            Log.Warning("ExportToPdfAsync called with empty data for report: {ReportName}", reportName);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? fileName : $"{fileName}.pdf",
            Title = "تصدير إلى PDF"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await Task.Run(() =>
            {
                var document = BuildReportPdf(reportName, data, total);
                document.GeneratePdf(dialog.FileName);
            });

            Log.Information("Report exported to PDF: {FileName}", dialog.FileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export report to PDF: {FileName}", dialog.FileName);
            throw;
        }
    }

    /// <summary>
    /// Generates PDF bytes from report data using a generic list of DTOs.
    /// </summary>
    public async Task<Result<byte[]>> GenerateExcelBytesAsync<T>(string reportName, List<T> data, Dictionary<string, string>? columnHeaders = null)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(reportName);

                var properties = typeof(T).GetProperties();
                var headers = columnHeaders ?? new Dictionary<string, string>();

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBgColor);
                headerRow.Style.Font.FontColor = XLColor.White;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                for (int col = 0; col < properties.Length; col++)
                {
                    var propName = properties[col].Name;
                    worksheet.Cell(1, col + 1).Value = headers.TryGetValue(propName, out var header) ? header : propName;
                }

                for (int row = 0; row < data.Count; row++)
                {
                    for (int col = 0; col < properties.Length; col++)
                    {
                        var cell = worksheet.Cell(row + 2, col + 1);
                        var value = properties[col].GetValue(data[row]);

                        if (value is decimal decimalValue)
                        {
                            cell.Value = decimalValue;
                            cell.Style.NumberFormat.Format = "#,##0.00";
                        }
                        else if (value is DateTime dateValue)
                        {
                            cell.Value = dateValue;
                            cell.Style.DateFormat.Format = "yyyy/MM/dd";
                        }
                        else
                        {
                            cell.Value = value?.ToString() ?? "";
                        }
                    }
                }

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return Result<byte[]>.Success(stream.ToArray());
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate Excel bytes for report: {ReportName}", reportName);
            return Result<byte[]>.Failure("فشل في تصدير التقرير إلى Excel");
        }
    }

    /// <summary>
    /// Generates PDF bytes using QuestPDF from a DataTable — suitable for API consumption.
    /// </summary>
    public async Task<Result<byte[]>> GeneratePdfBytesAsync(string reportName, DataTable data, string title)
    {
        try
        {
            return await Task.Run(() =>
            {
                var document = BuildReportPdf(title, data, 0);
                using var stream = new MemoryStream();
                document.GeneratePdf(stream);
                return Result<byte[]>.Success(stream.ToArray());
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate PDF bytes for report: {ReportName}", reportName);
            return Result<byte[]>.Failure("فشل في تصدير التقرير إلى PDF");
        }
    }

    // ═══════════════════════════════════════════════
    // QuestPDF Document Builder
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Builds a QuestPDF document for a report with RTL support,
    /// title header, alternating row colors, page numbers, and export metadata.
    /// </summary>
    private static IDocument BuildReportPdf(string title, DataTable data, decimal total)
    {
        var columns = data.Columns.Cast<DataColumn>().ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(style =>
                    style.FontFamily("Arial").FontSize(9));
                page.ContentFromRightToLeft();

                // ── Header ──
                page.Header().Element(c => ComposeHeader(c, title));

                // ── Content ──
                page.Content().Element(c => ComposeContent(c, columns, data, total));

                // ── Footer ──
                page.Footer().Element(ComposeFooter);
            });
        });
    }

    private static void ComposeHeader(IContainer container, string title)
    {
        container
            .BorderBottom(2).BorderColor(PrimaryColor)
            .PaddingBottom(8)
            .Row(row =>
            {
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item()
                        .Text(title)
                        .FontSize(16).Bold().FontColor(PrimaryColor);

                    col.Item()
                        .Text($"تم التصدير في: {DateTime.Now:yyyy/MM/dd HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });

                row.ConstantItem(120)
                    .Background(AccentColor)
                    .Padding(6)
                    .AlignCenter().AlignMiddle()
                    .Column(col =>
                    {
                        col.Item().Text("تقرير").FontSize(10).Bold().FontColor(PrimaryColor);
                        col.Item().Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(8).FontColor(Colors.Grey.Medium);
                    });
            });
    }

    private static void ComposeContent(IContainer container, List<DataColumn> columns, DataTable data, decimal total)
    {
        container.Table(table =>
        {
            // Define columns — equal width
            foreach (var _ in columns)
            {
                table.ColumnsDefinition(x => x.RelativeColumn());
            }

            // Header row
            table.Header(header =>
            {
                foreach (var col in columns)
                {
                    header.Cell()
                        .Background(PrimaryColor)
                        .Padding(6)
                        .DefaultTextStyle(s => s.FontColor("#FFFFFF").Bold().FontSize(9))
                        .AlignCenter()
                        .Text(col.ColumnName);
                }
            });

            // Data rows
            int rowNum = 0;
            foreach (DataRow row in data.Rows)
            {
                var isEven = rowNum % 2 == 0;
                var bgColor = isEven ? "#FFFFFF" : "#F5F5F5";

                for (int i = 0; i < columns.Count; i++)
                {
                    var value = row[columns[i]];
                    var displayValue = FormatValue(value);

                    table.Cell()
                        .Background(bgColor)
                        .BorderBottom(0.5f).BorderColor("#E0E0E0")
                        .Padding(5)
                        .AlignRight()
                        .Text(displayValue)
                        .FontSize(8);
                }
                rowNum++;
            }

            // Total row (if total > 0)
            if (total > 0)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    var isLast = i == columns.Count - 1;
                    table.Cell()
                        .Background("#E8EAF6")
                        .BorderBottom(1).BorderColor(PrimaryColor)
                        .Padding(6)
                        .DefaultTextStyle(s => s.Bold().FontSize(9))
                        .AlignRight()
                        .Text(isLast ? total.ToString("N2") : (i == 0 ? "الإجمالي" : ""));
                }
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container
            .BorderTop(1).BorderColor("#E0E0E0")
            .PaddingTop(6)
            .Row(row =>
            {
                row.RelativeItem().AlignRight()
                    .Text(ctx =>
                    {
                        ctx.Span("صفحة ").FontColor(Colors.Grey.Medium).FontSize(8);
                        ctx.CurrentPageNumber().FontColor(Colors.Grey.Medium).FontSize(8);
                        ctx.Span(" من ").FontColor(Colors.Grey.Medium).FontSize(8);
                        ctx.TotalPages().FontColor(Colors.Grey.Medium).FontSize(8);
                    });

                row.RelativeItem().AlignCenter()
                    .Text($"نظام إدارة المبيعات — {DateTime.Now:yyyy/MM/dd}")
                    .FontSize(8).FontColor(Colors.Grey.Medium).Italic();

                row.RelativeItem().AlignLeft()
                    .Text(DateTime.Now.ToString("HH:mm"))
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            decimal d => d.ToString("N2"),
            DateTime dt => dt.ToString("yyyy/MM/dd"),
            bool b => b ? "نعم" : "لا",
            _ => value?.ToString() ?? ""
        };
    }
}
