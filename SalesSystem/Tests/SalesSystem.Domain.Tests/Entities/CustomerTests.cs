using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class CustomerTests
{
    [Fact]
    public void Create_GivenValidPartyId_ShouldCreateCustomer()
    {
        var customer = Customer.Create(
            partyId: 1,
            creditLimit: 0,
            createdByUserId: 1
        );

        customer.Id.Should().Be(1);
        customer.CreditLimit.Should().Be(0);
        customer.CustomerSince.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_GivenCreditLimit_ShouldSetCreditLimit()
    {
        var customer = Customer.Create(
            partyId: 1,
            creditLimit: 500m,
            createdByUserId: 1
        );

        customer.CreditLimit.Should().Be(500m);
    }

    [Fact]
    public void Create_GivenPriceLevel_ShouldSetPriceLevel()
    {
        var customer = Customer.Create(
            partyId: 1,
            priceLevel: 2,
            createdByUserId: 1
        );

        customer.PriceLevel.Should().Be(2);
    }

    [Fact]
    public void Create_GivenInvalidPartyId_ShouldThrowDomainException()
    {
        var action = () => Customer.Create(partyId: 0, createdByUserId: 1);

        action.Should().Throw<DomainException>()
            .WithMessage("*معرّف الطرف غير صالح*");
    }

    [Fact]
    public void Create_GivenNegativeCreditLimit_ShouldThrowDomainException()
    {
        var action = () => Customer.Create(partyId: 1, creditLimit: -100m, createdByUserId: 1);

        action.Should().Throw<DomainException>()
            .WithMessage("*حد الائتمان لا يمكن أن يكون سالباً*");
    }

    [Fact]
    public void Create_GivenInvalidPriceLevel_ShouldThrowDomainException()
    {
        var action = () => Customer.Create(partyId: 1, priceLevel: 5, createdByUserId: 1);

        action.Should().Throw<DomainException>()
            .WithMessage("*مستوى السعر يجب أن يكون بين 1 و 4*");
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateCustomer()
    {
        var customer = Customer.Create(partyId: 1, creditLimit: 1000m, priceLevel: 2, createdByUserId: 1);

        customer.Update(
            creditLimit: 5000m,
            priceLevel: 3,
            updatedByUserId: 1
        );

        customer.CreditLimit.Should().Be(5000m);
        customer.PriceLevel.Should().Be(3);
    }

    [Fact]
    public void CheckCreditLimit_ZeroLimit_ShouldReturnTrue()
    {
        var customer = Customer.Create(partyId: 1, creditLimit: 0m, createdByUserId: 1);

        var result = customer.CheckCreditLimit(10000m);

        result.Should().BeTrue();
    }

    [Fact]
    public void CheckCreditLimit_UnderLimit_ShouldReturnTrue()
    {
        var customer = Customer.Create(partyId: 1, creditLimit: 1000m, createdByUserId: 1);

        var result = customer.CheckCreditLimit(500m);

        result.Should().BeTrue();
    }

    [Fact]
    public void MarkAsDeleted_ShouldSetIsActiveFalse()
    {
        var customer = Customer.Create(partyId: 1, createdByUserId: 1);

        customer.MarkAsDeleted();

        customer.IsActive.Should().BeFalse();
    }
}
