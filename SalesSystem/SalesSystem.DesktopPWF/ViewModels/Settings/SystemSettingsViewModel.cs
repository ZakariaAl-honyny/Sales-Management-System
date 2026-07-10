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
    // ── Inventory (5) ──
    // ═══════════════════════════════════════════════════════════════

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

    private bool _requireBatchOnPurchase;
    public bool RequireBatchOnPurchase
    {
        get => _requireBatchOnPurchase;
        set => SetProperty(ref _requireBatchOnPurchase, value);
    }

    private bool _requireExpiryOnPurchase;
    public bool RequireExpiryOnPurchase
    {
        get => _requireExpiryOnPurchase;
        set => SetProperty(ref _requireExpiryOnPurchase, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // ── Sales (9) ──
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

    private bool _autoPrintAfterPosting;
    public bool AutoPrintAfterPosting
    {
        get => _autoPrintAfterPosting;
        set => SetProperty(ref _autoPrintAfterPosting, value);
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
    // ── Barcode (2) ──
    // ═══════════════════════════════════════════════════════════════

    private bool _autoGenerateBarcode = true;
    public bool AutoGenerateBarcode
    {
        get => _autoGenerateBarcode;
        set => SetProperty(ref _autoGenerateBarcode, value);
    }

    private bool _allowDuplicateBarcode;
    public bool AllowDuplicateBarcode
    {
        get => _allowDuplicateBarcode;
        set => SetProperty(ref _allowDuplicateBarcode, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // ── Print (13) ──
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

    private int _printCopies = 1;
    public int PrintCopies
    {
        get => _printCopies;
        set => SetProperty(ref _printCopies, value);
    }

    private bool _showBalanceOnPrint;
    public bool ShowBalanceOnPrint
    {
        get => _showBalanceOnPrint;
        set => SetProperty(ref _showBalanceOnPrint, value);
    }

    private bool _printSignature;
    public bool PrintSignature
    {
        get => _printSignature;
        set => SetProperty(ref _printSignature, value);
    }

    private string _paperSize = "A4";
    public string PaperSize
    {
        get => _paperSize;
        set => SetProperty(ref _paperSize, value);
    }

    private bool _printBarcode;
    public bool PrintBarcode
    {
        get => _printBarcode;
        set => SetProperty(ref _printBarcode, value);
    }

    private bool _printQRCode;
    public bool PrintQRCode
    {
        get => _printQRCode;
        set => SetProperty(ref _printQRCode, value);
    }

    private bool _printCompanyAddress = true;
    public bool PrintCompanyAddress
    {
        get => _printCompanyAddress;
        set => SetProperty(ref _printCompanyAddress, value);
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
    // ── CashBox (1) ──
    // ═══════════════════════════════════════════════════════════════

    private bool _allowNegativeCash;
    public bool AllowNegativeCash
    {
        get => _allowNegativeCash;
        set => SetProperty(ref _allowNegativeCash, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // ── General (6) ──
    // ═══════════════════════════════════════════════════════════════

    private bool _enableAttachments = true;
    public bool EnableAttachments
    {
        get => _enableAttachments;
        set => SetProperty(ref _enableAttachments, value);
    }

    private bool _enableNotifications = true;
    public bool EnableNotifications
    {
        get => _enableNotifications;
        set => SetProperty(ref _enableNotifications, value);
    }

    private int _defaultSalesTax;
    public int DefaultSalesTax
    {
        get => _defaultSalesTax;
        set => SetProperty(ref _defaultSalesTax, value);
    }

    private int _defaultPurchaseTax;
    public int DefaultPurchaseTax
    {
        get => _defaultPurchaseTax;
        set => SetProperty(ref _defaultPurchaseTax, value);
    }

    private int _defaultBranch = 1;
    public int DefaultBranch
    {
        get => _defaultBranch;
        set => SetProperty(ref _defaultBranch, value);
    }

    private int _defaultWarehouse = 1;
    public int DefaultWarehouse
    {
        get => _defaultWarehouse;
        set => SetProperty(ref _defaultWarehouse, value);
    }

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
        AllowNegativeStock = ParseBool(settings, "AllowNegativeStock");
        EnableFefo = ParseBool(settings, "EnableFefo");
        StockAlertDays = ParseInt(settings, "StockAlertDays", 5);
        RequireBatchOnPurchase = ParseBool(settings, "RequireBatchOnPurchase");
        RequireExpiryOnPurchase = ParseBool(settings, "RequireExpiryOnPurchase");

        // Sales
        AutoPostInvoices = ParseBool(settings, "AutoPostInvoices", true);
        AllowDrafts = ParseBool(settings, "AllowDrafts", true);
        ShowProfitInInvoice = ParseBool(settings, "ShowProfitInInvoice", true);
        PreventBelowRetailPrice = ParseBool(settings, "PreventBelowRetailPrice");
        AllowBelowCostSale = ParseBool(settings, "AllowBelowCostSale");
        DefaultCashCustomerId = ParseInt(settings, "DefaultCashCustomerId", 1);
        HideTaxInSales = ParseBool(settings, "HideTaxInSales");
        ShowExpiryInInvoices = ParseBool(settings, "ShowExpiryInInvoices", true);
        AutoPrintAfterPosting = ParseBool(settings, "AutoPrintAfterPosting");

        // Purchases
        PurchaseAutoPost = ParseBool(settings, "PurchaseAutoPost", true);
        DefaultCashSupplierId = ParseInt(settings, "DefaultCashSupplierId", 1);
        HideTaxInPurchases = ParseBool(settings, "HideTaxInPurchases");

        // Barcode
        AutoGenerateBarcode = ParseBool(settings, "AutoGenerateBarcode", true);
        AllowDuplicateBarcode = ParseBool(settings, "AllowDuplicateBarcode");

        // Print
        ThermalPrinterName = settings.GetValueOrDefault("ThermalPrinterName", "");
        A4PrinterName = settings.GetValueOrDefault("A4PrinterName", "");
        LogoPath = settings.GetValueOrDefault("LogoPath", "");
        StoreTaxNumber = settings.GetValueOrDefault("StoreTaxNumber", "");
        ShowLogo = ParseBool(settings, "ShowLogo");
        FooterNote = settings.GetValueOrDefault("FooterNote", "");
        PrintCopies = ParseInt(settings, "PrintCopies", 1);
        ShowBalanceOnPrint = ParseBool(settings, "ShowBalanceOnPrint");
        PrintSignature = ParseBool(settings, "PrintSignature");
        PaperSize = settings.GetValueOrDefault("PaperSize", "A4");
        PrintBarcode = ParseBool(settings, "PrintBarcode");
        PrintQRCode = ParseBool(settings, "PrintQRCode");
        PrintCompanyAddress = ParseBool(settings, "PrintCompanyAddress", true);

        // Notifications
        LowStockAlert = ParseBool(settings, "LowStockAlert", true);
        ExpiryAlert = ParseBool(settings, "ExpiryAlert", true);
        ExpiryAlertDays = ParseInt(settings, "ExpiryAlertDays", 30);
        CreditLimitAlert = ParseBool(settings, "CreditLimitAlert", true);

        // Accounting
        AutoCreateJournalEntry = ParseBool(settings, "AutoCreateJournalEntry", true);

        // CashBox
        AllowNegativeCash = ParseBool(settings, "AllowNegativeCash");

        // General / Invoice Defaults
        EnableAttachments = ParseBool(settings, "EnableAttachments", true);
        EnableNotifications = ParseBool(settings, "EnableNotifications", true);
        DefaultSalesTax = ParseInt(settings, "DefaultSalesTax");
        DefaultPurchaseTax = ParseInt(settings, "DefaultPurchaseTax");
        DefaultBranch = ParseInt(settings, "DefaultBranch", 1);
        DefaultWarehouse = ParseInt(settings, "DefaultWarehouse", 1);
    }

    private Dictionary<string, string> BuildDictionary()
    {
        var dict = new Dictionary<string, string>
        {
            // Inventory
            ["AllowNegativeStock"] = AllowNegativeStock.ToString().ToLower(),
            ["EnableFefo"] = EnableFefo.ToString().ToLower(),
            ["StockAlertDays"] = StockAlertDays.ToString(),
            ["RequireBatchOnPurchase"] = RequireBatchOnPurchase.ToString().ToLower(),
            ["RequireExpiryOnPurchase"] = RequireExpiryOnPurchase.ToString().ToLower(),

            // Sales
            ["AutoPostInvoices"] = AutoPostInvoices.ToString().ToLower(),
            ["AllowDrafts"] = AllowDrafts.ToString().ToLower(),
            ["ShowProfitInInvoice"] = ShowProfitInInvoice.ToString().ToLower(),
            ["PreventBelowRetailPrice"] = PreventBelowRetailPrice.ToString().ToLower(),
            ["AllowBelowCostSale"] = AllowBelowCostSale.ToString().ToLower(),
            ["DefaultCashCustomerId"] = DefaultCashCustomerId.ToString(),
            ["HideTaxInSales"] = HideTaxInSales.ToString().ToLower(),
            ["ShowExpiryInInvoices"] = ShowExpiryInInvoices.ToString().ToLower(),
            ["AutoPrintAfterPosting"] = AutoPrintAfterPosting.ToString().ToLower(),

            // Purchases
            ["PurchaseAutoPost"] = PurchaseAutoPost.ToString().ToLower(),
            ["DefaultCashSupplierId"] = DefaultCashSupplierId.ToString(),
            ["HideTaxInPurchases"] = HideTaxInPurchases.ToString().ToLower(),

            // Barcode
            ["AutoGenerateBarcode"] = AutoGenerateBarcode.ToString().ToLower(),
            ["AllowDuplicateBarcode"] = AllowDuplicateBarcode.ToString().ToLower(),

            // Print
            ["ThermalPrinterName"] = ThermalPrinterName,
            ["A4PrinterName"] = A4PrinterName,
            ["LogoPath"] = LogoPath,
            ["StoreTaxNumber"] = StoreTaxNumber,
            ["ShowLogo"] = ShowLogo.ToString().ToLower(),
            ["FooterNote"] = FooterNote,
            ["PrintCopies"] = PrintCopies.ToString(),
            ["ShowBalanceOnPrint"] = ShowBalanceOnPrint.ToString().ToLower(),
            ["PrintSignature"] = PrintSignature.ToString().ToLower(),
            ["PaperSize"] = PaperSize,
            ["PrintBarcode"] = PrintBarcode.ToString().ToLower(),
            ["PrintQRCode"] = PrintQRCode.ToString().ToLower(),
            ["PrintCompanyAddress"] = PrintCompanyAddress.ToString().ToLower(),

            // Notifications
            ["LowStockAlert"] = LowStockAlert.ToString().ToLower(),
            ["ExpiryAlert"] = ExpiryAlert.ToString().ToLower(),
            ["ExpiryAlertDays"] = ExpiryAlertDays.ToString(),
            ["CreditLimitAlert"] = CreditLimitAlert.ToString().ToLower(),

            // Accounting
            ["AutoCreateJournalEntry"] = AutoCreateJournalEntry.ToString().ToLower(),

            // CashBox
            ["AllowNegativeCash"] = AllowNegativeCash.ToString().ToLower(),

            // General / Invoice Defaults
            ["EnableAttachments"] = EnableAttachments.ToString().ToLower(),
            ["EnableNotifications"] = EnableNotifications.ToString().ToLower(),
            ["DefaultSalesTax"] = DefaultSalesTax.ToString(),
            ["DefaultPurchaseTax"] = DefaultPurchaseTax.ToString(),
            ["DefaultBranch"] = DefaultBranch.ToString(),
            ["DefaultWarehouse"] = DefaultWarehouse.ToString(),
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
