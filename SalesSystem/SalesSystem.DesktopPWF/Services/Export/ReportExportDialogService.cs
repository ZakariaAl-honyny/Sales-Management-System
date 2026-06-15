using Microsoft.Win32;
using Serilog;

namespace SalesSystem.DesktopPWF.Services.Export;

/// <summary>
/// Implementation of IReportExportDialogService using WPF SaveFileDialog.
/// </summary>
public class ReportExportDialogService : IReportExportDialogService
{
    /// <inheritdoc />
    public Task<string?> ShowSaveFileDialogAsync(string defaultName, string filter)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            FileName = defaultName,
            Title = "تصدير التقرير"
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == true ? dialog.FileName : null);
    }

    /// <inheritdoc />
    public Task<bool> ShowExportProgressAsync(string reportName, string format)
    {
        // For now, just log and return true
        Log.Information("Exporting report '{ReportName}' to {Format}", reportName, format);
        return Task.FromResult(true);
    }
}
