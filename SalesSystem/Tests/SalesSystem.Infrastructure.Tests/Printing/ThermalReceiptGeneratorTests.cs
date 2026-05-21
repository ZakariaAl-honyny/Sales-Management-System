using System.Text;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Infrastructure.Printing.Thermal;

namespace SalesSystem.Infrastructure.Tests.Printing;

public class ThermalReceiptGeneratorTests
{
    static ThermalReceiptGeneratorTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
    private static InvoicePrintDto CreateInvoice() => new()
    {
        InvoiceId = 1,
        InvoiceNumber = "INV-2026-000001",
        InvoiceDate = new DateTime(2026, 5, 21),
        StoreName = "متجر الاختبار",
        CustomerOrSupplierName = "عميل اختبار",
        Items = new List<InvoiceItemPrintDto>
        {
            new("منتج أ", "قطعة", 2m, 50m, 0m, 100m),
            new("منتج ب", "قطعة", 1m, 150m, 10m, 140m),
        },
        SubTotal = 240m,
        DiscountAmount = 10m,
        TaxAmount = 0m,
        GrandTotal = 230m,
        PaymentMethod = "نقداً",
        AmountPaid = 230m,
        InvoiceType = InvoiceTypePrint.Sales,
        TaxRate = 15m,
    };

    // ─────────────────────────────────────────────
    // GenerateEscPosCommands — basic structure
    // ─────────────────────────────────────────────

    [Fact]
    public void GenerateEscPosCommands_ShouldReturnNonEmptyByteArray()
    {
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(CreateInvoice());
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateEscPosCommands_ShouldStartWithInitializeCommand()
    {
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(CreateInvoice());
        result[0].Should().Be(0x1B);
        result[1].Should().Be(0x40);
    }

    [Fact]
    public void GenerateEscPosCommands_ShouldEndWithCutPaperCommand()
    {
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(CreateInvoice());
        result[^4].Should().Be(0x1D);
        result[^3].Should().Be(0x56);
        result[^2].Should().Be(0x42);
        result[^1].Should().Be(0x00);
    }

    [Fact]
    public void GenerateEscPosCommands_ShouldIncludeStoreName()
    {
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(CreateInvoice());
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("متجر الاختبار");
    }

    [Fact]
    public void GenerateEscPosCommands_ShouldIncludeInvoiceNumber()
    {
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(CreateInvoice());
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("INV-2026-000001");
    }

    [Fact]
    public void GenerateEscPosCommands_ShouldIncludeCustomerName()
    {
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(CreateInvoice());
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("عميل اختبار");
    }

    [Fact]
    public void GenerateEscPosCommands_ShouldIncludeGrandTotal()
    {
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(CreateInvoice());
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("230.00");
    }

    // ─────────────────────────────────────────────
    // Store info inclusion
    // ─────────────────────────────────────────────

    [Fact]
    public void GenerateEscPosCommands_WithStorePhone_ShouldIncludePhone()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار", StorePhone = "0555555555",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto> { new("منتج", "قطعة", 1m, 100m, 0m, 100m) },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(invoice);
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("0555555555");
    }

