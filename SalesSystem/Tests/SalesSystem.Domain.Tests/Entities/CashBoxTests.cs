using Xunit;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class CashBoxTests
{
    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var box = CashBox.Create("Test Box", accountId: 1, phoneNumber: "0501234567");
        Assert.Equal("Test Box", box.BoxName);
        Assert.Equal(1, box.AccountId);
        Assert.Equal("0501234567", box.PhoneNumber);
        Assert.True(box.IsActive);
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CashBox.Create("", accountId: 1));
    }

    [Fact]
    public void Create_AccountIdIsOptional_AllowsZeroOrNegative_ServiceValidates()
    {
        // AccountId validation moved to service layer — domain allows nullable
        var boxZero = CashBox.Create("Test Box", accountId: 0);
        Assert.Equal(0, boxZero.AccountId);

        var boxNegative = CashBox.Create("Test Box", accountId: -1);
        Assert.Equal(-1, boxNegative.AccountId);

        var boxNull = CashBox.Create("Test Box"); // no accountId
        Assert.Null(boxNull.AccountId);
    }

    [Fact]
    public void Create_WithOptionalParameters_SetsCorrectly()
    {
        var box = CashBox.Create(
            "Box Name",
            accountId: 1,
            categoryId: 5,
            branchId: 2,
            assignedUserId: 3,
            currencyId: 1,
            phoneNumber: "0555000111",
            taxNumber: "TX12345",
            address: "Riyadh",
            notes: "Main cash box");

        Assert.Equal("Box Name", box.BoxName);
        Assert.Equal(1, box.AccountId);
        Assert.Equal(5, box.CategoryId);
        Assert.Equal(2, box.BranchId);
        Assert.Equal(3, box.AssignedUserId);
        Assert.Equal(1, box.CurrencyId);
        Assert.Equal("0555000111", box.PhoneNumber);
        Assert.Equal("TX12345", box.TaxNumber);
        Assert.Equal("Riyadh", box.Address);
        Assert.Equal("Main cash box", box.Notes);
    }

    [Fact]
    public void ValidateUserAccess_AssignedToDifferentUser_ShouldThrow()
    {
        var box = CashBox.Create("صندوق المدير", accountId: 1, assignedUserId: 1);
        Assert.Throws<DomainException>(() => box.ValidateUserAccess(2));
    }

    [Fact]
    public void ValidateUserAccess_SharedBox_ShouldAllowAnyone()
    {
        var box = CashBox.Create("صندوق مشترك", accountId: 1);
        box.ValidateUserAccess(99); // Should not throw
        Assert.True(true);
    }

    [Fact]
    public void UpdateName_ValidInput_UpdatesName()
    {
        var box = CashBox.Create("Old Name", accountId: 1);
        box.UpdateName("New Name");
        Assert.Equal("New Name", box.BoxName);
    }

    [Fact]
    public void UpdateName_EmptyName_ShouldThrow()
    {
        var box = CashBox.Create("Old Name", accountId: 1);
        Assert.Throws<DomainException>(() => box.UpdateName(""));
    }
}
