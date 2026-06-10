using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ClosedXML.Excel;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

/// <summary>
/// ViewModel for the Product Excel Import screen.
/// Handles file selection, Excel parsing, preview via API, and import execution.
/// </summary>
public class ProductImportViewModel : ViewModelBase
{
    private readonly IProductImportApiService _importService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private string _selectedFilePath = string.Empty;
    private ObservableCollection<ProductImportRowDto> _importRows = new();
    private ProductImportResultDto? _importResult;
    private bool _isPreviewMode;
    private bool _hasImportErrors;
    private string? _errorMessage;
    private int _totalParsedRows;
    private bool _hasSelectedFile;

    public ProductImportViewModel()
    {
        _importService = App.GetService<IProductImportApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();

        SetDialogService(_dialogService);
        InitializeCommands();
    }

    public ProductImportViewModel(
        IProductImportApiService importService,
        IDialogService dialogService,
        IToastNotificationService toastService)
    {
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        SetDialogService(_dialogService);
        InitializeCommands();
    }

    private void InitializeCommands()
    {
        SelectFileCommand = new RelayCommand(SelectFile);
        DownloadTemplateCommand = new AsyncRelayCommand(DownloadTemplateAsync);
        PreviewCommand = new AsyncRelayCommand(PreviewAsync);
        ExecuteImportCommand = new AsyncRelayCommand(ExecuteImportAsync);
        ResetCommand = new RelayCommand(Reset);
    }

    #region Properties

