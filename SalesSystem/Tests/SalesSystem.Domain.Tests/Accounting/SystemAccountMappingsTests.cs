using FluentAssertions;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Exceptions;

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

    // ─── Create ───────────────────────────────────────

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
    public void Create_WithBranchId_SetsBranchId()
    {
        // Act
        var mappings = SystemAccountMappings.Create(
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
            spoilageLossAccountId: 13,
            branchId: 2);

        // Assert
        mappings.BranchId.Should().Be(2);
    }

    [Fact]
    public void Create_WithCreatedByUserId_SetsCreatedBy()
    {
        // Act
        var mappings = SystemAccountMappings.Create(
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
            spoilageLossAccountId: 13,
            createdByUserId: 5);

        // Assert
        mappings.CreatedByUserId.Should().Be(5);
    }

    // ─── Guard: Zero Account IDs ──────────────────────

    [Fact]
    public void Create_ZeroCashAccount_ThrowsDomainException()
    {
        // Act
        var act = () => SystemAccountMappings.Create(
            defaultCashAccountId: 0,
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

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الصندوق النقدي");
    }

    [Fact]
    public void Create_NegativeCashAccount_ThrowsDomainException()
    {
        // Act
        var act = () => SystemAccountMappings.Create(
            defaultCashAccountId: -1,
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

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الصندوق النقدي");
    }

    [Fact]
    public void Create_ZeroBankAccount_ThrowsDomainException()
    {
        // Act
        var act = () => SystemAccountMappings.Create(
            defaultCashAccountId: 1,
            defaultBankAccountId: 0,
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

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الحساب البنكي");
    }

    [Fact]
    public void Create_ZeroInventoryAccount_ThrowsDomainException()
    {
        // Act
        var act = () => SystemAccountMappings.Create(
            defaultCashAccountId: 1,
            defaultBankAccountId: 2,
            inventoryAssetAccountId: 0,
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

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("المخزون");
    }

    [Fact]
    public void Create_ZeroReceivablesAccount_ThrowsDomainException()
    {
        // Act
        var act = () => SystemAccountMappings.Create(
            defaultCashAccountId: 1,
            defaultBankAccountId: 2,
            inventoryAssetAccountId: 3,
            accountsReceivableAccountId: 0,
            accountsPayableAccountId: 5,
            vatOutputAccountId: 6,
            vatInputAccountId: 7,
            capitalAccountId: 8,
            salesRevenueAccountId: 9,
            salesReturnAccountId: 10,
            cogsAccountId: 11,
            generalExpenseAccountId: 12,
            spoilageLossAccountId: 13);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الذمم المدينة");
    }

    [Fact]
    public void Create_ZeroPayablesAccount_ThrowsDomainException()
    {
        // Act
        var act = () => SystemAccountMappings.Create(
            defaultCashAccountId: 1,
            defaultBankAccountId: 2,
            inventoryAssetAccountId: 3,
            accountsReceivableAccountId: 4,
            accountsPayableAccountId: 0,
            vatOutputAccountId: 6,
            vatInputAccountId: 7,
            capitalAccountId: 8,
            salesRevenueAccountId: 9,
            salesReturnAccountId: 10,
            cogsAccountId: 11,
            generalExpenseAccountId: 12,
            spoilageLossAccountId: 13);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("الذمم الدائنة");
    }

    [Fact]
    public void Create_ZeroRevenueAccount_ThrowsDomainException()
    {
        // Act
        var act = () => SystemAccountMappings.Create(
            defaultCashAccountId: 1,
            defaultBankAccountId: 2,
            inventoryAssetAccountId: 3,
            accountsReceivableAccountId: 4,
            accountsPayableAccountId: 5,
            vatOutputAccountId: 6,
            vatInputAccountId: 7,
            capitalAccountId: 8,
            salesRevenueAccountId: 0,
            salesReturnAccountId: 10,
            cogsAccountId: 11,
            generalExpenseAccountId: 12,
            spoilageLossAccountId: 13);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("إيرادات المبيعات");
    }

    [Fact]
    public void Create_ZeroCogsAccount_ThrowsDomainException()
    {
        // Act
        var act = () => SystemAccountMappings.Create(
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
            cogsAccountId: 0,
            generalExpenseAccountId: 12,
            spoilageLossAccountId: 13);

        // Assert
        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("تكلفة البضاعة");
    }

    // ─── GetPaymentAccountId ──────────────────────────

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
    public void GetPaymentAccountId_CashLowercase_ReturnsCashAccountId()
    {
        // Arrange
        var mappings = CreateValidMappings();

        // Act
        var accountId = mappings.GetPaymentAccountId("cash");

        // Assert
        accountId.Should().Be(1);
    }

    [Fact]
    public void GetPaymentAccountId_Bank_ReturnsBankAccountId()
    {
        // Arrange
        var mappings = CreateValidMappings();

        // Act
        var accountId = mappings.GetPaymentAccountId("Bank");

        // Assert
        accountId.Should().Be(2);
    }

    [Fact]
    public void GetPaymentAccountId_Visa_ReturnsBankAccountId()
    {
        // Arrange
        var mappings = CreateValidMappings();

        // Act
        var accountId = mappings.GetPaymentAccountId("Visa");

        // Assert
        accountId.Should().Be(2);
    }

    [Fact]
    public void GetPaymentAccountId_CreditCard_ReturnsBankAccountId()
    {
        // Arrange
        var mappings = CreateValidMappings();

        // Act
        var accountId = mappings.GetPaymentAccountId("CreditCard");

        // Assert
        accountId.Should().Be(2);
    }

    [Fact]
    public void GetPaymentAccountId_Network_ReturnsBankAccountId()
    {
        // Arrange
        var mappings = CreateValidMappings();

        // Act
        var accountId = mappings.GetPaymentAccountId("Network");

        // Assert
        accountId.Should().Be(2);
    }

    [Fact]
    public void GetPaymentAccountId_UnknownPaymentMethod_ReturnsBankAccountId()
    {
        // Arrange
        var mappings = CreateValidMappings();

        // Act
        var accountId = mappings.GetPaymentAccountId("SomeUnknownMethod");

        // Assert
        accountId.Should().Be(2);
    }

    [Fact]
    public void GetPaymentAccountId_EmptyString_ReturnsBankAccountId()
    {
        // Arrange
        var mappings = CreateValidMappings();

        // Act
        var accountId = mappings.GetPaymentAccountId("");

        // Assert
        accountId.Should().Be(2);
    }

    [Fact]
    public void GetPaymentAccountId_Null_ReturnsBankAccountId()
    {
        // Arrange
        var mappings = CreateValidMappings();

        // Act
        var accountId = mappings.GetPaymentAccountId(null!);

        // Assert
        accountId.Should().Be(2);
    }
}
