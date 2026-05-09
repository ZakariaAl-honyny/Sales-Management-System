using System.Data;
using System.Text;
using ClosedXML.Excel;

namespace SalesSystem.Desktop.Helpers;

public static class ExportHelper
{
    public static void ExportToExcel(DataGridView dgv, string sheetName, string fileName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Headers
        int colIndex = 1;
        for (int i = 0; i < dgv.Columns.Count; i++)
        {
            if (!dgv.Columns[i].Visible) continue;
            worksheet.Cell(1, colIndex).Value = dgv.Columns[i].HeaderText;
            worksheet.Cell(1, colIndex).Style.Font.Bold = true;
            worksheet.Cell(1, colIndex).Style.Fill.BackgroundColor = XLColor.LightGray;
            colIndex++;
        }

        // Data
        for (int r = 0; r < dgv.Rows.Count; r++)
        {
            colIndex = 1;
            for (int c = 0; c < dgv.Columns.Count; c++)
            {
                if (!dgv.Columns[c].Visible) continue;
                var val = dgv.Rows[r].Cells[c].Value;
                worksheet.Cell(r + 2, colIndex).Value = val?.ToString() ?? "";
                colIndex++;
            }
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(fileName);
    }

    public static void ExportToCsv(DataGridView dgv, string fileName)
    {
        var sb = new StringBuilder();
        var headers = new List<string>();

        for (int i = 0; i < dgv.Columns.Count; i++)
        {
            if (!dgv.Columns[i].Visible) continue;
            headers.Add(EscapeCsv(dgv.Columns[i].HeaderText));
        }
        sb.AppendLine(string.Join(",", headers));

        for (int r = 0; r < dgv.Rows.Count; r++)
        {
            var rowValues = new List<string>();
            for (int c = 0; c < dgv.Columns.Count; c++)
            {
                if (!dgv.Columns[c].Visible) continue;
                var val = dgv.Rows[r].Cells[c].Value;
                rowValues.Add(EscapeCsv(val?.ToString() ?? ""));
            }
            sb.AppendLine(string.Join(",", rowValues));
        }

        File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeCsv(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Contains(",") || text.Contains("\"") || text.Contains("\n") || text.Contains("\r"))
        {
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        return text;
    }
}

