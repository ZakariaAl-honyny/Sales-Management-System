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
