using System.Windows.Input;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Settings;

/// <summary>
/// ViewModel for the System Settings bulk-editing screen.
/// Loads all SystemSettings as a Dictionary and maps to strongly-typed properties,
/// grouped by category (Inventory, Sales, Purchases, Barcode, Accounting, General).
/// Opened via ScreenWindowService.OpenScreen() — non-modal.
/// </summary>
public class SystemSettingsViewModel : ViewModelBase
{
    private readonly ISettingsApiService _settingsApi;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private Dictionary<string, string> _originalSettings = new();

    public SystemSettingsViewModel(
        ISettingsApiService settingsApi,
        IDialogService dialogService,
        IEventBus eventBus)
    {
        _settingsApi = settingsApi;
        _dialogService = dialogService;
        _eventBus = eventBus;
        SetDialogService(dialogService);

        LoadCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadOperationAsync)));
        SaveCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync)));

        _ = LoadAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════

    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // ── Inventory (4) ──
    // ═══════════════════════════════════════════════════════════════

    private int _costingMethod = 1;
    public int CostingMethod
    {
        get => _costingMethod;
        set
        {
            if (SetProperty(ref _costingMethod, value))
            {
                OnPropertyChanged(nameof(IsWeightedAverageSelected));
                OnPropertyChanged(nameof(IsLastPurchasePriceSelected));
                OnPropertyChanged(nameof(IsSupplierPriceSelected));
            }
        }
    }

    public bool IsWeightedAverageSelected
    {
        get => _costingMethod == 1;
        set { if (value) CostingMethod = 1; }
    }

    public bool IsLastPurchasePriceSelected
    {
        get => _costingMethod == 2;
        set { if (value) CostingMethod = 2; }
    }

    public bool IsSupplierPriceSelected
    {
        get => _costingMethod == 3;
        set { if (value) CostingMethod = 3; }
    }

    private bool _allowNegativeStock;
    public bool AllowNegativeStock
    {
        get => _allowNegativeStock;
        set => SetProperty(ref _allowNegativeStock, value);
    }

    private bool _enableFefo;
    public bool EnableFefo
    {
        get => _enableFefo;
        set => SetProperty(ref _enableFefo, value);
    }

    private int _stockAlertDays = 5;
    public int StockAlertDays
    {
        get => _stockAlertDays;
        set => SetProperty(ref _stockAlertDays, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // ── Sales (8) ──
    // ═══════════════════════════════════════════════════════════════

    private bool _autoPostInvoices = true;
    public bool AutoPostInvoices
    {
        get => _autoPostInvoices;
        set => SetProperty(ref _autoPostInvoices, value);
    }

    private bool _allowDrafts = true;
    public bool AllowDrafts
    {
        get => _allowDrafts;
        set => SetProperty(ref _allowDrafts, value);
    }

    private bool _showProfitInInvoice = true;
    public bool ShowProfitInInvoice
    {
        get => _showProfitInInvoice;
        set => SetProperty(ref _showProfitInInvoice, value);
    }

    private bool _preventBelowRetailPrice;
    public bool PreventBelowRetailPrice
    {
        get => _preventBelowRetailPrice;
        set => SetProperty(ref _preventBelowRetailPrice, value);
    }

    private bool _allowBelowCostSale;
    public bool AllowBelowCostSale
    {
        get => _allowBelowCostSale;
        set => SetProperty(ref _allowBelowCostSale, value);
    }

    private int _defaultCashCustomerId = 1;
    public int DefaultCashCustomerId
    {
        get => _defaultCashCustomerId;
        set => SetProperty(ref _defaultCashCustomerId, value);
    }

    private bool _hideTaxInSales;
    public bool HideTaxInSales
    {
        get => _hideTaxInSales;
        set => SetProperty(ref _hideTaxInSales, value);
    }

    private bool _showExpiryInInvoices = true;
    public bool ShowExpiryInInvoices
    {
        get => _showExpiryInInvoices;
        set => SetProperty(ref _showExpiryInInvoices, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // ── Purchases (3) ──
    // ═══════════════════════════════════════════════════════════════

    private bool _purchaseAutoPost = true;
    public bool PurchaseAutoPost
    {
        get => _purchaseAutoPost;
        set => SetProperty(ref _purchaseAutoPost, value);
    }

    private int _defaultCashSupplierId = 1;
    public int DefaultCashSupplierId
    {
        get => _defaultCashSupplierId;
        set => SetProperty(ref _defaultCashSupplierId, value);
    }

    private bool _hideTaxInPurchases;
    public bool HideTaxInPurchases
    {
        get => _hideTaxInPurchases;
        set => SetProperty(ref _hideTaxInPurchases, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // ── Barcode (3) ──
    // ═══════════════════════════════════════════════════════════════

    private bool _enableBarcode = true;
    public bool EnableBarcode
    {
        get => _enableBarcode;
        set => SetProperty(ref _enableBarcode, value);
    }

    private string _barcodeInputType = "Scanner";
    public string BarcodeInputType
    {
        get => _barcodeInputType;
        set => SetProperty(ref _barcodeInputType, value);
    }

    private bool _autoGenerateBarcode = true;
    public bool AutoGenerateBarcode
    {
        get => _autoGenerateBarcode;
        set => SetProperty(ref _autoGenerateBarcode, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // ── Print (6) ──
    // ═══════════════════════════════════════════════════════════════

    private string _thermalPrinterName = string.Empty;
    public string ThermalPrinterName
    {
        get => _thermalPrinterName;
        set => SetProperty(ref _thermalPrinterName, value);
    }

    private string _a4PrinterName = string.Empty;
    public string A4PrinterName
    {
        get => _a4PrinterName;
        set => SetProperty(ref _a4PrinterName, value);
    }

    private string _logoPath = string.Empty;
    public string LogoPath
    {
        get => _logoPath;
        set => SetProperty(ref _logoPath, value);
    }

    private string _storeTaxNumber = string.Empty;
    public string StoreTaxNumber
    {
        get => _storeTaxNumber;
        set => SetProperty(ref _storeTaxNumber, value);
    }

    private bool _showLogo;
    public bool ShowLogo
    {
        get => _showLogo;
        set => SetProperty(ref _showLogo, value);
    }

    private string _footerNote = string.Empty;
    public string FooterNote
    {
        get => _footerNote;
        set => SetProperty(ref _footerNote, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // ── Notifications (4) ──
    // ═══════════════════════════════════════════════════════════════

    private bool _lowStockAlert = true;
    public bool LowStockAlert
    {
        get => _lowStockAlert;
        set => SetProperty(ref _lowStockAlert, value);
    }

    private bool _expiryAlert = true;
    public bool ExpiryAlert
    {
        get => _expiryAlert;
        set => SetProperty(ref _expiryAlert, value);
    }

    private int _expiryAlertDays = 30;
    public int ExpiryAlertDays
    {
        get => _expiryAlertDays;
        set => SetProperty(ref _expiryAlertDays, value);
    }

    private bool _creditLimitAlert = true;
    public bool CreditLimitAlert
    {
        get => _creditLimitAlert;
        set => SetProperty(ref _creditLimitAlert, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // ── Accounting (1) ──
    // ═══════════════════════════════════════════════════════════════

    private bool _autoCreateJournalEntry = true;
    public bool AutoCreateJournalEntry
    {
        get => _autoCreateJournalEntry;
        set => SetProperty(ref _autoCreateJournalEntry, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // ── General (3) ──
    // ═══════════════════════════════════════════════════════════════

    private int _decimalPlaces = 2;
    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set => SetProperty(ref _decimalPlaces, value);
    }

    private string _language = "ar";
    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    private string _dateFormat = "dd/MM/yyyy";
    public string DateFormat
    {
        get => _dateFormat;
        set => SetProperty(ref _dateFormat, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // Barcode Input Types for ComboBox
    // ═══════════════════════════════════════════════════════════════

    public List<string> BarcodeInputTypes { get; } = new() { "Scanner", "Camera" };

    // ═══════════════════════════════════════════════════════════════
    // Decimal Places options for ComboBox
    // ═══════════════════════════════════════════════════════════════

    public List<int> DecimalPlacesOptions { get; } = new() { 0, 1, 2, 3, 4, 5, 6 };

    // ═══════════════════════════════════════════════════════════════
    // Language options for ComboBox
    // ═══════════════════════════════════════════════════════════════

    public List<string> LanguageOptions { get; } = new() { "ar", "en" };

    // ═══════════════════════════════════════════════════════════════
    // Operations
    // ═══════════════════════════════════════════════════════════════

    private async Task LoadAsync()
    {
        await ExecuteAsync(LoadOperationAsync);
    }

    private async Task LoadOperationAsync()
    {
        StatusMessage = string.Empty;
        var result = await _settingsApi.GetAllSystemSettingsAsync();

        if (!result.IsSuccess || result.Value == null)
        {
            StatusMessage = HandleFailure(result.Error ?? "فشل تحميل الإعدادات", "SystemSettingsViewModel.Load");
            return;
        }

        _originalSettings = new Dictionary<string, string>(result.Value);
        MapFromDictionary(result.Value);
        StatusMessage = "✅ تم تحميل الإعدادات";
    }

    private async Task SaveOperationAsync()
    {
        StatusMessage = string.Empty;

        var settings = BuildDictionary();

        var result = await _settingsApi.UpdateSystemSettingsAsync(settings);
        if (result.IsSuccess)
        {
            _originalSettings = new Dictionary<string, string>(settings);
            _settingsApi.RefreshCache();
            _eventBus.Publish(new StoreSettingsChangedMessage());

            await _dialogService.ShowSuccessAsync("تم", "تم حفظ إعدادات النظام بنجاح");
            StatusMessage = "✅ تم حفظ الإعدادات بنجاح";
        }
        else
        {
            LogSystemError($"Failed to save system settings: {result.Error}", "SystemSettingsViewModel.Save");
            StatusMessage = "فشل حفظ الإعدادات";
            await _dialogService.ShowErrorAsync("خطأ في حفظ الإعدادات",
                "حدث خطأ غير متوقع أثناء حفظ إعدادات النظام. يرجى المحاولة مرة أخرى.");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Dictionary Mapping
    // ═══════════════════════════════════════════════════════════════

    private void MapFromDictionary(Dictionary<string, string> settings)
    {
        // Inventory
        CostingMethod = ParseInt(settings, "CostingMethod", 1);
        AllowNegativeStock = ParseBool(settings, "AllowNegativeStock");
        EnableFefo = ParseBool(settings, "EnableFefo");
        StockAlertDays = ParseInt(settings, "StockAlertDays", 5);

        // Sales
        AutoPostInvoices = ParseBool(settings, "AutoPostInvoices", true);
        AllowDrafts = ParseBool(settings, "AllowDrafts", true);
        ShowProfitInInvoice = ParseBool(settings, "ShowProfitInInvoice", true);
        PreventBelowRetailPrice = ParseBool(settings, "PreventBelowRetailPrice");
        AllowBelowCostSale = ParseBool(settings, "AllowBelowCostSale");
        DefaultCashCustomerId = ParseInt(settings, "DefaultCashCustomerId", 1);
        HideTaxInSales = ParseBool(settings, "HideTaxInSales");
        ShowExpiryInInvoices = ParseBool(settings, "ShowExpiryInInvoices", true);

        // Purchases
        PurchaseAutoPost = ParseBool(settings, "PurchaseAutoPost", true);
        DefaultCashSupplierId = ParseInt(settings, "DefaultCashSupplierId", 1);
        HideTaxInPurchases = ParseBool(settings, "HideTaxInPurchases");

        // Barcode
        EnableBarcode = ParseBool(settings, "EnableBarcode", true);
        BarcodeInputType = settings.GetValueOrDefault("BarcodeInputType", "Scanner");
        AutoGenerateBarcode = ParseBool(settings, "AutoGenerateBarcode", true);

        // Print
        ThermalPrinterName = settings.GetValueOrDefault("ThermalPrinterName", "");
        A4PrinterName = settings.GetValueOrDefault("A4PrinterName", "");
        LogoPath = settings.GetValueOrDefault("LogoPath", "");
        StoreTaxNumber = settings.GetValueOrDefault("StoreTaxNumber", "");
        ShowLogo = ParseBool(settings, "ShowLogo");
        FooterNote = settings.GetValueOrDefault("FooterNote", "");

        // Notifications
        LowStockAlert = ParseBool(settings, "LowStockAlert", true);
        ExpiryAlert = ParseBool(settings, "ExpiryAlert", true);
        ExpiryAlertDays = ParseInt(settings, "ExpiryAlertDays", 30);
        CreditLimitAlert = ParseBool(settings, "CreditLimitAlert", true);

        // Accounting
        AutoCreateJournalEntry = ParseBool(settings, "AutoCreateJournalEntry", true);

        // General
        DecimalPlaces = ParseInt(settings, "DecimalPlaces", 2);
        Language = settings.GetValueOrDefault("Language", "ar");
        DateFormat = settings.GetValueOrDefault("DateFormat", "dd/MM/yyyy");
    }

    private Dictionary<string, string> BuildDictionary()
    {
        var dict = new Dictionary<string, string>
        {
            // Inventory
            ["CostingMethod"] = CostingMethod.ToString(),
            ["AllowNegativeStock"] = AllowNegativeStock.ToString().ToLower(),
            ["EnableFefo"] = EnableFefo.ToString().ToLower(),
            ["StockAlertDays"] = StockAlertDays.ToString(),

            // Sales
            ["AutoPostInvoices"] = AutoPostInvoices.ToString().ToLower(),
            ["AllowDrafts"] = AllowDrafts.ToString().ToLower(),
            ["ShowProfitInInvoice"] = ShowProfitInInvoice.ToString().ToLower(),
            ["PreventBelowRetailPrice"] = PreventBelowRetailPrice.ToString().ToLower(),
            ["AllowBelowCostSale"] = AllowBelowCostSale.ToString().ToLower(),
            ["DefaultCashCustomerId"] = DefaultCashCustomerId.ToString(),
            ["HideTaxInSales"] = HideTaxInSales.ToString().ToLower(),
            ["ShowExpiryInInvoices"] = ShowExpiryInInvoices.ToString().ToLower(),

            // Purchases
            ["PurchaseAutoPost"] = PurchaseAutoPost.ToString().ToLower(),
            ["DefaultCashSupplierId"] = DefaultCashSupplierId.ToString(),
            ["HideTaxInPurchases"] = HideTaxInPurchases.ToString().ToLower(),

            // Barcode
            ["EnableBarcode"] = EnableBarcode.ToString().ToLower(),
            ["BarcodeInputType"] = BarcodeInputType,
            ["AutoGenerateBarcode"] = AutoGenerateBarcode.ToString().ToLower(),

            // Print
            ["ThermalPrinterName"] = ThermalPrinterName,
            ["A4PrinterName"] = A4PrinterName,
            ["LogoPath"] = LogoPath,
            ["StoreTaxNumber"] = StoreTaxNumber,
            ["ShowLogo"] = ShowLogo.ToString().ToLower(),
            ["FooterNote"] = FooterNote,

            // Notifications
            ["LowStockAlert"] = LowStockAlert.ToString().ToLower(),
            ["ExpiryAlert"] = ExpiryAlert.ToString().ToLower(),
            ["ExpiryAlertDays"] = ExpiryAlertDays.ToString(),
            ["CreditLimitAlert"] = CreditLimitAlert.ToString().ToLower(),

            // Accounting
            ["AutoCreateJournalEntry"] = AutoCreateJournalEntry.ToString().ToLower(),

            // General
            ["DecimalPlaces"] = DecimalPlaces.ToString(),
            ["Language"] = Language,
            ["DateFormat"] = DateFormat,
        };

        return dict;
    }

    private static bool ParseBool(Dictionary<string, string> settings, string key, bool defaultValue = false)
    {
        if (settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var result))
            return result;
        return defaultValue;
    }

    private static int ParseInt(Dictionary<string, string> settings, string key, int defaultValue = 0)
    {
        if (settings.TryGetValue(key, out var raw) && int.TryParse(raw, out var result))
            return result;
        return defaultValue;
    }
}
