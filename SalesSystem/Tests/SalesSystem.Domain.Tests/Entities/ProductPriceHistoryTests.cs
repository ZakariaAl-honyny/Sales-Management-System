using FluentAssertions;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Domain.Tests.Entities;

public class ProductPriceHistoryTests
{
    [Fact]
    public void Create_GivenValidData_ShouldSetAllProperties()
    {
        var history = ProductPriceHistory.Create(
            productUnitId: 1,
            changeType: "CostUpdate",
            oldValue: 10.50m,
            newValue: 12.00m,
            costingMethod: "WeightedAverage",
            invoiceId: 42,
            changedBy: 5
        );

        history.ProductUnitId.Should().Be(1);
        history.ChangeType.Should().Be("CostUpdate");
        history.OldValue.Should().Be(10.50m);
        history.NewValue.Should().Be(12.00m);
        history.CostingMethod.Should().Be("WeightedAverage");
        history.InvoiceId.Should().Be(42);
        history.ChangedBy.Should().Be(5);
        history.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_GivenDefaultParameters_ShouldApplyDefaults()
    {
        var history = ProductPriceHistory.Create(
            productUnitId: 1,
            changeType: "ManualAdjustment",
            oldValue: 5m,
            newValue: 6m
        );

        history.CostingMethod.Should().BeNull();
        history.InvoiceId.Should().BeNull();
        history.ChangedBy.Should().Be(0);
    }

    [Fact]
    public void Create_GivenNullCostingMethod_ShouldStoreNull()
    {
        var history = ProductPriceHistory.Create(
            productUnitId: 1,
            changeType: "PriceUpdate",
            oldValue: 100m,
            newValue: 150m,
            costingMethod: null
        );

        history.CostingMethod.Should().BeNull();
    }

    [Fact]
    public void Create_GivenNullInvoiceId_ShouldStoreNull()
    {
        var history = ProductPriceHistory.Create(
            productUnitId: 1,
            changeType: "PriceUpdate",
            oldValue: 100m,
            newValue: 150m,
            invoiceId: null
        );

        history.InvoiceId.Should().BeNull();
    }

    [Fact]
    public void Create_GivenMultipleEntries_ShouldEachHaveOwnValues()
    {
        var first = ProductPriceHistory.Create(1, "CostUpdate", 10m, 15m, changedBy: 1);
        var second = ProductPriceHistory.Create(2, "PriceUpdate", 20m, 25m, changedBy: 2);

        first.ProductUnitId.Should().Be(1);
        first.ChangeType.Should().Be("CostUpdate");
        second.ProductUnitId.Should().Be(2);
        second.ChangeType.Should().Be("PriceUpdate");
    }

    [Fact]
    public void Create_GivenZeroChangedBy_ShouldSucceed()
    {
        var history = ProductPriceHistory.Create(
            productUnitId: 1,
            changeType: "SystemUpdate",
            oldValue: 0m,
            newValue: 0m,
            changedBy: 0
        );

        history.ChangedBy.Should().Be(0);
    }

    [Fact]
    public void Create_WhenCalled_SetsChangedAtToUtcNow()
    {
        var before = DateTime.UtcNow;
        var history = ProductPriceHistory.Create(1, "Test", 0m, 0m);
        var after = DateTime.UtcNow;

        history.ChangedAt.Should().BeOnOrAfter(before);
        history.ChangedAt.Should().BeOnOrBefore(after);
    }
}
