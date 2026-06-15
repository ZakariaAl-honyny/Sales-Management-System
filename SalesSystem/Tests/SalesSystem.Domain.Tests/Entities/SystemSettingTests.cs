using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class SystemSettingTests
{
    [Fact]
    public void Create_GivenValidKeyAndValue_ShouldSetProperties()
    {
        var setting = SystemSetting.Create(
            settingKey: "StoreName",
            settingValue: "My Store",
            settingType: 1,
            category: "General",
            displayName: "اسم المتجر",
            description: "اسم المتجر الظاهر في الفواتير"
        );

        setting.SettingKey.Should().Be("StoreName");
        setting.SettingValue.Should().Be("My Store");
        setting.SettingType.Should().Be((byte)1);
        setting.Category.Should().Be("General");
        setting.DisplayName.Should().Be("اسم المتجر");
        setting.Description.Should().Be("اسم المتجر الظاهر في الفواتير");
    }

    [Fact]
    public void Create_GivenDefaultParameters_ShouldUseDefaults()
    {
        var setting = SystemSetting.Create(
            settingKey: "DefaultTaxRate",
            settingValue: "0.15"
        );

        setting.SettingType.Should().Be((byte)1);
        setting.Category.Should().Be("General");
        setting.DisplayName.Should().Be("");
        setting.Description.Should().BeNull();
    }

    [Fact]
    public void Create_GivenDifferentSettingType_ShouldStoreCorrectly()
    {
        var setting = SystemSetting.Create(
            settingKey: "TaxRate",
            settingValue: "0.15",
            settingType: 3
        );

        setting.SettingType.Should().Be((byte)3);
    }

    [Fact]
    public void Create_GivenDifferentCategory_ShouldStoreCorrectly()
    {
        var setting = SystemSetting.Create(
            settingKey: "SmtpHost",
            settingValue: "smtp.example.com",
            category: "Email"
        );

        setting.Category.Should().Be("Email");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenEmptyKey_ShouldThrowDomainException(string? invalidKey)
    {
        var action = () => SystemSetting.Create(settingKey: invalidKey!, settingValue: "value");

        action.Should().Throw<DomainException>()
            .WithMessage("*مفتاح الإعداد مطلوب*");
    }

    [Fact]
    public void Create_GivenKeyWithWhitespace_ShouldTrimKey()
    {
        var setting = SystemSetting.Create(
            settingKey: "  TaxRate  ",
            settingValue: "0.15"
        );

        setting.SettingKey.Should().Be("TaxRate");
    }

    [Fact]
    public void Create_GivenEmptyValue_ShouldSucceed()
    {
        var setting = SystemSetting.Create(
            settingKey: "AllowNegativeStock",
            settingValue: ""
        );

        setting.SettingValue.Should().Be("");
    }

    [Fact]
    public void Create_SetsUpdatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;
        var setting = SystemSetting.Create("Key", "Value");
        var after = DateTime.UtcNow;

        setting.UpdatedAt.Should().BeOnOrAfter(before);
        setting.UpdatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void UpdateValue_GivenNewValue_ShouldChangeSettingValue()
    {
        var setting = SystemSetting.Create("TaxRate", "0.15");

        setting.UpdateValue("0.20");

        setting.SettingValue.Should().Be("0.20");
    }

    [Fact]
    public void UpdateValue_GivenNewValue_ShouldUpdateTimestamp()
    {
        var setting = SystemSetting.Create("TaxRate", "0.15");
        var beforeUpdate = DateTime.UtcNow;

        setting.UpdateValue("0.20");

        setting.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public void UpdateValue_GivenUpdatedByUserId_ShouldSetUpdatedByUserId()
    {
        var setting = SystemSetting.Create("TaxRate", "0.15");

        setting.UpdateValue("0.20", updatedByUserId: 5);

        setting.UpdatedByUserId.Should().Be(5);
    }

    [Fact]
    public void UpdateValue_GivenNullUpdatedByUserId_ShouldNotSetUpdatedByUserId()
    {
        var setting = SystemSetting.Create("TaxRate", "0.15");

        setting.UpdateValue("0.20", updatedByUserId: null);

        setting.UpdatedByUserId.Should().BeNull();
    }

    [Fact]
    public void UpdateValue_GivenEmptyString_ShouldSetEmptyValue()
    {
        var setting = SystemSetting.Create("Key", "OldValue");

        setting.UpdateValue("");

        setting.SettingValue.Should().Be("");
    }

    [Fact]
    public void UpdateValue_MultipleTimes_ShouldTrackLatest()
    {
        var setting = SystemSetting.Create("Key", "v1");

        setting.UpdateValue("v2", updatedByUserId: 1);
        setting.UpdateValue("v3", updatedByUserId: 2);

        setting.SettingValue.Should().Be("v3");
        setting.UpdatedByUserId.Should().Be(2);
    }

    [Fact]
    public void Create_GivenDisplayName_ShouldSetDisplayName()
    {
        var setting = SystemSetting.Create(
            settingKey: "TaxRate",
            settingValue: "0.15",
            displayName: "معدل الضريبة"
        );

        setting.DisplayName.Should().Be("معدل الضريبة");
    }

    [Fact]
    public void Setting_HasDefaultValues()
    {
        var setting = SystemSetting.Create("Key", "Value");

        setting.SettingKey.Should().Be("Key");
        setting.SettingValue.Should().Be("Value");
        setting.Category.Should().Be("General");
        setting.SettingType.Should().Be((byte)1);
    }
}
