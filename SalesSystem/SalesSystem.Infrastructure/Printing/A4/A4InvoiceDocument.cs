using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SalesSystem.Application.Printing.Contracts;

namespace SalesSystem.Infrastructure.Printing.A4;

public class A4InvoiceDocument : IDocument
{
    private readonly InvoicePrintDto _data;

    // ─── Design Constants ─────────────────────────
    private const string FontFamily = "Arial";
    private const float HeaderFontSize = 20f;
    private const float SubHeaderFontSize = 13f;
    private const float BodyFontSize = 10f;
    private const float SmallFontSize = 8f;

    private const string PrimaryColor = "#1565C0";
    private const string AccentColor = "#E3F2FD";
    private const string WhiteColor = "#FFFFFF";
    private const string TextColor = "#212121";
    private const string MutedColor = "#757575";
    private const string SuccessColor = "#2E7D32";

    public A4InvoiceDocument(InvoicePrintDto data)
    {
        _data = data;
    }

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = $"فاتورة {_data.InvoiceNumber}",
        Author = _data.StoreName,
        CreationDate = DateTimeOffset.Now
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(style =>
                style.FontFamily(FontFamily).FontSize(BodyFontSize));
            page.ContentFromRightToLeft();

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // ═══════════════════════════════════════════════
    // HEADER: Logo + Store Info + Invoice Badge
    // ═══════════════════════════════════════════════
    private void ComposeHeader(IContainer container)
    {
        container
            .BorderBottom(2).BorderColor(PrimaryColor)
            .PaddingBottom(10)
            .Row(row =>
            {
                // Left: Logo (or placeholder)
                if (_data.LogoBytes != null)
                {
                    row.ConstantItem(80).Height(80)
                        .Padding(4)
                        .Image(_data.LogoBytes)
                        .FitArea();
                }
                else
                {
                    row.ConstantItem(80).Height(80)
                        .Background(PrimaryColor)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(_data.StoreName.Length > 0
                            ? _data.StoreName[0].ToString()
                            : "م")
                        .FontSize(36).FontColor("#FFFFFF").Bold();
                }

                // Center: Store info
                row.RelativeItem().PaddingHorizontal(12).Column(col =>
                {
                    col.Item()
                        .Text(_data.StoreName)
                        .FontSize(HeaderFontSize)
                        .FontColor(PrimaryColor)
                        .Bold();

                    if (!string.IsNullOrWhiteSpace(_data.StorePhone))
                        col.Item().Text($"هاتف: {_data.StorePhone}")
                            .FontSize(SmallFontSize).FontColor(MutedColor);

                    if (!string.IsNullOrWhiteSpace(_data.StoreAddress))
                        col.Item().Text($"العنوان: {_data.StoreAddress}")
                            .FontSize(SmallFontSize).FontColor(MutedColor);

                    if (!string.IsNullOrWhiteSpace(_data.StoreTaxNumber))
                        col.Item().Text($"الرقم الضريبي: {_data.StoreTaxNumber}")
                            .FontSize(SmallFontSize).FontColor(MutedColor);
                });

                // Right: Invoice badge
                row.ConstantItem(140).Background(AccentColor)
                    .Padding(10)
                    .Column(col =>
                    {
                        col.Item().AlignCenter()
                            .Text(GetInvoiceTypeLabel())
                            .FontSize(SubHeaderFontSize)
                            .FontColor(PrimaryColor).Bold();

                        col.Item().AlignCenter()
                            .Text(_data.InvoiceNumber)
                            .FontSize(14).Bold();

                        col.Item().AlignCenter()
                            .Text(_data.InvoiceDate.ToString("dd/MM/yyyy"))
                            .FontSize(SmallFontSize).FontColor(MutedColor);

                        col.Item().AlignCenter()
                            .Text(_data.InvoiceDate.ToString("HH:mm"))
                            .FontSize(SmallFontSize).FontColor(MutedColor);
                    });
            });
    }

