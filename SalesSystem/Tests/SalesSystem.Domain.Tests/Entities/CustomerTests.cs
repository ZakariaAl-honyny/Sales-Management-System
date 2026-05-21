using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class CustomerTests
{
    [Fact]
    public void Create_GivenValidName_ShouldCreateCustomer()
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 0,
            code: "C001",
            phone: "1234567890",
            email: "test@example.com",
            address: "Test Address",
            createdByUserId: 1
        );

        customer.Name.Should().Be("Test Customer");
        customer.OpeningBalance.Should().Be(0);
        customer.CurrentBalance.Should().Be(0);
        customer.Code.Should().Be("C001");
        customer.Phone.Should().Be("1234567890");
        customer.Email.Should().Be("test@example.com");
        customer.Address.Should().Be("Test Address");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenEmptyName_ShouldThrowArgumentException(string? emptyName)
    {
        var action = () => Customer.Create(
            name: emptyName!,
            createdByUserId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم العميل مطلوب*");
    }

    [Fact]
    public void Create_GivenOpeningBalance_ShouldSetCurrentBalance()
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 500m,
            createdByUserId: 1
        );

        customer.OpeningBalance.Should().Be(500m);
        customer.CurrentBalance.Should().Be(500m);
    }

    [Fact]
    public void IncreaseBalance_GivenPositiveAmount_ShouldIncreaseCurrentBalance()
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 100m,
            createdByUserId: 1
        );

        customer.IncreaseBalance(50m);

        customer.CurrentBalance.Should().Be(150m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void IncreaseBalance_GivenInvalidAmount_ShouldThrowArgumentException(decimal invalidAmount)
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 100m,
            createdByUserId: 1
        );

        var action = () => customer.IncreaseBalance(invalidAmount);

        action.Should().Throw<DomainException>()
            .WithMessage("*المبلغ يجب أن يكون أكبر من الصفر*");
    }

    [Fact]
    public void IncreaseBalance_MultipleTimes_ShouldAccumulateCorrectly()
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 100m,
            createdByUserId: 1
        );

        customer.IncreaseBalance(50m);
        customer.IncreaseBalance(30m);
        customer.IncreaseBalance(20m);

        customer.CurrentBalance.Should().Be(200m);
    }

    [Fact]
    public void DecreaseBalance_GivenPositiveAmount_ShouldDecreaseCurrentBalance()
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 100m,
            createdByUserId: 1
        );

        customer.DecreaseBalance(30m);

        customer.CurrentBalance.Should().Be(70m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void DecreaseBalance_GivenInvalidAmount_ShouldThrowArgumentException(decimal invalidAmount)
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 100m,
            createdByUserId: 1
        );

        var action = () => customer.DecreaseBalance(invalidAmount);

        action.Should().Throw<DomainException>()
            .WithMessage("*المبلغ يجب أن يكون أكبر من الصفر*");
    }

    [Fact]
    public void DecreaseBalance_MultipleTimes_ShouldAccumulateCorrectly()
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 200m,
            createdByUserId: 1
        );

        customer.DecreaseBalance(50m);
        customer.DecreaseBalance(30m);
        customer.DecreaseBalance(20m);

        customer.CurrentBalance.Should().Be(100m);
    }

    [Fact]
    public void DecreaseBalance_CanMakeBalanceNegative()
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 100m,
            createdByUserId: 1
        );

        customer.DecreaseBalance(150m);

        customer.CurrentBalance.Should().Be(-50m);
    }

    [Fact]
    public void IncreaseBalance_CanMakeBalancePositive()
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 0,
            createdByUserId: 1
        );

        customer.IncreaseBalance(50m);

        customer.CurrentBalance.Should().Be(50m);
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateCustomer()
    {
        var customer = Customer.Create(
            name: "Original Name",
            code: "C001",
            phone: "1111111111",
            email: "old@example.com",
            address: "Old Address",
            createdByUserId: 1
        );

        customer.Update(
            name: "Updated Name",
            code: "C002",
            phone: "2222222222",
            email: "new@example.com",
            address: "New Address",
            taxNumber: null,
            creditLimit: 0,
            updatedByUserId: 1
        );

        customer.Name.Should().Be("Updated Name");
        customer.Code.Should().Be("C002");
        customer.Phone.Should().Be("2222222222");
        customer.Email.Should().Be("new@example.com");
        customer.Address.Should().Be("New Address");
    }

    [Fact]
    public void Create_GivenOptionalParametersAreNull_ShouldSucceed()
    {
        var customer = Customer.Create(
            name: "Test Customer",
            createdByUserId: 1
        );

        customer.Code.Should().BeNull();
        customer.Phone.Should().BeNull();
        customer.Email.Should().BeNull();
        customer.Address.Should().BeNull();
    }

    [Fact]
    public void Create_GivenNegativeOpeningBalance_ShouldThrowDomainException()
    {
        var action = () => Customer.Create(
            name: "Test Customer",
            openingBalance: -100m,
            createdByUserId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*الرصيد الافتتاحي لا يمكن أن يكون سالباً*");
    }

    [Fact]
    public void DecreaseBalance_ExactAmount_ShouldSetBalanceToZero()
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 100m,
            createdByUserId: 1
        );

        customer.DecreaseBalance(100m);

        customer.CurrentBalance.Should().Be(0);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.5)]
    [InlineData(999.99)]
    public void IncreaseBalance_GivenDecimalAmount_ShouldAccept(decimal amount)
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 100m,
            createdByUserId: 1
        );

        customer.IncreaseBalance(amount);

        customer.CurrentBalance.Should().Be(100m + amount);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.5)]
    [InlineData(999.99)]
    public void DecreaseBalance_GivenDecimalAmount_ShouldAccept(decimal amount)
    {
        var customer = Customer.Create(
            name: "Test Customer",
            openingBalance: 1000m,
            createdByUserId: 1
        );

        customer.DecreaseBalance(amount);

        customer.CurrentBalance.Should().Be(1000m - amount);
    }
}