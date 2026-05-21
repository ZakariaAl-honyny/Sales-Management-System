using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class StoreSettingsTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateStoreSettings()
    {
        var settings = StoreSettings.Create(
            storeName: "My Store",
            phone: "1234567890",
            address: "123 Main Street",
            logoPath: "/images/logo.png",
            currencyCode: "USD",
            defaultTaxRate: 0.15m,
            isTaxEnabled: true
        );

        settings.StoreName.Should().Be("My Store");
        settings.Phone.Should().Be("1234567890");
        settings.Address.Should().Be("123 Main Street");
        settings.LogoPath.Should().Be("/images/logo.png");
        settings.CurrencyCode.Should().Be("USD");
        settings.DefaultTaxRate.Should().Be(0.15m);
        settings.IsTaxEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidStoreName_ShouldThrowDomainException(string? invalidName)
    {
        var action = () => StoreSettings.Create(storeName: invalidName!);

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المتجر مطلوب*");
    }

    [Fact]
    public void Create_GivenNoOptionalParameters_ShouldUseDefaults()
    {
        var settings = StoreSettings.Create(storeName: "Test Store");

        settings.Phone.Should().BeNull();
        settings.Address.Should().BeNull();
        settings.LogoPath.Should().BeNull();
        settings.CurrencyCode.Should().Be("SAR");
        settings.DefaultTaxRate.Should().Be(0);
        settings.IsTaxEnabled.Should().BeFalse();
    }

    [Fact]
    public void Create_GivenDefaultCurrency_ShouldBeSAR()
    {
        var settings = StoreSettings.Create(storeName: "Test Store");

        settings.CurrencyCode.Should().Be("SAR");
    }

    [Fact]
    public void Create_GivenZeroTaxRate_ShouldSucceed()
    {
        var settings = StoreSettings.Create(
            storeName: "Test Store",
            defaultTaxRate: 0m
        );

        settings.DefaultTaxRate.Should().Be(0m);
    }

    [Fact]
    public void Create_GivenNegativeTaxRate_ShouldThrowDomainException()
    {
        var action = () => StoreSettings.Create(
            storeName: "Test Store",
            defaultTaxRate: -0.05m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*معدل الضريبة الافتراضي لا يمكن أن يكون سالباً*");
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateSettings()
    {
        var settings = StoreSettings.Create(
            storeName: "Original Store",
            phone: "1111111111",
            address: "Old Address",
            currencyCode: "SAR",
            defaultTaxRate: 0.10m,
            isTaxEnabled: false
        );

        settings.Update(
            storeName: "Updated Store",
            phone: "2222222222",
            address: "New Address",
            logoPath: "/new/logo.png",
            email: "updated@store.com",
            currencyCode: "USD",
            defaultTaxRate: 0.15m,
            isTaxEnabled: true,
            taxNumber: "12345",
            enableStockAlerts: true,
            allowNegativeStock: false,
            autoUpdatePrices: true,
            invoicePrefix: "INV"
        );

        settings.StoreName.Should().Be("Updated Store");
        settings.Phone.Should().Be("2222222222");
        settings.Address.Should().Be("New Address");
        settings.LogoPath.Should().Be("/new/logo.png");
        settings.CurrencyCode.Should().Be("USD");
        settings.DefaultTaxRate.Should().Be(0.15m);
        settings.IsTaxEnabled.Should().BeTrue();
    }

    [Fact]
    public void Update_GivenNullOptional_ShouldClearFields()
    {
        var settings = StoreSettings.Create(
            storeName: "Test",
            phone: "1111111111",
            address: "Old Address",
            logoPath: "/old/logo.png"
        );

        settings.Update(
            storeName: "Test",
            phone: null,
            address: null,
            logoPath: null,
            email: null,
            currencyCode: "SAR",
            defaultTaxRate: 0,
            isTaxEnabled: false,
            taxNumber: null,
            enableStockAlerts: false,
            allowNegativeStock: false,
            autoUpdatePrices: false,
            invoicePrefix: "INV"
        );

        settings.Phone.Should().BeNull();
        settings.Address.Should().BeNull();
        settings.LogoPath.Should().BeNull();
    }

    [Fact]
    public void Create_GivenDifferentCurrencyCodes_ShouldAccept()
    {
        var currencies = new[] { "SAR", "USD", "EUR", "GBP", "AED" };

        foreach (var currency in currencies)
        {
            var settings = StoreSettings.Create(
                storeName: "Test",
                currencyCode: currency
            );

            settings.CurrencyCode.Should().Be(currency);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.05)]
    [InlineData(0.10)]
    [InlineData(0.15)]
    [InlineData(0.25)]
    public void Create_GivenVariousTaxRates_ShouldAccept(decimal taxRate)
    {
        var settings = StoreSettings.Create(
            storeName: "Test",
            defaultTaxRate: taxRate
        );

        settings.DefaultTaxRate.Should().Be(taxRate);
    }
}