    // ═══════════════════════════════════════════════
    // CONTENT: Customer Info + Items Table + Totals
    // ═══════════════════════════════════════════════
    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // Customer / Supplier info
            col.Item().PaddingVertical(12).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor("#E0E0E0")
                    .Padding(10).Column(c =>
                    {
                        c.Item().Text("بيانات العميل")
                            .FontSize(SmallFontSize).FontColor(MutedColor).Bold();

                        c.Item().Text(_data.CustomerOrSupplierName)
                            .FontSize(SubHeaderFontSize).Bold();

                        if (!string.IsNullOrWhiteSpace(_data.CustomerPhone))
                            c.Item().Text($"هاتف: {_data.CustomerPhone}")
                                .FontSize(SmallFontSize);

                        if (!string.IsNullOrWhiteSpace(_data.CustomerAddress))
                            c.Item().Text($"العنوان: {_data.CustomerAddress}")
                                .FontSize(SmallFontSize);
                    });

                row.ConstantItem(20);

                row.RelativeItem().Border(1).BorderColor("#E0E0E0")
                    .Padding(10).Column(c =>
                    {
                        c.Item().Text("تفاصيل الدفع")
                            .FontSize(SmallFontSize).FontColor(MutedColor).Bold();
                        c.Item().Text($"طريقة الدفع: {_data.PaymentMethod}");
                        c.Item().Text($"المبلغ المدفوع: {_data.AmountPaid:N2} ر.س");
                        if (_data.ChangeAmount > 0)
                            c.Item().Text($"الباقي: {_data.ChangeAmount:N2} ر.س")
                                .FontColor(SuccessColor);
                    });
            });

            // Items table
            col.Item().Element(ComposeItemsTable);

            // Totals section
            col.Item().PaddingTop(12).AlignRight()
                .Width(280).Element(ComposeTotalsSection);

            // Notes
            if (!string.IsNullOrWhiteSpace(_data.Notes))
            {
                col.Item().PaddingTop(10)
                    .Border(1).BorderColor("#FFF9C4")
                    .Background("#FFFDE7")
                    .Padding(8)
                    .Column(c =>
                    {
                        c.Item().Text("ملاحظات").Bold().FontSize(SmallFontSize);
                        c.Item().Text(_data.Notes).FontSize(BodyFontSize);
                    });
            }
        });
    }

    private void ComposeItemsTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(25);
                cols.RelativeColumn(4);
                cols.RelativeColumn(1.5f);
                cols.RelativeColumn(1.5f);
                cols.RelativeColumn(2);
                cols.RelativeColumn(1.5f);
                cols.RelativeColumn(2);
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("#");
                header.Cell().Element(HeaderCell).Text("المنتج");
                header.Cell().Element(HeaderCell).AlignCenter().Text("الوحدة");
                header.Cell().Element(HeaderCell).AlignCenter().Text("الكمية");
                header.Cell().Element(HeaderCell).AlignCenter().Text("السعر");
                header.Cell().Element(HeaderCell).AlignCenter().Text("الخصم");
                header.Cell().Element(HeaderCell).AlignCenter().Text("الإجمالي");
            });

            // Rows
            var rowNumber = 1;
            foreach (var item in _data.Items)
            {
                var isEvenRow = rowNumber % 2 == 0;
                var rowBackground = isEvenRow ? AccentColor : "#FFFFFF";

                table.Cell().Element(c => DataCell(c, rowBackground))
                    .Text(rowNumber.ToString()).FontColor(MutedColor);

                table.Cell().Element(c => DataCell(c, rowBackground))
                    .Text(item.ProductName).Bold();

                table.Cell().Element(c => DataCell(c, rowBackground))
                    .AlignCenter().Text(item.UnitName);

                table.Cell().Element(c => DataCell(c, rowBackground))
                    .AlignCenter().Text(item.Quantity.ToString("N2"));

                table.Cell().Element(c => DataCell(c, rowBackground))
                    .AlignCenter().Text($"{item.UnitPrice:N2}");

                table.Cell().Element(c => DataCell(c, rowBackground))
                    .AlignCenter()
                    .Text(item.Discount > 0 ? $"{item.Discount:N2}" : "-")
                    .FontColor(item.Discount > 0 ? "#F44336" : MutedColor);

                table.Cell().Element(c => DataCell(c, rowBackground))
                    .AlignCenter().Text($"{item.Total:N2}").Bold();

                rowNumber++;
            }
        });
    }

    private void ComposeTotalsSection(IContainer container)
    {
        container.Border(1).BorderColor("#E0E0E0").Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn();
                cols.RelativeColumn();
            });

            // Sub total
            table.Cell().Padding(6).AlignRight()
                .Text("المجموع الفرعي:").FontColor(MutedColor);
            table.Cell().Padding(6).AlignLeft()
                .Text($"{_data.SubTotal:N2} ر.س");

            // Discount (only if exists)
            if (_data.DiscountAmount > 0)
            {
                table.Cell().Padding(6).AlignRight()
                    .Text("الخصم:").FontColor("#F44336");
                table.Cell().Padding(6).AlignLeft()
                    .Text($"- {_data.DiscountAmount:N2} ر.س").FontColor("#F44336");
            }

            // Tax
            var taxLabel = _data.IsTaxInclusive
                ? $"ضريبة القيمة المضافة ({_data.TaxRate:N0}%) - شاملة:"
                : $"ضريبة القيمة المضافة ({_data.TaxRate:N0}%) - مضافة:";

            table.Cell().Padding(6).AlignRight().Text(taxLabel).FontColor(MutedColor);
            table.Cell().Padding(6).AlignLeft().Text($"{_data.TaxAmount:N2} ر.س");

            // Grand total — highlighted
            table.Cell().ColumnSpan(2)
                .Background(PrimaryColor)
                .Padding(10)
                .Row(row =>
                {
                    row.RelativeItem().AlignRight()
                        .Text("الإجمالي النهائي:").FontColor("#FFFFFF").Bold()
                        .FontSize(SubHeaderFontSize);
                    row.RelativeItem().AlignLeft()
                        .Text($"{_data.GrandTotal:N2} ر.س")
                        .FontColor("#FFFFFF").Bold()
                        .FontSize(SubHeaderFontSize);
                });
        });
    }

    // ═══════════════════════════════════════════════
    // FOOTER
    // ═══════════════════════════════════════════════
    private void ComposeFooter(IContainer container)
    {
        container
            .BorderTop(1).BorderColor("#E0E0E0")
            .PaddingTop(8)
            .Row(row =>
            {
                row.RelativeItem().AlignRight()
                    .Text(ctx =>
                    {
                        ctx.Span("صفحة ").FontColor(MutedColor).FontSize(SmallFontSize);
                        ctx.CurrentPageNumber().FontColor(MutedColor).FontSize(SmallFontSize);
                        ctx.Span(" من ").FontColor(MutedColor).FontSize(SmallFontSize);
                        ctx.TotalPages().FontColor(MutedColor).FontSize(SmallFontSize);
                    });

                row.RelativeItem().AlignCenter()
                    .Text("شكراً لتعاملكم معنا")
                    .FontSize(SmallFontSize).FontColor(MutedColor).Italic();

                row.RelativeItem().AlignLeft()
                    .Text($"طُبع: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(SmallFontSize).FontColor(MutedColor);
            });
    }

    // ─── Cell Style Helpers ───────────────────────

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background(PrimaryColor)
            .Padding(8)
            .DefaultTextStyle(s => s
                .FontColor("#FFFFFF")
                .Bold()
                .FontSize(BodyFontSize));
    }

    private static IContainer DataCell(IContainer container, string background)
    {
        return container
            .Background(background)
            .BorderBottom(1).BorderColor("#E0E0E0")
            .Padding(7);
    }

    private string GetInvoiceTypeLabel() => _data.InvoiceType switch
    {
        InvoiceTypePrint.Sales => "فاتورة مبيعات",
        InvoiceTypePrint.Purchase => "فاتورة مشتريات",
        InvoiceTypePrint.SalesReturn => "مرتجع مبيعات",
        InvoiceTypePrint.PurchaseReturn => "مرتجع مشتريات",
        _ => "فاتورة"
    };
}
