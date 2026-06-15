using Xunit;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class CashBoxTests
{
    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var box = CashBox.Create("Test Box", accountId: 1, currencyId: 1, phoneNumber: "0501234567");
        Assert.Equal("Test Box", box.BoxName);
        Assert.Equal(1, box.AccountId);
        Assert.Equal((short)1, box.CurrencyId);
        Assert.Equal("0501234567", box.PhoneNumber);
        Assert.True(box.IsActive);
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CashBox.Create("", accountId: 1, currencyId: 1));
    }

    [Fact]
    public void Create_AccountIdZero_ShouldSucceed_ValidationDeferredToSetAccountId()
    {
        // AccountId is nullable during creation — validation happens in SetAccountId
        var box = CashBox.Create("Test Box", currencyId: 1, accountId: 0);
        Assert.NotNull(box);
        Assert.Equal(0, box.AccountId);
    }

    [Fact]
    public void SetAccountId_Zero_ShouldThrow()
    {
        var box = CashBox.Create("Test Box", currencyId: 1);
        Assert.Throws<DomainException>(() => box.SetAccountId(0));
    }

    [Fact]
    public void Create_CurrencyIdZero_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CashBox.Create("Test Box", accountId: 1, currencyId: 0));
    }

    [Fact]
    public void Create_WithOptionalParameters_SetsCorrectly()
    {
        var box = CashBox.Create(
            "Box Name",
            accountId: 1,
            currencyId: 1,
            branchId: (short)2,
            assignedUserId: 3,
            phoneNumber: "0555000111",
            taxNumber: "TX12345",
            address: "Riyadh",
            notes: "Main cash box");

        Assert.Equal("Box Name", box.BoxName);
        Assert.Equal(1, box.AccountId);
        Assert.Equal((short)2, box.BranchId);
        Assert.Equal(3, box.AssignedUserId);
        Assert.Equal((short)1, box.CurrencyId);
        Assert.Equal("0555000111", box.PhoneNumber);
        Assert.Equal("TX12345", box.TaxNumber);
        Assert.Equal("Riyadh", box.Address);
        Assert.Equal("Main cash box", box.Notes);
    }

    [Fact]
    public void ValidateUserAccess_AssignedToDifferentUser_ShouldThrow()
    {
        var box = CashBox.Create("صندوق المدير", accountId: 1, currencyId: 1, assignedUserId: 1);
        Assert.Throws<DomainException>(() => box.ValidateUserAccess(2));
    }

    [Fact]
    public void ValidateUserAccess_SharedBox_ShouldAllowAnyone()
    {
        var box = CashBox.Create("صندوق مشترك", accountId: 1, currencyId: 1);
        box.ValidateUserAccess(99); // Should not throw
    }

    [Fact]
    public void UpdateName_ValidInput_UpdatesName()
    {
        var box = CashBox.Create("Old Name", accountId: 1, currencyId: 1);
        box.UpdateName("New Name");
        Assert.Equal("New Name", box.BoxName);
    }

    [Fact]
    public void UpdateName_EmptyName_ShouldThrow()
    {
        var box = CashBox.Create("Old Name", accountId: 1, currencyId: 1);
        Assert.Throws<DomainException>(() => box.UpdateName(""));
    }
}
