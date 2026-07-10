namespace SalesSystem.Contracts.Requests;

public record UpdateCompanySettingsRequest(
    string CompanyName,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? TaxNumber = null,
    string? LogoPath = null);

public record UpdateDocumentSequenceRequest(
    int Id,
    int NextNumber);

public record RestoreBackupRequest(string FileName);

public record ReportFilterRequest(
    DateTime? DateFrom, DateTime? DateTo,
    int? CustomerId, int? SupplierId,
    int? WarehouseId, int? ProductId
);

public record UpdateSettingsRequest(
    string StoreName, string? Address, string? Phone, string? Email,
    string? LogoUrl, decimal DefaultTaxRate, // DEPRECATED: DefaultTaxRate — use Tax entity instead (kept for backwards compat). Remove in Phase 20.
    bool IsTaxEnabled, string? TaxNumber, // DEPRECATED: IsTaxEnabled — use Tax entity instead (kept for backwards compat). Remove in Phase 20.
    bool EnableStockAlerts, bool AllowNegativeStock,
    string InvoicePrefix, // DEPRECATED: InvoicePrefix — use InvoiceNo (int) instead. Remove in Phase 20.
    string? BackupPath = null,
    string? BackupScheduleTime = null,
    int BackupRetentionDays = 30,
    string? UpdateServerUrl = null,
    string? SignatureUrl = null);

public record UpdatePrintSettingsRequest(
    string ThermalPrinterName,
    string A4PrinterName,
    string LogoPath,
    string StoreTaxNumber,
    decimal TaxRate,
    bool AutoPrintOnPost,
    string ReceiptHeader,
    string ReceiptFooter,
    int EscPosCodePage = 22,
    string PaperSize = "A4",
    int PrintCopies = 1,
    bool ShowBalanceOnPrint = true,
    bool PrintSignature = false,
    bool ShowLogo = true,
    string FooterNote = "",
    bool PrintBarcode = false,
    bool PrintQRCode = false,
    bool PrintCompanyAddress = true);
