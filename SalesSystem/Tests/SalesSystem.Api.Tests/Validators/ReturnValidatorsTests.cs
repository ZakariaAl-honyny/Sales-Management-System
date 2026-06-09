using FluentValidation.TestHelper;
using SalesSystem.Api.Validators.Returns;
using SalesSystem.Contracts.Requests;
using Xunit;

namespace SalesSystem.Api.Tests.Validators;

[Trait("Category", "Validator")]
public class ReturnValidatorsTests
{
    #region CreateSalesReturnValidator Tests

    [Trait("Category", "Validator")]
    public class CreateSalesReturnValidatorTests
    {
        private readonly CreateSalesReturnValidator _validator = new();

        #region WarehouseId Validation

        [Theory]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(1, true)]
        [InlineData(100, true)]
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

        #region Items Validation

        [Fact]
        public void GivenEmptyItems_WhenValidating_ThenFails()
        {
            // Arrange
            var request = CreateValidRequest() with { Items = new List<ReturnItemRequest>() };

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

        #endregion

        #region Valid Request

        [Fact]
        public void GivenValidRequest_WhenValidating_ThenPasses()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
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

        private static CreateSalesReturnRequest CreateValidRequest() => new(
            SalesInvoiceId: 1,
            CustomerId: 1,
            WarehouseId: 1,
            ReturnDate: DateTime.UtcNow.AddDays(-1),
            Notes: "Return note",
            Items: new List<ReturnItemRequest>
            {
                new(1, 5, 100, 0)
            }
        );
    }

    #endregion

    #region CreatePurchaseReturnValidator Tests

    [Trait("Category", "Validator")]
    public class CreatePurchaseReturnValidatorTests
    {
        private readonly CreatePurchaseReturnValidator _validator = new();

        #region SupplierId Validation

        [Theory]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(1, true)]
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

        #region WarehouseId Validation

        [Theory]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(1, true)]
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

        #region Items Validation

        [Fact]
        public void GivenEmptyItems_WhenValidating_ThenFails()
        {
            // Arrange
            var request = CreateValidRequest() with { Items = new List<CreatePurchaseReturnItemRequest>() };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Items)
                .WithErrorMessage("يجب إضافة صنف واحد على الأقل");
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

        #region Valid Request

        [Fact]
        public void GivenValidRequest_WhenValidating_ThenPasses()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        private static CreatePurchaseReturnRequest CreateValidRequest() => new(
            PurchaseInvoiceId: 1,
            SupplierId: 1,
            WarehouseId: 1,
            LinkToInvoice: null,
            ReturnDate: DateTime.UtcNow.AddDays(-1),
            DiscountAmount: 0m,
            DiscountType: null,
            DiscountRate: null,
            CurrencyId: null,
            ExchangeRate: null,
            Notes: "Purchase return note",
            Items: new List<CreatePurchaseReturnItemRequest>
            {
                new(ProductId: 1, ProductUnitId: 1, Quantity: 5, UnitCost: 100, DiscountAmount: 0)
            }
        );
    }

    #endregion

    #region ReturnItemRequest Validation Tests

    [Trait("Category", "Validator")]
    public class ReturnItemRequestValidatorTests
    {
        private readonly CreateSalesReturnValidator _validator = new();

        #region ProductId Validation

        [Theory]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(1, true)]
        [InlineData(100, true)]
        public void GivenItemProductId_WhenValidating_ThenCorrectResult(int productId, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with
            {
                Items = new List<ReturnItemRequest>
                {
                    new(productId, 5, 100, 0)
                }
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor("Items[0].ProductId");
            else
                result.ShouldHaveValidationErrorFor("Items[0].ProductId")
                    .WithErrorMessage("يجب اختيار المنتج");
        }

        #endregion

        #region Quantity Validation

        [Theory]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(0.1, true)]
        [InlineData(1, true)]
        [InlineData(100.5, true)]
        public void GivenItemQuantity_WhenValidating_ThenCorrectResult(decimal quantity, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with
            {
                Items = new List<ReturnItemRequest>
                {
                    new(1, quantity, 100, 0)
                }
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor("Items[0].Quantity");
            else
                result.ShouldHaveValidationErrorFor("Items[0].Quantity")
                    .WithErrorMessage("الكمية يجب أن تكون أكبر من صفر");
        }

        #endregion

        #region UnitPrice Validation

        [Theory]
        [InlineData(-1, false)]
        [InlineData(-100, false)]
        [InlineData(0, true)]
        [InlineData(0.01, true)]
        [InlineData(100, true)]
        public void GivenItemUnitPrice_WhenValidating_ThenCorrectResult(decimal unitPrice, bool isValid)
        {
            // Arrange
            var request = CreateValidRequest() with
            {
                Items = new List<ReturnItemRequest>
                {
                    new(1, 5, unitPrice, 0)
                }
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            if (isValid)
                result.ShouldNotHaveValidationErrorFor("Items[0].UnitPrice");
            else
                result.ShouldHaveValidationErrorFor("Items[0].UnitPrice")
                    .WithErrorMessage("السعر لا يمكن أن يكون سالباً");
        }

        #endregion

        #region Multiple Items Validation

        [Fact]
        public void GivenMultipleValidItems_WhenValidating_ThenPasses()
        {
            // Arrange
            var request = CreateValidRequest() with
            {
                Items = new List<ReturnItemRequest>
                {
                    new(1, 5, 100, 0),
                    new(2, 10, 50, 5),
                    new(3, 1, 200, 0)
                }
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void GivenFirstItemValidSecondInvalid_WhenValidating_ThenFails()
        {
            // Arrange
            var request = CreateValidRequest() with
            {
                Items = new List<ReturnItemRequest>
                {
                    new(1, 5, 100, 0),
                    new(0, 5, 100, 0) // Invalid ProductId
                }
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor("Items[1].ProductId")
                .WithErrorMessage("يجب اختيار المنتج");
        }

        #endregion

        private static CreateSalesReturnRequest CreateValidRequest() => new(
            SalesInvoiceId: 1,
            CustomerId: 1,
            WarehouseId: 1,
            ReturnDate: DateTime.UtcNow.AddDays(-1),
            Notes: "Return note",
            Items: new List<ReturnItemRequest>
            {
                new(1, 5, 100, 0)
            }
        );
    }

    #endregion
}