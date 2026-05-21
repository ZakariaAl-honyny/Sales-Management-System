using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Infrastructure.Printing.A4;

namespace SalesSystem.Infrastructure.Tests.Printing;

public class A4InvoiceDocumentTests
{
    public A4InvoiceDocumentTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static InvoicePrintDto CreateBaseInvoice() => new()
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
        IsTaxInclusive = true,
    };

    // ─────────────────────────────────────────────
    // Constructor & Metadata
    // ─────────────────────────────────────────────

    [Fact]
    public void Constructor_ShouldStoreData()
    {
        var doc = new A4InvoiceDocument(CreateBaseInvoice());
        doc.Should().NotBeNull();
    }

    [Fact]
    public void GetMetadata_ShouldReturnCorrectTitle()
    {
        var doc = new A4InvoiceDocument(CreateBaseInvoice());
        var meta = doc.GetMetadata();
        meta.Title.Should().Be("فاتورة INV-2026-000001");
    }

    [Fact]
    public void GetMetadata_ShouldReturnStoreNameAsAuthor()
    {
        var doc = new A4InvoiceDocument(CreateBaseInvoice());
        var meta = doc.GetMetadata();
        meta.Author.Should().Be("متجر الاختبار");
    }

    [Fact]
    public void GetMetadata_ShouldSetCreationDate()
    {
        var doc = new A4InvoiceDocument(CreateBaseInvoice());
        var meta = doc.GetMetadata();
        meta.CreationDate.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(10));
    }

    // ─────────────────────────────────────────────
    // Document Generation (Compose)
    // ─────────────────────────────────────────────

    [Fact]
    public void Compose_WithMinimalData_ShouldGeneratePdfSuccessfully()
    {
        var doc = new A4InvoiceDocument(CreateBaseInvoice());
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithMinimalData_ShouldProduceNonEmptyPdf()
    {
        var doc = new A4InvoiceDocument(CreateBaseInvoice());
        var pdfBytes = doc.GeneratePdf();
        pdfBytes.Should().NotBeNullOrEmpty();
        pdfBytes.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public void Compose_WithFullStoreInfo_ShouldGeneratePdf()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار", StorePhone = "0555555555",
            StoreAddress = "شارع الملك فهد، الرياض", StoreTaxNumber = "310123456700003",
            Notes = "ملاحظات اختبار للفاتورة",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج أ", "قطعة", 2m, 50m, 0m, 100m),
            },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithLogoBytes_ShouldGeneratePdf()
    {
        var logoBytes = CreateMinimalPngLogo();
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار", LogoBytes = logoBytes,
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج أ", "قطعة", 2m, 50m, 0m, 100m),
            },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithNoLogo_ShouldGeneratePdfGracefully()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار", LogoBytes = null,
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج أ", "قطعة", 2m, 50m, 0m, 100m),
            },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithCustomerInfo_ShouldGeneratePdf()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            CustomerPhone = "0566666666", CustomerAddress = "حي النزهة، جدة",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج أ", "قطعة", 2m, 50m, 0m, 100m),
            },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithChangeAmount_ShouldGeneratePdf()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج أ", "قطعة", 2m, 50m, 0m, 100m),
            },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً",
            AmountPaid = 250m, ChangeAmount = 20m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithTaxExclusive_ShouldGeneratePdf()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج أ", "قطعة", 2m, 50m, 0m, 100m),
            },
            SubTotal = 100m, TaxAmount = 15m, GrandTotal = 115m, TaxRate = 15m,
            IsTaxInclusive = false,
            PaymentMethod = "نقداً", AmountPaid = 115m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithNoItems_ShouldGenerateEmptyPdf()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>(),
            SubTotal = 0m, GrandTotal = 0m, PaymentMethod = "نقداً", AmountPaid = 0m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithSingleItem_ShouldGeneratePdf()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج واحد", "كرتون", 5m, 200m, 0m, 1000m),
            },
            SubTotal = 1000m, GrandTotal = 1000m, PaymentMethod = "نقداً", AmountPaid = 1000m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithManyItems_ShouldGeneratePdf()
    {
        var items = Enumerable.Range(1, 20).Select(i =>
            new InvoiceItemPrintDto($"منتج رقم {i}", "قطعة", 1m, 10m * i, 0m, 10m * i))
            .ToList();

        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            Items = items,
            SubTotal = items.Sum(x => x.Total),
            GrandTotal = items.Sum(x => x.Total),
            PaymentMethod = "نقداً", AmountPaid = items.Sum(x => x.Total),
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    // ─────────────────────────────────────────────
    // Invoice type labels
    // ─────────────────────────────────────────────

    [Theory]
    [InlineData(InvoiceTypePrint.Sales)]
    [InlineData(InvoiceTypePrint.Purchase)]
    [InlineData(InvoiceTypePrint.SalesReturn)]
    [InlineData(InvoiceTypePrint.PurchaseReturn)]
    public void Compose_WithInvoiceType_ShouldGeneratePdf(InvoiceTypePrint type)
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج أ", "قطعة", 2m, 50m, 0m, 100m),
            },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = type,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    // ─────────────────────────────────────────────
    // Edge cases
    // ─────────────────────────────────────────────

    [Fact]
    public void Compose_WithEmptyStoreName_ShouldGeneratePdf()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = string.Empty,
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج أ", "قطعة", 2m, 50m, 0m, 100m),
            },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithVeryLongStoreName_ShouldGeneratePdf()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = new string('أ', 200),
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج أ", "قطعة", 2m, 50m, 0m, 100m),
            },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithVeryLongProductNames_ShouldGeneratePdf()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new(new string('ب', 100), "علبة", 1m, 100m, 0m, 100m),
            },
            SubTotal = 100m, GrandTotal = 100m, PaymentMethod = "نقداً", AmountPaid = 100m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    [Fact]
    public void Compose_WithZeroGrandTotal_ShouldGeneratePdf()
    {
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1, InvoiceNumber = "INV-2026-000001",
            InvoiceDate = new DateTime(2026, 5, 21),
            StoreName = "متجر الاختبار",
            CustomerOrSupplierName = "عميل اختبار",
            Items = new List<InvoiceItemPrintDto>
            {
                new("منتج مجاني", "قطعة", 0m, 0m, 0m, 0m),
            },
            SubTotal = 0m, GrandTotal = 0m, PaymentMethod = "نقداً", AmountPaid = 0m,
            InvoiceType = InvoiceTypePrint.Sales,
        };
        var doc = new A4InvoiceDocument(invoice);
        Action act = () => doc.GeneratePdf();
        act.Should().NotThrow();
    }

    // ─── Helpers ───────────────────────────────

    private static byte[] CreateMinimalPngLogo()
    {
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==");
    }
}
