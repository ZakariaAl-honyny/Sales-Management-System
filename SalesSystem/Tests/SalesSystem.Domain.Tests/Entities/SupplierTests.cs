using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class SupplierTests
{
    [Fact]
    public void Create_GivenValidName_ShouldCreateSupplier()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            accountId: 1,
            createdByUserId: 1
        );

        supplier.Name.Should().Be("Test Supplier");
        supplier.AccountId.Should().Be(1);
        supplier.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_GivenEmptyName_ShouldThrowDomainException()
    {
        var action = () => Supplier.Create(name: "", accountId: 1, createdByUserId: 1);

        action.Should().Throw<DomainException>()
            .WithMessage("*اسم المورد مطلوب*");
    }

    [Fact]
    public void Create_GivenInvalidAccountId_ShouldThrowDomainException()
    {
        var action = () => Supplier.Create(name: "Test Supplier", accountId: 0, createdByUserId: 1);

        action.Should().Throw<DomainException>()
            .WithMessage("*معرّف الحساب غير صالح*");
    }

    [Fact]
    public void Create_WithCategoryId_SetsCategoryId()
    {
        var supplier = Supplier.Create(
            name: "Test Supplier",
            accountId: 1,
            categoryId: 2,
            createdByUserId: 1
        );

        supplier.CategoryId.Should().Be(2);
    }

    [Fact]
    public void Update_WithCategoryId_SetsCategoryId()
    {
        var supplier = Supplier.Create(name: "Test Supplier", accountId: 1, createdByUserId: 1);

        supplier.Update(
            name: "Updated Supplier",
            categoryId: 3,
            updatedByUserId: 1
        );

        supplier.Name.Should().Be("Updated Supplier");
        supplier.CategoryId.Should().Be(3);
    }

    [Fact]
    public void MarkAsDeleted_ShouldSetIsActiveFalse()
    {
        var supplier = Supplier.Create(name: "Test Supplier", accountId: 1, createdByUserId: 1);

        supplier.MarkAsDeleted();

        supplier.IsActive.Should().BeFalse();
    }
}
