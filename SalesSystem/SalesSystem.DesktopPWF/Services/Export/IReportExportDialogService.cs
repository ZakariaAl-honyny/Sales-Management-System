namespace SalesSystem.DesktopPWF.Services.Export;

/// <summary>
/// Service for showing export-related file dialogs and progress.
/// </summary>
public interface IReportExportDialogService
{
    /// <summary>
    /// Shows a SaveFileDialog and returns the selected file path, or null if cancelled.
    /// </summary>
    Task<string?> ShowSaveFileDialogAsync(string defaultName, string filter);

    /// <summary>
    /// Shows a simple export progress indicator.
    /// </summary>
    Task<bool> ShowExportProgressAsync(string reportName, string format);
}
