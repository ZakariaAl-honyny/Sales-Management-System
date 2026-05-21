using FluentAssertions;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Tests.Entities;

public class StockTransferTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateStockTransfer()
    {
        var transfer = StockTransfer.Create(
            transferNo: "TRF-2026-000001",
            fromWarehouseId: 1,
            toWarehouseId: 2,
            notes: "Stock transfer",
            transferDate: new DateTime(2026, 1, 15)
        );

        transfer.TransferNo.Should().Be("TRF-2026-000001");
        transfer.FromWarehouseId.Should().Be(1);
        transfer.ToWarehouseId.Should().Be(2);
        transfer.Notes.Should().Be("Stock transfer");
        transfer.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_GivenInvalidTransferNo_ShouldThrowDomainException(string? invalidTransferNo)
    {
        var action = () => StockTransfer.Create(
            transferNo: invalidTransferNo!,
            fromWarehouseId: 1,
            toWarehouseId: 2
        );

        action.Should().Throw<DomainException>()
            .WithMessage("رقم التحويل مطلوب.");
    }

    [Fact]
    public void Create_GivenFromWarehouseIdIsZero_ShouldThrowDomainException()
    {
        var action = () => StockTransfer.Create(
            transferNo: "TRF-001",
            fromWarehouseId: 0,
            toWarehouseId: 2
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع المصدر مطلوب.");
    }

    [Fact]
    public void Create_GivenToWarehouseIdIsZero_ShouldThrowDomainException()
    {
        var action = () => StockTransfer.Create(
            transferNo: "TRF-001",
            fromWarehouseId: 1,
            toWarehouseId: 0
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المستودع الوجهة مطلوب.");
    }

    [Fact]
    public void Create_GivenSameWarehouseIds_ShouldThrowDomainException()
    {
        var action = () => StockTransfer.Create(
            transferNo: "TRF-001",
            fromWarehouseId: 1,
            toWarehouseId: 1
        );

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن التحويل إلى نفس المستودع.");
    }

    [Fact]
    public void AddItem_GivenValidData_ShouldAddItem()
    {
        var transfer = StockTransfer.Create(
            transferNo: "TRF-2026-000001",
            fromWarehouseId: 1,
            toWarehouseId: 2,
            notes: "Product transfer"
        );

        transfer.AddItem(productId: 1, quantity: 10m, notes: "Product transfer");

        transfer.Items.Should().HaveCount(1);
        transfer.Items.First().ProductId.Should().Be(1);
        transfer.Items.First().Quantity.Should().Be(10m);
    }

    [Fact]
    public void AddItem_GivenNonDraftTransfer_ShouldThrowDomainException()
    {
        var transfer = StockTransfer.Create(
            transferNo: "TRF-2026-000001",
            fromWarehouseId: 1,
            toWarehouseId: 2
        );

        transfer.AddItem(productId: 1, quantity: 10m);
        transfer.Post();

        var action = () => transfer.AddItem(productId: 2, quantity: 5m);

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن إضافة أصناف لتحويل غير مسودة.");
    }

    [Fact]
    public void Post_GivenDraftTransfer_ShouldTransitionToPosted()
    {
        var transfer = StockTransfer.Create(
            transferNo: "TRF-2026-000001",
            fromWarehouseId: 1,
            toWarehouseId: 2
        );

        transfer.AddItem(productId: 1, quantity: 10m);
        transfer.Post();

        transfer.Status.Should().Be(InvoiceStatus.Posted);
    }

    [Fact]
    public void Post_GivenEmptyTransfer_ShouldThrowDomainException()
    {
        var transfer = StockTransfer.Create(
            transferNo: "TRF-2026-000001",
            fromWarehouseId: 1,
            toWarehouseId: 2
        );

        var action = () => transfer.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("لا يمكن ترحيل تحويل بدون أصناف.");
    }

    [Fact]
    public void Post_GivenAlreadyPostedTransfer_ShouldThrowDomainException()
    {
        var transfer = StockTransfer.Create(
            transferNo: "TRF-2026-000001",
            fromWarehouseId: 1,
            toWarehouseId: 2
        );

        transfer.AddItem(productId: 1, quantity: 10m);
        transfer.Post();

        var action = () => transfer.Post();

        action.Should().Throw<DomainException>()
            .WithMessage("فقط التحويلات المسودة يمكن ترحيلها.");
    }

    [Fact]
    public void Cancel_GivenDraftTransfer_ShouldTransitionToCancelled()
    {
        var transfer = StockTransfer.Create(
            transferNo: "TRF-2026-000001",
            fromWarehouseId: 1,
            toWarehouseId: 2
        );

        transfer.Cancel();

        transfer.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenPostedTransfer_ShouldTransitionToCancelled()
    {
        var transfer = StockTransfer.Create(
            transferNo: "TRF-2026-000001",
            fromWarehouseId: 1,
            toWarehouseId: 2
        );

        transfer.AddItem(productId: 1, quantity: 10m);
        transfer.Post();
        transfer.Cancel();

        transfer.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Cancel_GivenAlreadyCancelled_ShouldThrowDomainException()
    {
        var transfer = StockTransfer.Create(
            transferNo: "TRF-2026-000001",
            fromWarehouseId: 1,
            toWarehouseId: 2
        );

        transfer.Cancel();

        var action = () => transfer.Cancel();

        action.Should().Throw<DomainException>()
            .WithMessage("التحويل ملغى بالفعل.");
    }

    [Fact]
    public void AddItem_MultipleItems_ShouldAddAll()
    {
        var transfer = StockTransfer.Create(
            transferNo: "TRF-2026-000001",
            fromWarehouseId: 1,
            toWarehouseId: 2
        );

        transfer.AddItem(productId: 1, quantity: 10m);
        transfer.AddItem(productId: 2, quantity: 20m);
        transfer.AddItem(productId: 3, quantity: 5m);

        transfer.Items.Should().HaveCount(3);
    }
}

public class StockTransferItemTests
{
    [Fact]
    public void Create_GivenValidData_ShouldCreateItem()
    {
        var item = StockTransferItem.Create(
            productId: 1,
            quantity: 10m,
            notes: "Transfer notes"
        );

        item.ProductId.Should().Be(1);
        item.Quantity.Should().Be(10m);
        item.Notes.Should().Be("Transfer notes");
    }

    [Fact]
    public void Create_GivenProductIdIsZero_ShouldThrowDomainException()
    {
        var action = () => StockTransferItem.Create(
            productId: 0,
            quantity: 10m
        );

        action.Should().Throw<DomainException>()
            .WithMessage("المنتج مطلوب.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_GivenInvalidQuantity_ShouldThrowDomainException(decimal invalidQuantity)
    {
        var action = () => StockTransferItem.Create(
            productId: 1,
            quantity: invalidQuantity
        );

        action.Should().Throw<DomainException>()
            .WithMessage("الكمية يجب أن تكون أكبر من الصفر.");
    }

    [Fact]
    public void Create_GivenNoNotes_ShouldHaveNullNotes()
    {
        var item = StockTransferItem.Create(
            productId: 1,
            quantity: 10m
        );

        item.Notes.Should().BeNull();
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.5)]
    [InlineData(999.999)]
    public void Create_GivenDecimalQuantity_ShouldAccept(decimal quantity)
    {
        var item = StockTransferItem.Create(
            productId: 1,
            quantity: quantity
        );

        item.Quantity.Should().Be(quantity);
    }
}