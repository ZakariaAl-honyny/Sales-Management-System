using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class SupplierTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateSupplier()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 0,
            phone: "1234567890",
            email: "supplier@example.com",
            address: "Supplier Address",
            createdByUserId: 1
        );

        supplier.Name.Should().Be("Test Supplier");
        supplier.OpeningBalance.Should().Be(0);
        supplier.CurrentBalance.Should().Be(0);
        supplier.Phone.Should().Be("1234567890");
        supplier.Email.Should().Be("supplier@example.com");
        supplier.Address.Should().Be("Supplier Address");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        var action = () => Supplier.Create(name: invalidName!);

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المورد مطلوب*");
    }

    [Fact]
    public void Create_GivenOpeningBalance_ShouldSetCurrentBalance()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 500m,
            createdByUserId: 1
        );

        supplier.OpeningBalance.Should().Be(500m);
        supplier.CurrentBalance.Should().Be(500m);
    }

    [Fact]
    public void IncreaseBalance_GivenPositiveAmount_ShouldIncreaseCurrentBalance()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 100m,
            createdByUserId: 1
        );

        supplier.IncreaseBalance(50m);

        supplier.CurrentBalance.Should().Be(150m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void IncreaseBalance_GivenInvalidAmount_ShouldThrowArgumentException(decimal invalidAmount)
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 100m,
            createdByUserId: 1
        );

        var action = () => supplier.IncreaseBalance(invalidAmount);

        action.Should().Throw<DomainException>()
            .WithMessage("*المبلغ يجب أن يكون أكبر من الصفر*");
    }

    [Fact]
    public void IncreaseBalance_MultipleTimes_ShouldAccumulateCorrectly()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 100m,
            createdByUserId: 1
        );

        supplier.IncreaseBalance(50m);
        supplier.IncreaseBalance(30m);
        supplier.IncreaseBalance(20m);

        supplier.CurrentBalance.Should().Be(200m);
    }

    [Fact]
    public void DecreaseBalance_GivenPositiveAmount_ShouldDecreaseCurrentBalance()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 100m,
            createdByUserId: 1
        );

        supplier.DecreaseBalance(30m);

        supplier.CurrentBalance.Should().Be(70m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void DecreaseBalance_GivenInvalidAmount_ShouldThrowArgumentException(decimal invalidAmount)
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 100m,
            createdByUserId: 1
        );

        var action = () => supplier.DecreaseBalance(invalidAmount);

        action.Should().Throw<DomainException>()
            .WithMessage("*المبلغ يجب أن يكون أكبر من الصفر*");
    }

    [Fact]
    public void DecreaseBalance_MultipleTimes_ShouldAccumulateCorrectly()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 200m,
            createdByUserId: 1
        );

        supplier.DecreaseBalance(50m);
        supplier.DecreaseBalance(30m);
        supplier.DecreaseBalance(20m);

        supplier.CurrentBalance.Should().Be(100m);
    }

    [Fact]
    public void DecreaseBalance_CanMakeBalanceNegative()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 100m,
            createdByUserId: 1
        );

        supplier.DecreaseBalance(150m);

        supplier.CurrentBalance.Should().Be(-50m);
    }

    [Fact]
    public void IncreaseBalance_CanMakeBalancePositive()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 0,
            createdByUserId: 1
        );

        supplier.IncreaseBalance(50m);

        supplier.CurrentBalance.Should().Be(50m);
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateSupplier()
    {
        var supplier = Supplier.Create(
            name: "Original Name",
            phone: "1111111111",
            email: "old@example.com",
            address: "Old Address",
            createdByUserId: 1
        );

        supplier.Update(
            name: "Updated Name",
            phone: "2222222222",
            email: "new@example.com",
            address: "New Address",
            taxNumber: null,
            creditLimit: 0,
            updatedByUserId: 1
        );

        supplier.Name.Should().Be("Updated Name");
        supplier.Phone.Should().Be("2222222222");
        supplier.Email.Should().Be("new@example.com");
        supplier.Address.Should().Be("New Address");
    }

    [Fact]
    public void Create_GivenOptionalParametersAreNull_ShouldSucceed()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            createdByUserId: 1
        );

        supplier.Phone.Should().BeNull();
        supplier.Email.Should().BeNull();
        supplier.Address.Should().BeNull();
    }

    [Fact]
    public void Create_GivenNegativeOpeningBalance_ShouldThrowDomainException()
    {
        var action = () => Supplier.Create(
            name: "Test Supplier",
            openingBalance: -100m,
            createdByUserId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الرصيد الافتتاحي لا يمكن أن يكون سالباً*");
    }

    [Fact]
    public void DecreaseBalance_ExactAmount_ShouldSetBalanceToZero()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 100m,
            createdByUserId: 1
        );

        supplier.DecreaseBalance(100m);

        supplier.CurrentBalance.Should().Be(0);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.5)]
    [InlineData(999.99)]
    public void IncreaseBalance_GivenDecimalAmount_ShouldAccept(decimal amount)
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 100m,
            createdByUserId: 1
        );

        supplier.IncreaseBalance(amount);

        supplier.CurrentBalance.Should().Be(100m + amount);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.5)]
    [InlineData(999.99)]
    public void DecreaseBalance_GivenDecimalAmount_ShouldAccept(decimal amount)
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            openingBalance: 1000m,
            createdByUserId: 1
        );

        supplier.DecreaseBalance(amount);

        supplier.CurrentBalance.Should().Be(1000m - amount);
    }
}