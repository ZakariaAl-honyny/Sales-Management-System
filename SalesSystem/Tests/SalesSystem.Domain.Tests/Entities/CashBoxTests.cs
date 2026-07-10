using Xunit;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class CashBoxTests
{
    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var box = CashBox.Create("Test Box", currencyId: 1, accountId: 1, description: "0501234567");
        Assert.Equal("Test Box", box.Name);
        Assert.Equal(1, box.AccountId);
        Assert.Equal((short)1, box.CurrencyId);
        Assert.Equal("0501234567", box.Description);
        Assert.True(box.IsActive);
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CashBox.Create("", currencyId: 1, accountId: 1));
    }

    [Fact]
    public void Create_AccountIdZero_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CashBox.Create("Test Box", currencyId: 1, accountId: 0));
    }

    [Fact]
    public void Create_CurrencyIdZero_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => CashBox.Create("Test Box", currencyId: 0, accountId: 1));
    }

    [Fact]
    public void Create_WithOptionalParameters_SetsCorrectly()
    {
        var box = CashBox.Create(
            "Box Name",
            currencyId: 1,
            accountId: 1,
            description: "Main cash box");

        Assert.Equal("Box Name", box.Name);
        Assert.Equal(1, box.AccountId);
        Assert.Equal((short)1, box.CurrencyId);
        Assert.Equal("Main cash box", box.Description);
    }

    [Fact]
    public void Update_ValidInput_UpdatesName()
    {
        var box = CashBox.Create("Old Name", currencyId: 1, accountId: 1);
        box.Update("New Name", (short)1);
        Assert.Equal("New Name", box.Name);
    }

    [Fact]
    public void Update_EmptyName_ShouldThrow()
    {
        var box = CashBox.Create("Old Name", currencyId: 1, accountId: 1);
        Assert.Throws<DomainException>(() => box.Update("", (short)1));
    }

    [Fact]
    public void Update_ValidInput_UpdatesDescription()
    {
        var box = CashBox.Create("Name", currencyId: 1, accountId: 1, description: "Old description");
        box.Update("Name", (short)1, "New description");
        Assert.Equal("New description", box.Description);
    }

    [Fact]
    public void Create_CurrencyId_SetsCorrectly()
    {
        var box = CashBox.Create("Test Box", currencyId: 2, accountId: 1);
        Assert.Equal((short)2, box.CurrencyId);
    }
}
