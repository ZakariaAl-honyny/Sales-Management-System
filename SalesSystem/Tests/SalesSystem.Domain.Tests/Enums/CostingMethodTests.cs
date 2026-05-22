using Xunit;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Tests.Enums;

public class CostingMethodTests
{
    [Fact]
    public void WeightedAverage_ShouldBeOne()
    {
        Assert.Equal(1, (int)CostingMethod.WeightedAverage);
    }

    [Fact]
    public void LastPurchasePrice_ShouldBeTwo()
    {
        Assert.Equal(2, (int)CostingMethod.LastPurchasePrice);
    }

    [Fact]
    public void SupplierPrice_ShouldBeThree()
    {
        Assert.Equal(3, (int)CostingMethod.SupplierPrice);
    }
}
