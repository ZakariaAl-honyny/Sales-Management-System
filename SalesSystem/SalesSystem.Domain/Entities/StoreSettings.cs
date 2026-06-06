using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class StoreSettings : BaseEntity
{
    public string StoreName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? LogoPath { get; private set; }
    public string? Email { get; private set; }
    public string CurrencyCode { get; private set; } = "SAR"; // DEPRECATED: CurrencyCode — use Currencies table instead. Kept in DB for backwards compat. Remove in Phase 20.
    public decimal DefaultTaxRate { get; private set; } // DEPRECATED: DefaultTaxRate — use Tax entity instead. Remove in Phase 20.
    public bool IsTaxEnabled { get; private set; }      // DEPRECATED: IsTaxEnabled — use Tax entity instead. Remove in Phase 20.
    public string? TaxNumber { get; private set; }
    public bool EnableStockAlerts { get; private set; }
    public bool AllowNegativeStock { get; private set; }
    public bool AutoUpdatePrices { get; private set; }
    public string InvoicePrefix { get; private set; } = "INV"; // DEPRECATED: InvoicePrefix — use InvoiceNo (int) instead. Remove in Phase 20.

    /// <summary>
    /// File path to the company signature image printed on invoices.
    /// </summary>
    public string? SignaturePath { get; private set; }

    private StoreSettings() { }

    public static StoreSettings Create(
        string storeName,
        string? phone = null,
        string? address = null,
        string? logoPath = null,
        string? email = null,
        string currencyCode = "SAR",
        decimal defaultTaxRate = 0,
        bool isTaxEnabled = false,
        string? taxNumber = null,
        bool enableStockAlerts = true,
        bool allowNegativeStock = false,
        bool autoUpdatePrices = false,
        string invoicePrefix = "INV",
        string? signaturePath = null)
    {
        if (string.IsNullOrWhiteSpace(storeName))
            throw new DomainException("اسم المتجر مطلوب.");
        if (defaultTaxRate < 0)
            throw new DomainException("معدل الضريبة الافتراضي لا يمكن أن يكون سالباً.");
        if (signaturePath != null && signaturePath.Length > 255)
            throw new DomainException("مسار التوقيع طويل جداً");

        return new StoreSettings
        {
            StoreName = storeName,
            Phone = phone,
            Address = address,
            LogoPath = logoPath,
            Email = email,
            CurrencyCode = currencyCode,
            DefaultTaxRate = defaultTaxRate,
            IsTaxEnabled = isTaxEnabled,
            TaxNumber = taxNumber,
            EnableStockAlerts = enableStockAlerts,
            AllowNegativeStock = allowNegativeStock,
            AutoUpdatePrices = autoUpdatePrices,
            InvoicePrefix = invoicePrefix,
            SignaturePath = signaturePath
        };
    }

    public void Update(
        string storeName,
        string? phone,
        string? address,
        string? logoPath,
        string? email,
        string currencyCode,
        decimal defaultTaxRate,
        bool isTaxEnabled,
        string? taxNumber,
        bool enableStockAlerts,
        bool allowNegativeStock,
        bool autoUpdatePrices,
        string invoicePrefix,
        string? signaturePath = null)
    {
        if (string.IsNullOrWhiteSpace(storeName))
            throw new DomainException("اسم المتجر مطلوب.");
        if (defaultTaxRate < 0)
            throw new DomainException("معدل الضريبة الافتراضي لا يمكن أن يكون سالباً.");
        if (signaturePath != null && signaturePath.Length > 255)
            throw new DomainException("مسار التوقيع طويل جداً");
        StoreName = storeName;
        Phone = phone;
        Address = address;
        LogoPath = logoPath;
        Email = email;
        CurrencyCode = currencyCode;
        DefaultTaxRate = defaultTaxRate;
        IsTaxEnabled = isTaxEnabled;
        TaxNumber = taxNumber;
        EnableStockAlerts = enableStockAlerts;
        AllowNegativeStock = allowNegativeStock;
        AutoUpdatePrices = autoUpdatePrices;
        InvoicePrefix = invoicePrefix;
        SignaturePath = signaturePath;
        UpdateTimestamp();
    }
}