    [Fact]
    public void GenerateEscPosCommands_WithStoreAddress_ShouldIncludeAddress()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار", StoreAddress = "شارع الملك فهد",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto> { new("منتج", "قطعة", 1m, 100m, 0m, 100m) },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(invoice);
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("شارع الملك فهد");
    }

    [Fact]
    public void GenerateEscPosCommands_WithStoreTaxNumber_ShouldIncludeTaxNumber()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار", StoreTaxNumber = "310123456700003",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto> { new("منتج", "قطعة", 1m, 100m, 0m, 100m) },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(invoice);
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("310123456700003");
    }

    // ─────────────────────────────────────────────
    // Financial details
    // ─────────────────────────────────────────────

    [Fact]
    public void GenerateEscPosCommands_WithChangeAmount_ShouldIncludeChange()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto> { new("منتج", "قطعة", 1m, 100m, 0m, 100m) },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً",
            AmountPaid = 250m, ChangeAmount = 20m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(invoice);
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("20.00");
    }

    [Fact]
    public void GenerateEscPosCommands_WithTax_ShouldIncludeTaxBreakdown()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto> { new("منتج", "قطعة", 1m, 100m, 0m, 100m) },
            SubTotal = 100m, TaxAmount = 15m, GrandTotal = 115m, TaxRate = 15m,
            PaymentMethod = "نقداً", AmountPaid = 115m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(invoice);
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("15.00");
    }

    // ─────────────────────────────────────────────
    // Item-level details
    // ─────────────────────────────────────────────

    [Fact]
    public void GenerateEscPosCommands_ShouldIncludeProductNames()
    {
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(CreateInvoice());
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("منتج أ");
        text.Should().Contain("منتج ب");
    }

    [Fact]
    public void GenerateEscPosCommands_WithItemDiscount_ShouldIncludeItemDiscount()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج بعرض", "قطعة", 1m, 200m, 50m, 150m),
            },
            SubTotal = 150m, GrandTotal = 150m,
            PaymentMethod = "نقداً", AmountPaid = 150m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(invoice);
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("50.00");
    }

    // ─────────────────────────────────────────────
    // Invoice types
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData(InvoiceTypePrint.Sales)]
    [InlineData(InvoiceTypePrint.Purchase)]
    [InlineData(InvoiceTypePrint.SalesReturn)]
    [InlineData(InvoiceTypePrint.PurchaseReturn)]
    public void GenerateEscPosCommands_WithDifferentInvoiceTypes_ShouldProduceOutput(InvoiceTypePrint type)
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "TEST-001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر",
            CustomerOrSupplierName = "عميل",
            Items = new List<InvoiceItemPrintDto> { new("منتج", "قطعة", 1m, 100m, 0m, 100m) },
            SubTotal = 100m, GrandTotal = 100m,
            PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = type,
        };
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(invoice);
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeGreaterThan(50);
    }

    // ─────────────────────────────────────────────
    // Edge cases
    // ─────────────────────────────────────────────

    [Fact]
    public void GenerateEscPosCommands_WithEmptyItems_ShouldStillPrint()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "TEST-001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر",
            CustomerOrSupplierName = "عميل",
            Items = new List<InvoiceItemPrintDto>(),
            SubTotal = 0m, GrandTotal = 0m,
            PaymentMethod = "نقداً", AmountPaid = 0m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(invoice);
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeGreaterThan(30);
    }

    [Fact]
    public void GenerateEscPosCommands_WithEmptyStoreName_ShouldNotCrash()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "TEST-001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = string.Empty,
            CustomerOrSupplierName = "عميل",
            Items = new List<InvoiceItemPrintDto> { new("منتج", "قطعة", 1m, 100m, 0m, 100m) },
            SubTotal = 100m, GrandTotal = 100m,
            PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(invoice);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateEscPosCommands_WithVeryLongStoreName_ShouldNotCrash()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "TEST-001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = new string('أ', 100),
            CustomerOrSupplierName = "عميل",
            Items = new List<InvoiceItemPrintDto> { new("منتج", "قطعة", 1m, 100m, 0m, 100m) },
            SubTotal = 100m, GrandTotal = 100m,
            PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(invoice);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateEscPosCommands_WithManyItems_ShouldProduceOutput()
    {
        var items = Enumerable.Range(1, 30).Select(i =>
            new InvoiceItemPrintDto($"منتج {i}", "قطعة", 1m, 10m, 0m, 10m))
            .ToList();
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "TEST-001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر",
            CustomerOrSupplierName = "عميل",
            Items = items,
            SubTotal = items.Sum(x => x.Total),
            GrandTotal = items.Sum(x => x.Total),
            PaymentMethod = "نقداً", AmountPaid = items.Sum(x => x.Total),
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(invoice);
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeGreaterThan(200);
    }

    [Fact]
    public void GenerateEscPosCommands_OutputShouldBeInWindows1256()
    {
        var generator = new ThermalReceiptGenerator();
        var result = generator.GenerateEscPosCommands(CreateInvoice());
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.Should().Contain("متجر");
        var utf8Bytes = Encoding.UTF8.GetBytes(text);
        utf8Bytes.Length.Should().NotBe(result.Length);
    }

    // ─────────────────────────────────────────────
    // Text formatting helpers (via reflection)
    // ─────────────────────────────────────────────

    [Fact]
    public void FormatTwoColumns_ShouldFillToLineWidth()
    {
        var result = CallPrivateFormatTwoColumns("الاسم:", "أحمد");
        result.Length.Should().Be(42);
    }

    [Fact]
    public void FormatTwoColumns_WithLongLabel_ShouldNotExceedLineWidth()
    {
        var result = CallPrivateFormatTwoColumns("رقم الفاتورة:", "INV-2026-000001");
        result.Length.Should().Be(42);
    }

    [Fact]
    public void TruncateRight_WithinLimit_ReturnsFullText()
    {
        var result = CallPrivateTruncateRight("Hello", 10);
        result.Should().Be("Hello");
    }

    [Fact]
    public void TruncateRight_ExceedsLimit_Truncates()
    {
        var result = CallPrivateTruncateRight("Hello World", 5);
        result.Should().Be("Hello");
    }

    [Fact]
    public void TruncateCenter_WithinLimit_ReturnsFullText()
    {
        var result = CallPrivateTruncateCenter("Hello", 10);
        result.Should().Be("Hello");
    }

    [Fact]
    public void TruncateCenter_ExceedsLimit_TruncatesWithEllipsis()
    {
        var result = CallPrivateTruncateCenter("Hello World This Is Long", 20);
        // (20-3)/2 = 8 using integer division => 8 + "..." + 8 = 19 chars max
        result.Length.Should().BeLessOrEqualTo(20);
        result.Should().Contain("...");
    }

    // ─── Private method helpers (via reflection) ───

    private static string CallPrivateFormatTwoColumns(string label, string value)
    {
        var method = typeof(ThermalReceiptGenerator)
            .GetMethod("FormatTwoColumns",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var instance = new ThermalReceiptGenerator();
        return (string)method!.Invoke(instance, new object[] { label, value })!;
    }

    private static string CallPrivateTruncateRight(string text, int maxLength)
    {
        var method = typeof(ThermalReceiptGenerator)
            .GetMethod("TruncateRight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (string)method!.Invoke(null, new object[] { text, maxLength })!;
    }

    private static string CallPrivateTruncateCenter(string text, int maxLength)
    {
        var method = typeof(ThermalReceiptGenerator)
            .GetMethod("TruncateCenter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (string)method!.Invoke(null, new object[] { text, maxLength })!;
    }
}
