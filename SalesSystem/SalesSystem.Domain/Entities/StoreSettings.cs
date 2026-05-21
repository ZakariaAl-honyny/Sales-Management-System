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
    public string CurrencyCode { get; private set; } = "SAR";
    public decimal DefaultTaxRate { get; private set; }
    public bool IsTaxEnabled { get; private set; }
    public string? TaxNumber { get; private set; }
    public bool EnableStockAlerts { get; private set; }
    public bool AllowNegativeStock { get; private set; }
    public bool AutoUpdatePrices { get; private set; }
    public string InvoicePrefix { get; private set; } = "INV";

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
        string invoicePrefix = "INV")
    {
        if (string.IsNullOrWhiteSpace(storeName))
            throw new DomainException("اسم المتجر مطلوب.");
        if (defaultTaxRate < 0)
            throw new DomainException("معدل الضريبة الافتراضي لا يمكن أن يكون سالباً.");

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
            InvoicePrefix = invoicePrefix
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
        string invoicePrefix)
    {
        if (string.IsNullOrWhiteSpace(storeName))
            throw new DomainException("اسم المتجر مطلوب.");
        if (defaultTaxRate < 0)
            throw new DomainException("معدل الضريبة الافتراضي لا يمكن أن يكون سالباً.");
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
        UpdateTimestamp();
    }
}