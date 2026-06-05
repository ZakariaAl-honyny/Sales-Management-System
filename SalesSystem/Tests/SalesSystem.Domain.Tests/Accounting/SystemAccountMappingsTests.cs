using FluentAssertions;
using SalesSystem.Domain.Accounting.Entities;

namespace SalesSystem.Domain.Tests.Accounting;

public class SystemAccountMappingsTests
{
    private static SystemAccountMappings CreateValidMappings()
    {
        return SystemAccountMappings.Create(
            defaultCashAccountId: 1,
            defaultBankAccountId: 2,
            inventoryAssetAccountId: 3,
            accountsReceivableAccountId: 4,
            accountsPayableAccountId: 5,
            vatOutputAccountId: 6,
            vatInputAccountId: 7,
            capitalAccountId: 8,
            salesRevenueAccountId: 9,
            salesReturnAccountId: 10,
            cogsAccountId: 11,
            generalExpenseAccountId: 12,
            spoilageLossAccountId: 13);
    }

    [Fact]
    public void Create_ValidMappings_Succeeds()
    {
        // Arrange & Act
        var mappings = CreateValidMappings();

        // Assert
        mappings.DefaultCashAccountId.Should().Be(1);
        mappings.DefaultBankAccountId.Should().Be(2);
        mappings.InventoryAssetAccountId.Should().Be(3);
        mappings.AccountsReceivableAccountId.Should().Be(4);
        mappings.AccountsPayableAccountId.Should().Be(5);
        mappings.VatOutputAccountId.Should().Be(6);
        mappings.VatInputAccountId.Should().Be(7);
        mappings.CapitalAccountId.Should().Be(8);
        mappings.SalesRevenueAccountId.Should().Be(9);
        mappings.SalesReturnAccountId.Should().Be(10);
        mappings.CogsAccountId.Should().Be(11);
        mappings.GeneralExpenseAccountId.Should().Be(12);
        mappings.SpoilageLossAccountId.Should().Be(13);
        mappings.BranchId.Should().BeNull();
    }

    [Fact]
    public void GetPaymentAccountId_Cash_ReturnsCashAccountId()
    {
        // Arrange
        var mappings = CreateValidMappings();

        // Act
        var accountId = mappings.GetPaymentAccountId("Cash");

        // Assert
        accountId.Should().Be(1);
    }

    [Fact]
    public void GetPaymentAccountId_NotCash_ReturnsBankAccountId()
    {
        // Arrange
        var mappings = CreateValidMappings();

        // Act & Assert
        mappings.GetPaymentAccountId("Bank").Should().Be(2);
        mappings.GetPaymentAccountId("Visa").Should().Be(2);
        mappings.GetPaymentAccountId("CreditCard").Should().Be(2);
    }
}