    /// <summary>
    /// Full path of the selected Excel file.
    /// </summary>
    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            if (SetProperty(ref _selectedFilePath, value))
            {
                OnPropertyChanged(nameof(SelectedFileName));
                HasSelectedFile = !string.IsNullOrEmpty(value) && File.Exists(value);
            }
        }
    }

    /// <summary>
    /// Display name of the selected file (file name only).
    /// </summary>
    public string SelectedFileName => string.IsNullOrEmpty(SelectedFilePath)
        ? string.Empty
        : Path.GetFileName(SelectedFilePath);

    /// <summary>
    /// True when a valid file has been selected.
    /// </summary>
    public bool HasSelectedFile
    {
        get => _hasSelectedFile;
        private set => SetProperty(ref _hasSelectedFile, value);
    }

    /// <summary>
    /// Parsed rows ready for preview display.
    /// </summary>
    public ObservableCollection<ProductImportRowDto> ImportRows
    {
        get => _importRows;
        set => SetProperty(ref _importRows, value);
    }

    /// <summary>
    /// Result returned from the import execution.
    /// </summary>
    public ProductImportResultDto? ImportResult
    {
        get => _importResult;
        set
        {
            if (SetProperty(ref _importResult, value))
            {
                OnPropertyChanged(nameof(HasImportResult));
                OnPropertyChanged(nameof(ResultSummaryText));
            }
        }
    }

    /// <summary>
    /// True when an import result is available.
    /// </summary>
    public bool HasImportResult => ImportResult != null;

    /// <summary>
    /// Formatted summary text of the import result.
    /// </summary>
    public string ResultSummaryText
    {
        get
        {
            if (ImportResult == null) return string.Empty;
            return $"الإجمالي: {ImportResult.TotalRows} | الناجح: {ImportResult.SuccessCount} | الفاشل: {ImportResult.FailureCount}";
        }
    }

    /// <summary>
    /// True when preview data is being shown.
    /// </summary>
    public bool IsPreviewMode
    {
        get => _isPreviewMode;
        set
        {
            if (SetProperty(ref _isPreviewMode, value))
            {
                OnPropertyChanged(nameof(ShowActions));
            }
        }
    }

    /// <summary>
    /// True when the preview has validation errors.
    /// </summary>
    public bool HasImportErrors
    {
        get => _hasImportErrors;
        set => SetProperty(ref _hasImportErrors, value);
    }

    /// <summary>
    /// Error message for display.
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Number of rows parsed from the Excel file.
    /// </summary>
    public int TotalParsedRows
    {
        get => _totalParsedRows;
        private set => SetProperty(ref _totalParsedRows, value);
    }

    /// <summary>
    /// True when action buttons (Execute Import) should be visible,
    /// i.e., preview is shown and there are no blocking errors.
    /// </summary>
    public bool ShowActions => IsPreviewMode && ImportRows.Count > 0;

    /// <summary>
    /// Import errors from the API preview/execute response.
    /// </summary>
    public List<ProductImportErrorDto> ImportErrors =>
        ImportResult?.Errors ?? new List<ProductImportErrorDto>();

    /// <summary>
    /// True when there are import error details to display.
    /// </summary>
    public bool HasImportErrorDetails =>
        ImportResult != null && ImportResult.Errors.Count > 0;

    #endregion

    #region Commands

    public ICommand SelectFileCommand { get; private set; } = null!;
    public ICommand DownloadTemplateCommand { get; private set; } = null!;
    public ICommand PreviewCommand { get; private set; } = null!;
    public ICommand ExecuteImportCommand { get; private set; } = null!;
    public ICommand ResetCommand { get; private set; } = null!;

    #endregion

    #region Methods

    /// <summary>
    /// Opens a file dialog to select an Excel file (.xlsx).
    /// </summary>
    private void SelectFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "ملفات Excel (.xlsx)|*.xlsx",
            Title = "اختيار ملف Excel للاستيراد"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
            ErrorMessage = null;
            ImportResult = null;
            IsPreviewMode = false;
        }
    }

    /// <summary>
    /// Downloads the import template from the API and saves it to a user-selected location.
    /// </summary>
    private async Task DownloadTemplateAsync()
    {
        await ExecuteAsync(DownloadTemplateOperationAsync, "جاري تنزيل القالب...");
    }

    private async Task DownloadTemplateOperationAsync()
    {
        ErrorMessage = null;

        var bytes = await _importService.DownloadTemplateAsync();
        if (bytes == null || bytes.Length == 0)
        {
            ErrorMessage = "فشل في تنزيل القالب. يرجى التحقق من اتصال الخادم.";
            return;
        }

        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "ملفات Excel (.xlsx)|*.xlsx",
            FileName = "قالب_استيراد_المنتجات.xlsx",
            Title = "حفظ قالب الاستيراد"
        };

        if (saveDialog.ShowDialog() == true)
        {
            await File.WriteAllBytesAsync(saveDialog.FileName, bytes);
            _toastService.ShowSuccess("تم تنزيل قالب الاستيراد بنجاح");
        }
    }

    /// <summary>
    /// Parses the selected Excel file and sends data to the API for preview/validation.
    /// </summary>
    private async Task PreviewAsync()
    {
        if (!HasSelectedFile)
        {
            await _dialogService.ShowWarningAsync("استيراد منتجات", "يرجى اختيار ملف Excel أولاً.");
            return;
        }

        await ExecuteAsync(PreviewOperationAsync, "جاري تحليل الملف وإرسال البيانات للتحقق...");
    }

    private async Task PreviewOperationAsync()
    {
        ErrorMessage = null;
        ImportResult = null;
        IsPreviewMode = false;
        HasImportErrors = false;

        List<ProductImportRowDto> parsedRows;

        try
        {
            parsedRows = ParseExcelFile(SelectedFilePath);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل في قراءة ملف Excel: {ex.Message}";
            LogSystemError("Excel parsing failed", "ProductImportViewModel.PreviewOperationAsync", ex);
            return;
        }

        if (parsedRows.Count == 0)
        {
            ErrorMessage = "لم يتم العثور على بيانات صالحة في الملف. تأكد من أن الملف يحتوي على بيانات في الأعمدة المطلوبة.";
            return;
        }

        TotalParsedRows = parsedRows.Count;

        // Show parsed rows immediately for user visibility
        await InvokeOnUIThreadAsync(async () =>
        {
            ImportRows = new ObservableCollection<ProductImportRowDto>(parsedRows);
            await Task.CompletedTask;
        });

        // Send to API for validation
        var result = await _importService.PreviewAsync(parsedRows);

        if (result == null)
        {
            ErrorMessage = "فشل الاتصال بالخادم للتحقق من البيانات. يرجى المحاولة مرة أخرى.";
            return;
        }

        ImportResult = result;
        HasImportErrors = result.FailureCount > 0;
        IsPreviewMode = true;

        if (result.FailureCount > 0)
        {
            await _dialogService.ShowWarningAsync(
                "تحذير: توجد أخطاء في البيانات",
                $"عدد الصفوف التي تحتوي على أخطاء: {result.FailureCount}\n\n" +
                "يمكنك مراجعة الأخطاء أدناه وتصحيح الملف، أو المتابعة لاستيراد البيانات الصحيحة فقط.");
        }
    }

    /// <summary>
    /// Executes the import by sending data to the API.
    /// </summary>
    private async Task ExecuteImportAsync()
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "تأكيد الاستيراد",
            $"هل أنت متأكد من استيراد {ImportRows.Count} منتج؟\n\n" +
            $"عدد المنتجات الصالحة: {ImportResult?.SuccessCount ?? ImportRows.Count}\n" +
            $"عدد المنتجات التي بها أخطاء: {ImportResult?.FailureCount ?? 0}");

        if (!confirmed) return;

        await ExecuteAsync(ExecuteImportOperationAsync, "جاري استيراد المنتجات...");
    }

    private async Task ExecuteImportOperationAsync()
    {
        ErrorMessage = null;

        var rows = ImportRows.ToList();
        var result = await _importService.ExecuteAsync(rows);

        if (result == null)
        {
            ErrorMessage = "فشل الاتصال بالخادم أثناء الاستيراد. يرجى المحاولة مرة أخرى.";
            return;
        }

        ImportResult = result;
        HasImportErrors = result.FailureCount > 0;

        if (result.SuccessCount > 0)
        {
            _toastService.ShowSuccess($"تم استيراد {result.SuccessCount} منتج بنجاح");
        }

        if (result.FailureCount > 0)
        {
            await _dialogService.ShowWarningAsync(
                "نتيجة الاستيراد",
                $"تم استيراد {result.SuccessCount} منتج بنجاح.\n" +
                $"فشل استيراد {result.FailureCount} منتج.\n\n" +
                "يرجى مراجعة قائمة الأخطاء أدناه لتصحيح البيانات وإعادة المحاولة.");
        }
        else
        {
            await _dialogService.ShowSuccessAsync("استيراد المنتجات", $"تم استيراد {result.SuccessCount} منتج بنجاح.");
        }
    }

    /// <summary>
    /// Resets the ViewModel to its initial state.
    /// </summary>
    private void Reset()
    {
        SelectedFilePath = string.Empty;
        ImportRows.Clear();
        ImportResult = null;
        IsPreviewMode = false;
        HasImportErrors = false;
        ErrorMessage = null;
        TotalParsedRows = 0;
    }

    /// <summary>
    /// Parses an Excel file using ClosedXML and returns a list of import row DTOs.
    /// Expected columns (row 1 = header, data starts from row 2):
    ///   1. Product Name (required)
    ///   2. Category Name (optional)
    ///   3. Barcode (optional)
    ///   4. Base Unit Name (default "قطعة")
    ///   5. Purchase Cost (optional, decimal)
    ///   6. Min Stock Level (optional, decimal)
    ///   7. Description (optional)
    /// </summary>
    private static List<ProductImportRowDto> ParseExcelFile(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var ws = workbook.Worksheet(1);
        var rows = new List<ProductImportRowDto>();

        // Skip header row (row 1), data starts from row 2
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var name = row.Cell(1).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            rows.Add(new ProductImportRowDto(
                ProductName: name,
                CategoryName: row.Cell(2).GetString()?.Trim(),
                Barcode: row.Cell(3).GetString()?.Trim(),
                BaseUnitId: null, // TODO: Phase 25 — map unit name from Excel to Unit entity IDs
                MinStockLevel: decimal.TryParse(row.Cell(6).GetString(), out var min) ? min : null,
                Description: row.Cell(7).GetString()?.Trim()
            ));
        }

        return rows;
    }

    #endregion
}
