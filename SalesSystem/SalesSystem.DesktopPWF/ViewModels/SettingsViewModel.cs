using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Settings ViewModel - handles system settings management
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsApiService _settingsService;
    private readonly IPrintApiService _printService;

    private string _companyName = string.Empty;
    private string? _taxNumber;
    private string? _phone;
    private string? _email;
    private string? _address;
    private decimal _defaultTaxRate;
    private string _invoicePrefix = "INV";
    private bool _enableStockAlerts = true;
    private bool _allowNegativeStock;
    private bool _autoUpdatePrices;
    private int _costingMethod = 1; // Default WeightedAverage
    private string _thermalPrinterName = string.Empty;
    private string _a4PrinterName = string.Empty;
    private string _logoPath = string.Empty;
    private string _storeTaxNumber = string.Empty;
    private decimal _printTaxRate;
    private string _receiptHeader = string.Empty;
    private string _receiptFooter = string.Empty;
    private int _escPosCodePage = 22;
    private bool _autoPrintOnPost;
    private string _paperSize = "A4";
    private int _printCopies = 1;
    private bool _showBalanceOnPrint = true;
    private bool _printSignature;
    private bool _showLogo = true;
    private string _footerNote = string.Empty;
    private string _signaturePath = string.Empty;

    public SettingsViewModel()
    {
        _settingsService = App.GetService<ISettingsApiService>();
        _printService = App.GetService<IPrintApiService>();
        SetDialogService(App.GetService<IDialogService>());

        LoadCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadSettingsOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.LoadSettingsAsync", "[SettingsViewModel.LoadSettingsAsync] Failed to load system settings."))));
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveSettingsOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.SaveSettingsAsync", "[SettingsViewModel.SaveSettingsAsync] Unexpected error during save."))));
        BrowseLogoCommand = new RelayCommand(_ => BrowseLogo());
        TestPrintCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(TestPrintOperationAsync, ex => StatusMessage = HandleException(ex, "SettingsViewModel.TestPrintAsync", "[SettingsViewModel.TestPrintAsync] Test print failed."))));
        BrowseSignatureCommand = new RelayCommand(_ => BrowseSignature());
        ClearSignatureCommand = new RelayCommand(_ => SignaturePath = string.Empty);

        _ = ExecuteAsync(LoadSettingsOperationAsync);
    }

    #region Properties
    public string CompanyName
    {
        get => _companyName;
        set => SetProperty(ref _companyName, value);
    }

    public string? TaxNumber
    {
        get => _taxNumber;
        set => SetProperty(ref _taxNumber, value);
    }

    public string? Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    // DEPRECATED: DefaultTaxRate — use Tax entity instead. Remove in Phase 20.
    public decimal DefaultTaxRate
    {
        get => 0m;
        set { _defaultTaxRate = 0m; OnPropertyChanged(); }
    }

    // DEPRECATED: InvoicePrefix — use InvoiceNo (int) instead. Remove in Phase 20.
    public string InvoicePrefix
    {
        get => string.Empty;
        set { _invoicePrefix = string.Empty; OnPropertyChanged(); }
    }

    public bool EnableStockAlerts
    {
        get => _enableStockAlerts;
        set => SetProperty(ref _enableStockAlerts, value);
    }

    public bool AllowNegativeStock
    {
        get => _allowNegativeStock;
        set => SetProperty(ref _allowNegativeStock, value);
    }

    public bool AutoUpdatePrices
    {
        get => _autoUpdatePrices;
        set => SetProperty(ref _autoUpdatePrices, value);
    }

    #endregion

    #region Costing Method Properties
    public int CostingMethod
    {
        get => _costingMethod;
        set => SetProperty(ref _costingMethod, value);
    }

    public bool IsWeightedAverageSelected
    {
        get => _costingMethod == 1;
        set { if (value) CostingMethod = 1; }
    }

    public bool IsLastPriceSelected
    {
        get => _costingMethod == 2;
        set { if (value) CostingMethod = 2; }
    }

    public bool IsSupplierPriceSelected
    {
        get => _costingMethod == 3;
        set { if (value) CostingMethod = 3; }
    }
    #endregion

    #region Print Properties
    public string ThermalPrinterName
    {
        get => _thermalPrinterName;
        set => SetProperty(ref _thermalPrinterName, value);
    }

    public string A4PrinterName
    {
        get => _a4PrinterName;
        set => SetProperty(ref _a4PrinterName, value);
    }

    public string LogoPath
    {
        get => _logoPath;
        set => SetProperty(ref _logoPath, value);
    }

    public string StoreTaxNumber
    {
        get => _storeTaxNumber;
        set => SetProperty(ref _storeTaxNumber, value);
    }

    public decimal PrintTaxRate
    {
        get => _printTaxRate;
        set => SetProperty(ref _printTaxRate, value);
    }

    public string ReceiptHeader
    {
        get => _receiptHeader;
        set
        {
            if (SetProperty(ref _receiptHeader, value))
            {
                ValidateField(() => value?.Length <= 200, nameof(ReceiptHeader), "رأس الإيصال يجب ألا يتجاوز 200 حرف");
            }
        }
    }

    public string ReceiptFooter
    {
        get => _receiptFooter;
        set
        {
            if (SetProperty(ref _receiptFooter, value))
            {
                ValidateField(() => value?.Length <= 200, nameof(ReceiptFooter), "تذييل الإيصال يجب ألا يتجاوز 200 حرف");
            }
        }
    }

    public int EscPosCodePage
    {
        get => _escPosCodePage;
        set
        {
            if (SetProperty(ref _escPosCodePage, value))
            {
                ValidateField(() => value >= 0 && value <= 255, nameof(EscPosCodePage), "كود صفحة ESC/POS يجب أن يكون بين 0 و 255");
            }
        }
    }

    public bool AutoPrintOnPost
    {
        get => _autoPrintOnPost;
        set => SetProperty(ref _autoPrintOnPost, value);
    }

    public string PaperSize
    {
        get => _paperSize;
        set => SetProperty(ref _paperSize, value);
    }

    public int PrintCopies
    {
        get => _printCopies;
        set => SetProperty(ref _printCopies, value);
    }

    public bool ShowBalanceOnPrint
    {
        get => _showBalanceOnPrint;
        set => SetProperty(ref _showBalanceOnPrint, value);
    }

    public bool PrintSignature
    {
        get => _printSignature;
        set => SetProperty(ref _printSignature, value);
    }

    public bool ShowLogo
    {
        get => _showLogo;
        set => SetProperty(ref _showLogo, value);
    }

    public string FooterNote
    {
        get => _footerNote;
        set => SetProperty(ref _footerNote, value);
    }

    public List<string> InstalledPrinters { get; } =
        System.Drawing.Printing.PrinterSettings.InstalledPrinters
            .Cast<string>()
            .OrderBy(p => p)
            .ToList();
    #endregion

    public string SignaturePath
    {
        get => _signaturePath;
        set => SetProperty(ref _signaturePath, value);
    }

    #region Commands
    public AsyncRelayCommand LoadCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public ICommand BrowseLogoCommand { get; }
    public ICommand TestPrintCommand { get; }
    public ICommand BrowseSignatureCommand { get; }
    public ICommand ClearSignatureCommand { get; }
    #endregion

    #region Logic

    private void BrowseLogo()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "اختيار شعار المتجر",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            LogoPath = dialog.FileName;
            StatusMessage = "✅ تم اختيار الشعار";
        }
    }

    private void BrowseSignature()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "اختيار ملف التوقيع",
            Filter = "Image Files|*.png;*.jpg;*.jpeg",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            SignaturePath = dialog.FileName;
            StatusMessage = "✅ تم اختيار ملف التوقيع";
        }
    }

    private async Task TestPrintOperationAsync()
    {
        StatusMessage = string.Empty;

        var result = await _printService.TestPrintAsync();
        if (result.IsSuccess)
        {
            StatusMessage = "✅ تمت طباعة الاختبار بنجاح";
        }
        else
        {
            LogSystemError($"Print test failed: {result.Error}", "SettingsViewModel.TestPrintOperationAsync");
            StatusMessage = "فشلت طباعة الاختبار";
            await DialogService!.ShowErrorAsync("خطأ في الطباعة", "فشل اختبار الطباعة. يرجى التحقق من إعدادات الطابعة والمحاولة مرة أخرى.");
        }
    }

    private async Task LoadSettingsOperationAsync()
    {
        StatusMessage = string.Empty;

        var result = await _settingsService.GetSettingsAsync();
        if (result.IsSuccess && result.Value != null)
        {
            var s = result.Value;
            CompanyName = s.StoreName;
            Phone = s.Phone;
            Address = s.Address;
            TaxNumber = s.TaxNumber;
            Email = s.Email;
            DefaultTaxRate = s.DefaultTaxRate;
            EnableStockAlerts = s.EnableStockAlerts;
            AllowNegativeStock = s.AllowNegativeStock;
            AutoUpdatePrices = s.AutoUpdatePrices;
            InvoicePrefix = s.InvoicePrefix;
            CostingMethod = s.CostingMethod;
            SignaturePath = s.SignaturePath ?? string.Empty;
        }

        try
        {
            var printResult = await _settingsService.GetPrintSettingsAsync();
            if (printResult.IsSuccess && printResult.Value != null)
            {
                var p = printResult.Value;
                ThermalPrinterName = p.ThermalPrinterName;
                A4PrinterName = p.A4PrinterName;
                LogoPath = p.LogoPath;
                StoreTaxNumber = p.StoreTaxNumber;
                PrintTaxRate = p.TaxRate;
                ReceiptHeader = p.ReceiptHeader ?? string.Empty;
                ReceiptFooter = p.ReceiptFooter ?? string.Empty;
                EscPosCodePage = p.EscPosCodePage;
                AutoPrintOnPost = p.AutoPrintOnPost;
                PaperSize = p.PaperSize;
                PrintCopies = p.PrintCopies;
                ShowBalanceOnPrint = p.ShowBalanceOnPrint;
                PrintSignature = p.PrintSignature;
                ShowLogo = p.ShowLogo;
                FooterNote = p.FooterNote ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            LogSystemError("Failed to load print settings", "SettingsViewModel.LoadSettingsOperationAsync", ex);
        }
    }

    private async Task SaveSettingsOperationAsync()
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            var errors = new List<string>
            {
                "• اسم المنشأة مطلوب"
            };
            StatusMessage = "يرجى إدخال اسم المنشأة";
            string errorMsg = "يرجى إكمال البيانات الإلزامية التالية:\n\n" + string.Join("\n", errors);
            await DialogService!.ShowWarningAsync("بيانات غير مكتملة", errorMsg);
            return;
        }

        StatusMessage = string.Empty;

        // DEPRECATED: DefaultTaxRate — use Tax entity instead; DEPRECATED: InvoicePrefix — use InvoiceNo (int) instead. Remove in Phase 20.
        var request = new UpdateSettingsRequest(
            CompanyName,
            Address,
            Phone,
            Email,
            null,
            "SAR",
            0m,            // DefaultTaxRate — deprecated, always send 0
            true,          // IsTaxEnabled — deprecated, always send true
            TaxNumber,
            EnableStockAlerts,
            AllowNegativeStock,
            AutoUpdatePrices,
            string.Empty,  // InvoicePrefix — deprecated, always send empty
            CostingMethod,
            BackupPath: null,
            BackupScheduleTime: null,
            BackupRetentionDays: 30,
            UpdateServerUrl: null,
            SignatureUrl: SignaturePath
        );

        var result = await _settingsService.UpdateSettingsAsync(request);
        if (result.IsSuccess)
        {
            _settingsService.RefreshCache();

            var printRequest = new UpdatePrintSettingsRequest(
                ThermalPrinterName,
                A4PrinterName,
                LogoPath,
                StoreTaxNumber,
                PrintTaxRate,
                AutoPrintOnPost,
                ReceiptHeader,
                ReceiptFooter,
                EscPosCodePage,
                PaperSize: PaperSize,
                PrintCopies: PrintCopies,
                ShowBalanceOnPrint: ShowBalanceOnPrint,
                PrintSignature: PrintSignature,
                ShowLogo: ShowLogo,
                FooterNote: FooterNote);
            var printResult = await _settingsService.UpdatePrintSettingsAsync(printRequest);
            if (!printResult.IsSuccess)
            {
                StatusMessage = HandleFailure(printResult.Error ?? "فشل في حفظ إعدادات الطباعة",
                    "SettingsViewModel.SaveSettingsAsync");
                await DialogService!.ShowErrorAsync("خطأ في حفظ إعدادات الطباعة", StatusMessage);
                return;
            }

            // Publish settings changed event so other ViewModels can react
            App.GetService<IEventBus>().Publish(new StoreSettingsChangedMessage());

            StatusMessage = "✅ تم حفظ الإعدادات بنجاح";
            _ = Task.Delay(3000).ContinueWith(_ => StatusMessage = string.Empty);
        }
        else
        {
            StatusMessage = HandleFailure(result.Error ?? "فشل في حفظ الإعدادات", "SettingsViewModel.SaveSettingsAsync", "[SettingsViewModel.SaveSettingsAsync] Failed to update system settings.");
            await DialogService!.ShowErrorAsync("خطأ في الحفظ", StatusMessage);
        }
    }

    #endregion
}
