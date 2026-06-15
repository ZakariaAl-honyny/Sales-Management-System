using Xunit;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class CashBoxTests
{
    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var box = CashBox.Create("Test Box", branchId: (short)1, accountId: 1, description: "0501234567");
        Assert.Equal("Test Box", box.Name);
        Assert.Equal(1, box.AccountId);
        Assert.Equal((short)1, box.BranchId);
        Assert.Equal("0501234567", box.Description);
        Assert.True(box.IsActive);
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CashBox.Create("", branchId: (short)1, accountId: 1));
    }

    [Fact]
    public void Create_AccountIdZero_ShouldThrow()
    {
        // AccountId <= 0 is now validated in Create()
        Assert.Throws<DomainException>(() => CashBox.Create("Test Box", branchId: (short)1, accountId: 0));
    }

    [Fact]
    public void Create_BranchIdZero_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CashBox.Create("Test Box", branchId: (short)0, accountId: 1));
    }

    [Fact]
    public void Create_WithOptionalParameters_SetsCorrectly()
    {
        var box = CashBox.Create(
            "Box Name",
            branchId: (short)2,
            accountId: 1,
            description: "Main cash box");

        Assert.Equal("Box Name", box.Name);
        Assert.Equal(1, box.AccountId);
        Assert.Equal((short)2, box.BranchId);
        Assert.Equal("Main cash box", box.Description);
    }

    [Fact]
    public void Update_ValidInput_UpdatesName()
    {
        var box = CashBox.Create("Old Name", branchId: (short)1, accountId: 1);
        box.Update("New Name", (short)1);
        Assert.Equal("New Name", box.Name);
    }

    [Fact]
    public void Update_EmptyName_ShouldThrow()
    {
        var box = CashBox.Create("Old Name", branchId: (short)1, accountId: 1);
        Assert.Throws<DomainException>(() => box.Update("", (short)1));
    }

    [Fact]
    public void Update_ValidInput_UpdatesDescription()
    {
        var box = CashBox.Create("Name", branchId: (short)1, accountId: 1, description: "Old description");
        box.Update("Name", (short)1, "New description");
        Assert.Equal("New description", box.Description);
    }
}
