using System.Data;
using System.IO;
using ClosedXML.Excel;
using Microsoft.Win32;
using Serilog;
using SalesSystem.Contracts.Common;

namespace SalesSystem.DesktopPWF.Services.Export;

/// <summary>
/// Service for exporting financial reports to Excel (.xlsx) and PDF formats.
/// </summary>
public class FinancialReportExportService : IFinancialReportExportService
{
    /// <inheritdoc />
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

                // Header row
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                headerRow.Style.Font.FontColor = XLColor.White;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                for (int col = 0; col < data.Columns.Count; col++)
                {
                    worksheet.Cell(1, col + 1).Value = data.Columns[col].ColumnName;
                }

                // Data rows
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

                // Total row
                worksheet.Columns().AdjustToContents();
                int lastRow = data.Rows.Count + 3;
                worksheet.Cell(lastRow, 1).Value = "الإجمالي";
                worksheet.Cell(lastRow, 1).Style.Font.Bold = true;

                var lastCol = data.Columns.Count;
                worksheet.Cell(lastRow, lastCol).Value = total;
                worksheet.Cell(lastRow, lastCol).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(lastRow, lastCol).Style.Font.Bold = true;

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

    /// <inheritdoc />
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
                using var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write);
                using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8);

                // Simple HTML-based PDF using basic HTML structure
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html dir='rtl'>");
                writer.WriteLine("<head><meta charset='UTF-8'><title>" + reportName + "</title>");
                writer.WriteLine("<style>");
                writer.WriteLine("body { font-family: 'Traditional Arabic', Arial, sans-serif; margin: 20px; }");
                writer.WriteLine("h1 { text-align: center; color: #333; }");
                writer.WriteLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
                writer.WriteLine("th { background-color: #4472C4; color: white; padding: 8px; text-align: center; }");
                writer.WriteLine("td { padding: 6px; border: 1px solid #ddd; text-align: center; }");
                writer.WriteLine("tr:nth-child(even) { background-color: #f9f9f9; }");
                writer.WriteLine(".total-row { font-weight: bold; background-color: #e8e8e8 !important; }");
                writer.WriteLine("</style></head><body>");
                writer.WriteLine($"<h1>{reportName}</h1>");
                writer.WriteLine("<table>");
                writer.WriteLine("<thead><tr>");

                // Header row
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    writer.WriteLine($"<th>{data.Columns[col].ColumnName}</th>");
                }
                writer.WriteLine("</tr></thead>");
                writer.WriteLine("<tbody>");

                // Data rows
                for (int row = 0; row < data.Rows.Count; row++)
                {
                    writer.WriteLine("<tr>");
                    for (int col = 0; col < data.Columns.Count; col++)
                    {
                        var value = data.Rows[row][col];
                        string displayValue;

                        if (value is decimal decimalValue)
                            displayValue = decimalValue.ToString("N2");
                        else if (value is DateTime dateValue)
                            displayValue = dateValue.ToString("yyyy/MM/dd");
                        else
                            displayValue = value?.ToString() ?? "";

                        writer.WriteLine($"<td>{displayValue}</td>");
                    }
                    writer.WriteLine("</tr>");
                }

                // Total row
                writer.WriteLine("<tr class='total-row'>");
                writer.WriteLine($"<td colspan='{data.Columns.Count - 1}'>الإجمالي</td>");
                writer.WriteLine($"<td>{total:N2}</td>");
                writer.WriteLine("</tr>");

                writer.WriteLine("</tbody></table>");
                writer.WriteLine("</body></html>");
                writer.Flush();
            });

            Log.Information("Report exported to PDF: {FileName}", dialog.FileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export report to PDF: {FileName}", dialog.FileName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> GenerateExcelBytesAsync<T>(string reportName, List<T> data, Dictionary<string, string>? columnHeaders = null)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(reportName);

                // Get properties from T
                var properties = typeof(T).GetProperties();
                var headers = columnHeaders ?? new Dictionary<string, string>();

                // Header row
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                headerRow.Style.Font.FontColor = XLColor.White;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                for (int col = 0; col < properties.Length; col++)
                {
                    var propName = properties[col].Name;
                    worksheet.Cell(1, col + 1).Value = headers.TryGetValue(propName, out var header) ? header : propName;
                }

                // Data rows
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

    /// <inheritdoc />
    public async Task<Result<byte[]>> GeneratePdfBytesAsync(string reportName, DataTable data, string title)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8);

                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html dir='rtl'>");
                writer.WriteLine("<head><meta charset='UTF-8'><title>" + reportName + "</title>");
                writer.WriteLine("<style>");
                writer.WriteLine("body { font-family: 'Traditional Arabic', Arial, sans-serif; margin: 20px; }");
                writer.WriteLine("h1 { text-align: center; color: #333; }");
                writer.WriteLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
                writer.WriteLine("th { background-color: #4472C4; color: white; padding: 8px; text-align: center; }");
                writer.WriteLine("td { padding: 6px; border: 1px solid #ddd; text-align: center; }");
                writer.WriteLine("tr:nth-child(even) { background-color: #f9f9f9; }");
                writer.WriteLine("</style></head><body>");
                writer.WriteLine($"<h1>{title}</h1>");
                writer.WriteLine("<table>");
                writer.WriteLine("<thead><tr>");

                for (int col = 0; col < data.Columns.Count; col++)
                    writer.WriteLine($"<th>{data.Columns[col].ColumnName}</th>");

                writer.WriteLine("</tr></thead>");
                writer.WriteLine("<tbody>");

                for (int row = 0; row < data.Rows.Count; row++)
                {
                    writer.WriteLine("<tr>");
                    for (int col = 0; col < data.Columns.Count; col++)
                    {
                        var value = data.Rows[row][col];
                        string displayValue = value switch
                        {
                            decimal d => d.ToString("N2"),
                            DateTime dt => dt.ToString("yyyy/MM/dd"),
                            _ => value?.ToString() ?? ""
                        };
                        writer.WriteLine($"<td>{displayValue}</td>");
                    }
                    writer.WriteLine("</tr>");
                }

                writer.WriteLine("</tbody></table>");
                writer.WriteLine("</body></html>");
                writer.Flush();

                return Result<byte[]>.Success(stream.ToArray());
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate PDF bytes for report: {ReportName}", reportName);
            return Result<byte[]>.Failure("فشل في تصدير التقرير إلى PDF");
        }
    }
}
