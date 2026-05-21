using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class SupplierPaymentTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateSupplierPayment()
    {
        var payment = SupplierPayment.Create(
            paymentNo: "SP-2026-000001",
            supplierId: 1,
            amount: 500m,
            paymentMethod: 1, // Cash
            purchaseInvoiceId: 10,
            referenceNo: "REF001",
            notes: "Payment for invoice",
            createdByUserId: 1,
            paymentDate: new DateTime(2026, 1, 15)
        );

        payment.PaymentNo.Should().Be("SP-2026-000001");
        payment.SupplierId.Should().Be(1);
        payment.Amount.Should().Be(500m);
        payment.PaymentMethod.Should().Be(1);
        payment.PurchaseInvoiceId.Should().Be(10);
        payment.ReferenceNo.Should().Be("REF001");
        payment.Notes.Should().Be("Payment for invoice");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidPaymentNo_ShouldThrowArgumentException(string? invalidPaymentNo)
    {
        var action = () => SupplierPayment.Create(
            paymentNo: invalidPaymentNo!,
            supplierId: 1,
            amount: 100m,
            paymentMethod: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*رقم السداد مطلوب*");
    }

    [Fact]
    public void Create_GivenSupplierIdIsZero_ShouldThrowArgumentException()
    {
        var action = () => SupplierPayment.Create(
            paymentNo: "SP-001",
            supplierId: 0,
            amount: 100m,
            paymentMethod: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*المورد مطلوب*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenInvalidAmount_ShouldThrowArgumentException(decimal invalidAmount)
    {
        var action = () => SupplierPayment.Create(
            paymentNo: "SP-001",
            supplierId: 1,
            amount: invalidAmount,
            paymentMethod: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("*المبلغ يجب أن يكون أكبر من الصفر*");
    }

    [Fact]
    public void Create_GivenNoPurchaseInvoiceId_ShouldBeNull()
    {
        var payment = SupplierPayment.Create(
            paymentNo: "SP-2026-000001",
            supplierId: 1,
            amount: 100m,
            paymentMethod: 1,
            purchaseInvoiceId: null
        );

        payment.PurchaseInvoiceId.Should().BeNull();
    }

    [Fact]
    public void Create_GivenNoReferenceNo_ShouldBeNull()
    {
        var payment = SupplierPayment.Create(
            paymentNo: "SP-2026-000001",
            supplierId: 1,
            amount: 100m,
            paymentMethod: 1,
            referenceNo: null
        );

        payment.ReferenceNo.Should().BeNull();
    }

    [Fact]
    public void Create_GivenAllPaymentMethods_ShouldSucceed()
    {
        foreach (byte method in new[] { 1, 2, 3 })
        {
            var payment = SupplierPayment.Create(
                paymentNo: $"SP-{method}",
                supplierId: 1,
                amount: 100m,
                paymentMethod: method
            );

            payment.PaymentMethod.Should().Be(method);
        }
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.5)]
    [InlineData(999999.99)]
    public void Create_GivenDecimalAmount_ShouldAccept(decimal amount)
    {
        var payment = SupplierPayment.Create(
            paymentNo: "SP-2026-000001",
            supplierId: 1,
            amount: amount,
            paymentMethod: 1
        );

        payment.Amount.Should().Be(amount);
    }
}