using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class SupplierTests
{
    [Fact]
    public void Create_GivenValidPartyId_ShouldCreateSupplier()
    {
        var supplier = Supplier.Create(
            partyId: 1,
            createdByUserId: 1
        );

        supplier.Id.Should().Be(1);
        supplier.PaymentTerms.Should().BeNull();
        supplier.Notes.Should().BeNull();
    }

    [Fact]
    public void Create_GivenPaymentTerms_ShouldSetPaymentTerms()
    {
        var supplier = Supplier.Create(
            partyId: 1,
            paymentTerms: "صافي 30 يوم",
            createdByUserId: 1
        );

        supplier.PaymentTerms.Should().Be("صافي 30 يوم");
    }

    [Fact]
    public void Create_GivenInvalidPartyId_ShouldThrowDomainException()
    {
        var action = () => Supplier.Create(partyId: 0, createdByUserId: 1);

        action.Should().Throw<DomainException>()
            .WithMessage("*معرّف الطرف غير صالح*");
    }

    [Fact]
    public void Update_GivenValidData_ShouldUpdateSupplier()
    {
        var supplier = Supplier.Create(partyId: 1, createdByUserId: 1);

        supplier.Update(
            paymentTerms: "صافي 60 يوم",
            notes: "ملاحظات",
            updatedByUserId: 1
        );

        supplier.PaymentTerms.Should().Be("صافي 60 يوم");
        supplier.Notes.Should().Be("ملاحظات");
    }

    [Fact]
    public void MarkAsDeleted_ShouldSetIsActiveFalse()
    {
        var supplier = Supplier.Create(partyId: 1, createdByUserId: 1);

        supplier.MarkAsDeleted();

        supplier.IsActive.Should().BeFalse();
    }
}
