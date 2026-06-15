using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class SupplierPaymentTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateSupplierPayment()
    {
        var payment = SupplierPayment.Create(
            paymentNo: 1,
            supplierId: 1,
            cashBoxId: 1,
            currencyId: (short)1,
            amount: 500m,
            paymentMethod: PaymentMethod.Cash,
            referenceNo: "REF001",
            notes: "Payment for invoice"
        );

        payment.PaymentNo.Should().Be(1);
        payment.SupplierId.Should().Be(1);
        payment.CashBoxId.Should().Be(1);
        payment.CurrencyId.Should().Be(1);
        payment.Amount.Should().Be(500m);
        payment.PaymentMethod.Should().Be(PaymentMethod.Cash);
        payment.ReferenceNo.Should().Be("REF001");
        payment.Notes.Should().Be("Payment for invoice");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_GivenInvalidPaymentNo_ShouldThrowDomainException(int invalidPaymentNo)
    {
        var action = () => SupplierPayment.Create(
            paymentNo: invalidPaymentNo,
            supplierId: 1,
            cashBoxId: 1,
            currencyId: (short)1,
            amount: 100m,
            paymentMethod: PaymentMethod.Cash
        );

        action.Should().Throw<DomainException>()
            .WithMessage("رقم السداد مطلوب.");
    }

    [Fact]
    public void Create_GivenSupplierIdIsZero_ShouldThrowDomainException()
    {
        var action = () => SupplierPayment.Create(
            paymentNo: 1,
            supplierId: 0,
            cashBoxId: 1,
            currencyId: (short)1,
            amount: 100m,
            paymentMethod: PaymentMethod.Cash
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المورد مطلوب.");
    }

    [Fact]
    public void Create_GivenCashBoxIdIsZero_ShouldThrowDomainException()
    {
        var action = () => SupplierPayment.Create(
            paymentNo: 1,
            supplierId: 1,
            cashBoxId: 0,
            currencyId: (short)1,
            amount: 100m,
            paymentMethod: PaymentMethod.Cash
        );

        action.Should().Throw<DomainException>()
            .WithMessage("الصندوق مطلوب.");
    }

    [Fact]
    public void Create_GivenCurrencyIdIsZero_ShouldThrowDomainException()
    {
        var action = () => SupplierPayment.Create(
            paymentNo: 1,
            supplierId: 1,
            cashBoxId: 1,
            currencyId: 0,
            amount: 100m,
            paymentMethod: PaymentMethod.Cash
        );

        action.Should().Throw<DomainException>()
            .WithMessage("العملة مطلوبة.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenInvalidAmount_ShouldThrowDomainException(decimal invalidAmount)
    {
        var action = () => SupplierPayment.Create(
            paymentNo: 1,
            supplierId: 1,
            cashBoxId: 1,
            currencyId: (short)1,
            amount: invalidAmount,
            paymentMethod: PaymentMethod.Cash
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المبلغ يجب أن يكون أكبر من الصفر.");
    }

    [Fact]
    public void Create_GivenNoReferenceNo_ShouldBeNull()
    {
        var payment = SupplierPayment.Create(
            paymentNo: 1,
            supplierId: 1,
            cashBoxId: 1,
            currencyId: (short)1,
            amount: 100m,
            paymentMethod: PaymentMethod.Cash,
            referenceNo: null
        );

        payment.ReferenceNo.Should().BeNull();
    }

    [Fact]
    public void Create_GivenAllPaymentMethods_ShouldSucceed()
    {
        var cashBoxId = 1;
        var currencyId = (short)1;
        foreach (PaymentMethod method in Enum.GetValues<PaymentMethod>())
        {
            var payment = SupplierPayment.Create(
                paymentNo: (int)method + 1,
                supplierId: 1,
                cashBoxId: cashBoxId,
                currencyId: currencyId,
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
            paymentNo: 100,
            supplierId: 1,
            cashBoxId: 1,
            currencyId: (short)1,
            amount: amount,
            paymentMethod: PaymentMethod.Cash
        );

        payment.Amount.Should().Be(amount);
    }

    [Fact]
    public void Post_GivenDraftPayment_ShouldTransitionToPosted()
    {
        var payment = SupplierPayment.Create(
            paymentNo: 1,
            supplierId: 1,
            cashBoxId: 1,
            currencyId: (short)1,
            amount: 500m,
            paymentMethod: PaymentMethod.Cash
        );

        payment.Post();

        payment.Status.Should().Be(InvoiceStatus.Posted);
    }

    [Fact]
    public void Cancel_GivenPostedPayment_ShouldTransitionToCancelled()
    {
        var payment = SupplierPayment.Create(
            paymentNo: 1,
            supplierId: 1,
            cashBoxId: 1,
            currencyId: (short)1,
            amount: 500m,
            paymentMethod: PaymentMethod.Cash
        );

        payment.Post();
        payment.Cancel();

        payment.Status.Should().Be(InvoiceStatus.Cancelled);
    }
}
