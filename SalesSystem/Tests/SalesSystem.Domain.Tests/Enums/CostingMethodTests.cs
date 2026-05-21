using Xunit;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Tests.Enums;

public class CostingMethodTests
{
    [Fact]
    public void WeightedAverage_ShouldBeZero()
    {
        Assert.Equal(0, (int)CostingMethod.WeightedAverage);
    }

    [Fact]
    public void LastPurchasePrice_ShouldBeOne()
    {
        Assert.Equal(1, (int)CostingMethod.LastPurchasePrice);
    }

    [Fact]
    public void SupplierPrice_ShouldBeTwo()
    {
        Assert.Equal(2, (int)CostingMethod.SupplierPrice);
    }
}
