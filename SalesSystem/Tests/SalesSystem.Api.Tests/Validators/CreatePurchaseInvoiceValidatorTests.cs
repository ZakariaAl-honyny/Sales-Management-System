using FluentValidation.TestHelper;
using SalesSystem.Api.Validators.Purchases;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Tests.Validators;

public class CreatePurchaseInvoiceValidatorTests
{
    private readonly CreatePurchaseInvoiceValidator _validator = new();

    #region Warehouse Validation

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void GivenWarehouseId_WhenValidating_ThenCorrectResult(int warehouseId, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { WarehouseId = warehouseId };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.WarehouseId);
        else
            result.ShouldHaveValidationErrorFor(x => x.WarehouseId)
                .WithErrorMessage("يجب اختيار المستودع");
    }

    #endregion

    #region Supplier Validation

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void GivenSupplierId_WhenValidating_ThenCorrectResult(int supplierId, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { SupplierId = supplierId };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.SupplierId);
        else
            result.ShouldHaveValidationErrorFor(x => x.SupplierId)
                .WithErrorMessage("يجب اختيار المورد");
    }

    #endregion

    #region Amount Validation

    [Fact]
    public void GivenZeroPaidAmount_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { PaidAmount = 0 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PaidAmount);
    }

    [Fact]
    public void GivenPositivePaidAmount_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { PaidAmount = 100.50m };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PaidAmount);
    }

    [Fact]
    public void GivenNegativePaidAmount_WhenValidating_ThenFails()
    {
        // Arrange
        var request = CreateValidRequest() with { PaidAmount = -0.01m };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaidAmount)
            .WithErrorMessage("المبلغ المدفوع لا يمكن أن يكون سالباً");
    }

    [Fact]
    public void GivenZeroDiscountAmount_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { DiscountAmount = 0 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountAmount);
    }

    [Fact]
    public void GivenPositiveDiscountAmount_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { DiscountAmount = 50m };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountAmount);
    }

    [Fact]
    public void GivenNegativeDiscountAmount_WhenValidating_ThenFails()
    {
        // Arrange
        var request = CreateValidRequest() with { DiscountAmount = -1 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountAmount)
            .WithErrorMessage("الخصم لا يمكن أن يكون سالباً");
    }

    [Fact]
    public void GivenZeroTaxAmount_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { TaxAmount = 0 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TaxAmount);
    }

    [Fact]
    public void GivenPositiveTaxAmount_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest() with { TaxAmount = 15m };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TaxAmount);
    }

    [Fact]
    public void GivenNegativeTaxAmount_WhenValidating_ThenFails()
    {
        // Arrange
        var request = CreateValidRequest() with { TaxAmount = -5 };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TaxAmount)
            .WithErrorMessage("الضريبة لا يمكن أن تكون سالبة");
    }

    #endregion

    #region Items Validation

    [Fact]
    public void GivenEmptyItems_WhenValidating_ThenFails()
    {
        // Arrange
        var request = CreateValidRequest() with { Items = new List<CreatePurchaseInvoiceItemRequest>() };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("يجب إضافة صنف واحد على الأقل");
    }

    [Fact]
    public void GivenNullItems_WhenValidating_ThenFails()
    {
        // Arrange
        var request = CreateValidRequest() with { Items = null! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("يجب إضافة صنف واحد على الأقل");
    }

    [Fact]
    public void GivenValidItems_WhenValidating_ThenPasses()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    #endregion

    #region Invoice Item Validation

    [Fact]
    public void GivenItemWithZeroProductId_WhenValidating_ThenFails()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { ProductId = 0 }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].ProductId")
            .WithErrorMessage("يجب اختيار المنتج");
    }

    [Fact]
    public void GivenItemWithNegativeProductId_WhenValidating_ThenFails()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { ProductId = -1 }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].ProductId")
            .WithErrorMessage("يجب اختيار المنتج");
    }

    [Fact]
    public void GivenItemWithValidProductId_WhenValidating_ThenPasses()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { ProductId = 1 }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor("Items[0].ProductId");
    }

    [Fact]
    public void GivenItemWithSmallPositiveQuantity_WhenValidating_ThenPasses()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { Quantity = 0.01m }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void GivenItemWithUnitQuantity_WhenValidating_ThenPasses()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { Quantity = 1 }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void GivenItemWithLargeQuantity_WhenValidating_ThenPasses()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { Quantity = 1000m }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void GivenItemWithZeroQuantity_WhenValidating_ThenFails()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { Quantity = 0 }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("الكمية يجب أن تكون أكبر من صفر");
    }

    [Fact]
    public void GivenItemWithNegativeQuantity_WhenValidating_ThenFails()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { Quantity = -1 }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("الكمية يجب أن تكون أكبر من صفر");
    }

    [Fact]
    public void GivenItemWithSmallNegativeQuantity_WhenValidating_ThenFails()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { Quantity = -0.01m }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("الكمية يجب أن تكون أكبر من صفر");
    }

    [Fact]
    public void GivenItemWithZeroUnitCost_WhenValidating_ThenPasses()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { UnitCost = 0 }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor("Items[0].UnitCost");
    }

    [Fact]
    public void GivenItemWithPositiveUnitCost_WhenValidating_ThenPasses()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { UnitCost = 80.25m }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor("Items[0].UnitCost");
    }

    [Fact]
    public void GivenItemWithNegativeUnitCost_WhenValidating_ThenFails()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { UnitCost = -0.01m }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].UnitCost")
            .WithErrorMessage("التكلفة لا يمكن أن تكون سالبة");
    }



    #endregion

    #region Multiple Items Validation

    [Fact]
    public void GivenMultipleValidItems_WhenValidating_ThenPasses()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem(productId: 1),
            CreateValidItem(productId: 2),
            CreateValidItem(productId: 3)
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GivenOneInvalidItemAmongValidItems_WhenValidating_ThenFailsForInvalidOnly()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem(productId: 1, quantity: 1),
            CreateValidItem(productId: 2, quantity: 0), // Invalid - zero quantity
            CreateValidItem(productId: 3, quantity: 2)
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[1].Quantity");
    }

    #endregion

    #region Notes Validation (Arabic Support)

    [Theory]
    [InlineData("ملاحظات عربية صحيحة", true)]      // Valid Arabic
    [InlineData("هذا منتج ممتاز جداً", true)]        // Long Arabic text
    [InlineData(" Arabic text ", true)]             // Mixed Arabic/English
    public void GivenArabicNotes_WhenValidating_ThenCorrectResult(string notes, bool isValid)
    {
        // Arrange
        var request = CreateValidRequest() with { Notes = notes };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Notes);
        else
            result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GivenVeryLargeQuantity_WhenValidating_ThenPasses()
    {
        // Arrange
        var items = new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem() with { Quantity = 999999.999m }
        };
        var request = CreateValidRequest() with { Items = items };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void GivenNegativePaidAmountWithValidItems_WhenValidating_ThenFails()
    {
        // Arrange
        var request = CreateValidRequest() with { PaidAmount = -50m };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaidAmount);
    }

    #endregion

    #region PaymentType Validation (TC-20-007)

    [Theory]
    [InlineData(1, true)]  // PaymentType.Cash
    [InlineData(2, true)]  // PaymentType.Credit
    [InlineData(3, true)]  // PaymentType.Mixed
    [InlineData(999, false)]
    public void GivenPaymentType_WhenValidating_ThenIsInEnumChecked(int paymentTypeValue, bool isValid)
    {
        var paymentType = (PaymentType)paymentTypeValue;
        var request = CreateValidRequest() with { PaymentType = paymentType };

        var result = _validator.TestValidate(request);

        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.PaymentType);
        else
            result.ShouldHaveValidationErrorFor(x => x.PaymentType)
                .WithErrorMessage("نوع الدفع غير صحيح");
    }

    #endregion

    private static CreatePurchaseInvoiceRequest CreateValidRequest() => new(
        WarehouseId: 1,
        SupplierId: 1,
        InvoiceNo: null,
        InvoiceDate: DateTime.UtcNow.AddDays(-1),
        DueDate: null,
        PaymentType: PaymentType.Cash,
        DiscountAmount: 0,
        TaxAmount: 0,
        PaidAmount: 100m,
        CurrencyId: null,
        ExchangeRate: null,
        Notes: "Test purchase invoice",
        Items: new List<CreatePurchaseInvoiceItemRequest>
        {
            CreateValidItem()
        }
    );

    private static CreatePurchaseInvoiceItemRequest CreateValidItem(
        int productId = 1,
        int productUnitId = 1,
        decimal quantity = 1,
        decimal unitCost = 100m
    ) => new(
        ProductId: productId,
        ProductUnitId: productUnitId,
        Quantity: quantity,
        UnitCost: unitCost
    );
}