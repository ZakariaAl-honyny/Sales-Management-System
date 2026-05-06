namespace SalesSystem.Domain.Entities;

public class StoreSettings
{
    public int StoreSettingsId { get; private set; }
    public string StoreName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? LogoPath { get; private set; }
    public string CurrencyCode { get; private set; } = "SAR";
    public decimal DefaultTaxRate { get; private set; }
    public bool IsTaxEnabled { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private StoreSettings() { }

    public static StoreSettings Create(
        string storeName,
        string? phone = null,
        string? address = null,
        string? logoPath = null,
        string currencyCode = "SAR",
        decimal defaultTaxRate = 0,
        bool isTaxEnabled = false)
    {
        if (string.IsNullOrWhiteSpace(storeName))
            throw new ArgumentException("StoreName is required.", nameof(storeName));

        return new StoreSettings
        {
            StoreName = storeName,
            Phone = phone,
            Address = address,
            LogoPath = logoPath,
            CurrencyCode = currencyCode,
            DefaultTaxRate = defaultTaxRate,
            IsTaxEnabled = isTaxEnabled
        };
    }

    public void Update(
        string storeName,
        string? phone,
        string? address,
        string? logoPath,
        string currencyCode,
        decimal defaultTaxRate,
        bool isTaxEnabled)
    {
        StoreName = storeName;
        Phone = phone;
        Address = address;
        LogoPath = logoPath;
        CurrencyCode = currencyCode;
        DefaultTaxRate = defaultTaxRate;
        IsTaxEnabled = isTaxEnabled;
        UpdatedAt = DateTime.UtcNow;
    }
}