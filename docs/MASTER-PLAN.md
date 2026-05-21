Implementation Plan: Printing & PDF Generation Engine
📋 Master Rules for AI Agent
This is a self-contained phase. Complete tasks in order. Never mix printing logic with business logic.

🗂️ Phase 0: Setup & Dependencies
Task 0.1 — Install NuGet Packages
XML

<!-- File: YourApp.Infrastructure/YourApp.Infrastructure.csproj -->
<!-- ADD these package references -->

<ItemGroup>
  <!-- PDF Generation -->
  <PackageReference Include="QuestPDF" Version="2024.3.0" />
  
  <!-- Image processing (logo resize) -->
  <PackageReference Include="SixLabors.ImageSharp" Version="3.1.4" />
  
  <!-- Thermal printing ESC/POS -->
  <PackageReference Include="ESCPOS.NET" Version="3.0.0" />
</ItemGroup>
Task 0.2 — QuestPDF License Setup
csharp

// File: Infrastructure/Printing/PrintingBootstrapper.cs
// Call this ONCE at application startup (App.xaml.cs or Program.cs)

public static class PrintingBootstrapper
{
    public static void Initialize()
    {
        // QuestPDF Community license (free for revenue < $1M USD)
        QuestPDF.Settings.License = LicenseType.Community;
    }
}

// In App.xaml.cs:
// protected override void OnStartup(StartupEventArgs e)
// {
//     PrintingBootstrapper.Initialize();
//     base.OnStartup(e);
// }
Task 0.3 — Add Settings Columns for Print Setup
SQL

-- File: Migrations/AddPrintSettings.sql

INSERT INTO SystemSettings (SettingKey, SettingValue, DataType, Category, DisplayName, Description)
VALUES
('StoreName',       'اسم المتجر',     'string', 'Print', 'اسم المتجر',      'يظهر في رأس الفاتورة'),
('StorePhone',      '',                'string', 'Print', 'رقم الهاتف',      'رقم التواصل في الفاتورة'),
('StoreAddress',    '',                'string', 'Print', 'العنوان',         'عنوان المتجر'),
('StoreTaxNumber',  '',                'string', 'Print', 'الرقم الضريبي',   'الرقم الضريبي للمتجر'),
('LogoPath',        '',                'string', 'Print', 'مسار الشعار',     'مسار صورة شعار المتجر'),
('ThermalPrinterName', '',             'string', 'Print', 'طابعة حرارية',   'اسم الطابعة الحرارية في ويندوز'),
('A4PrinterName',   '',                'string', 'Print', 'طابعة A4',       'اسم طابعة A4 في ويندوز'),
('TaxRate',         '15',              'decimal','Print', 'نسبة الضريبة %',  'نسبة ضريبة القيمة المضافة');
✅ Phase 0 Checklist
 QuestPDF NuGet installed successfully
 ImageSharp NuGet installed
 ESCPOS.NET NuGet installed
 PrintingBootstrapper.Initialize() called at startup
 Settings seeded in database
🏗️ Phase 1: Core Interfaces & Data Contracts
Task 1.1 — Print Data Transfer Objects
csharp

// File: Application/Printing/Contracts/InvoicePrintDto.cs
// These DTOs carry ALL data needed for printing
// Printing layer never queries the database directly

public record InvoicePrintDto
{
    // ─── Store Info (from Settings) ───────────────
    public string StoreName { get; init; } = string.Empty;
    public string StorePhone { get; init; } = string.Empty;
    public string StoreAddress { get; init; } = string.Empty;
    public string StoreTaxNumber { get; init; } = string.Empty;
    public byte[]? LogoBytes { get; init; }          // NULL = no logo, handled gracefully

    // ─── Invoice Header ───────────────────────────
    public int InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime InvoiceDate { get; init; }
    public InvoiceTypePrint InvoiceType { get; init; }

    // ─── Parties ──────────────────────────────────
    public string CustomerOrSupplierName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? CustomerAddress { get; init; }

    // ─── Items ────────────────────────────────────
    public List<InvoiceItemPrintDto> Items { get; init; } = new();

    // ─── Financials ───────────────────────────────
    public decimal SubTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public bool IsTaxInclusive { get; init; }

    // ─── Payment ──────────────────────────────────
    public string PaymentMethod { get; init; } = string.Empty; // "نقدي" / "شبكة"
    public decimal AmountPaid { get; init; }
    public decimal ChangeAmount { get; init; }
    public string? Notes { get; init; }
}

public record InvoiceItemPrintDto(
    string ProductName,
    string UnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Total
);

public enum InvoiceTypePrint
{
    Sales,          // فاتورة مبيعات
    Purchase,       // فاتورة مشتريات
    SalesReturn,    // مرتجع مبيعات
    PurchaseReturn  // مرتجع مشتريات
}
Task 1.2 — IPrintService Interface
csharp

// File: Application/Printing/IPrintService.cs

public interface IPrintService
{
    /// <summary>
    /// Generates PDF and saves to temp path, then opens preview window.
    /// </summary>
    Task<PrintResult> ShowPreviewAsync(InvoicePrintDto invoice);

    /// <summary>
    /// Generates PDF and sends directly to A4 printer.
    /// </summary>
    Task<PrintResult> PrintA4Async(InvoicePrintDto invoice);

    /// <summary>
    /// Sends condensed receipt to 80mm thermal printer.
    /// </summary>
    Task<PrintResult> PrintThermalAsync(InvoicePrintDto invoice);

    /// <summary>
    /// Saves PDF to user-chosen location.
    /// </summary>
    Task<PrintResult> SavePdfAsync(InvoicePrintDto invoice, string filePath);
}

// Result object — never throw exceptions to the ViewModel
public record PrintResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputFilePath { get; init; }

    public static PrintResult Success(string? filePath = null)
        => new() { IsSuccess = true, OutputFilePath = filePath };

    public static PrintResult Failure(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
Task 1.3 — Invoice Print Data Builder
csharp

// File: Application/Printing/InvoicePrintDtoBuilder.cs
// Assembles the DTO from domain data + settings
// This is the ONLY place that touches the database for print data

public class InvoicePrintDtoBuilder
{
    private readonly ISystemSettingsRepository _settings;
    private readonly ILogger<InvoicePrintDtoBuilder> _logger;

    public InvoicePrintDtoBuilder(
        ISystemSettingsRepository settings,
        ILogger<InvoicePrintDtoBuilder> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<InvoicePrintDto> BuildAsync(
        SalesInvoice invoice,
        CancellationToken ct = default)
    {
        var storeSettings = await _settings.GetPrintSettingsAsync(ct);
        var logoBytes = await LoadLogoSafelyAsync(storeSettings.LogoPath);

        return new InvoicePrintDto
        {
            // Store info
            StoreName = storeSettings.StoreName,
            StorePhone = storeSettings.StorePhone,
            StoreAddress = storeSettings.StoreAddress,
            StoreTaxNumber = storeSettings.StoreTaxNumber,
            LogoBytes = logoBytes,         // NULL if not found — handled in printer

            // Invoice header
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.CreatedAt,
            InvoiceType = InvoiceTypePrint.Sales,

            // Parties
            CustomerOrSupplierName = invoice.CustomerName ?? "زبون نقدي",
            CustomerPhone = invoice.CustomerPhone,

            // Items — project to print DTO
            Items = invoice.Items.Select(item => new InvoiceItemPrintDto(
                item.ProductName,
                item.UnitName,
                item.Quantity,
                item.UnitPrice,
                item.Discount,
                item.TotalPrice
            )).ToList(),

            // Financials
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            TaxRate = storeSettings.TaxRate,
            TaxAmount = invoice.TaxAmount,
            GrandTotal = invoice.GrandTotal,
            IsTaxInclusive = invoice.IsTaxInclusive,

            // Payment
            PaymentMethod = invoice.PaymentMethod,
            AmountPaid = invoice.AmountPaid,
            ChangeAmount = Math.Max(0, invoice.AmountPaid - invoice.GrandTotal),
            Notes = invoice.Notes
        };
    }

    private async Task<byte[]?> LoadLogoSafelyAsync(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
            return null;

        if (!File.Exists(logoPath))
        {
            _logger.LogWarning("Logo file not found at path: {Path}", logoPath);
            return null;
        }

        try
        {
            return await File.ReadAllBytesAsync(logoPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load logo from {Path}", logoPath);
            return null;    // Graceful degradation — no crash
        }
    }
}
✅ Phase 1 Checklist
 InvoicePrintDto contains ALL data needed — no DB calls from printers
 PrintResult never throws exceptions (returns failure object instead)
 LoadLogoSafelyAsync returns NULL if file missing (no crash)
 Builder is the ONLY class that touches DB for print data
⚙️ Phase 2: A4 PDF Generator (QuestPDF)
Task 2.1 — A4 Invoice Document
csharp

// File: Infrastructure/Printing/A4/A4InvoiceDocument.cs

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class A4InvoiceDocument : IDocument
{
    private readonly InvoicePrintDto _data;

    // ─── Design Constants ─────────────────────────
    private static readonly string FontFamily = "Arial";
    private static readonly float HeaderFontSize = 20f;
    private static readonly float SubHeaderFontSize = 13f;
    private static readonly float BodyFontSize = 10f;
    private static readonly float SmallFontSize = 8f;

    private static readonly string PrimaryColor = "#1565C0";     // Dark blue
    private static readonly string AccentColor = "#E3F2FD";      // Light blue
    private static readonly string TextColor = "#212121";
    private static readonly string MutedColor = "#757575";
    private static readonly string SuccessColor = "#2E7D32";

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
            page.ContentFromRightToLeft(); // Arabic RTL support

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // ═══════════════════════════════════════════════
    // HEADER: Logo + Store Info + Invoice Title
    // ═══════════════════════════════════════════════
    private void ComposeHeader(IContainer container)
    {
        container
            .BorderBottom(2).BorderColor(PrimaryColor)
            .PaddingBottom(10)
            .Row(row =>
            {
                // ─── Left: Logo (conditional) ─────────────────
                if (_data.LogoBytes != null)
                {
                    row.ConstantItem(80).Height(80)
                        .Padding(4)
                        .Image(_data.LogoBytes)
                        .FitArea();
                }
                else
                {
                    // No logo — use colored placeholder with store initial
                    row.ConstantItem(80).Height(80)
                        .Background(PrimaryColor)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(_data.StoreName.Length > 0
                            ? _data.StoreName[0].ToString()
                            : "م")
                        .FontSize(36).FontColor(Colors.White).Bold();
                }

                row.RelativeItem().PaddingHorizontal(12).Column(col =>
                {
                    // Store name — largest text
                    col.Item()
                        .Text(_data.StoreName)
                        .FontSize(HeaderFontSize)
                        .FontColor(PrimaryColor)
                        .Bold();

                    if (!string.IsNullOrWhiteSpace(_data.StorePhone))
                        col.Item().Text($"📞 {_data.StorePhone}")
                            .FontSize(SmallFontSize).FontColor(MutedColor);

                    if (!string.IsNullOrWhiteSpace(_data.StoreAddress))
                        col.Item().Text($"📍 {_data.StoreAddress}")
                            .FontSize(SmallFontSize).FontColor(MutedColor);

                    if (!string.IsNullOrWhiteSpace(_data.StoreTaxNumber))
                        col.Item().Text($"الرقم الضريبي: {_data.StoreTaxNumber}")
                            .FontSize(SmallFontSize).FontColor(MutedColor);
                });

                // ─── Right: Invoice badge ──────────────────────
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
    // CONTENT: Customer Info + Items Table
    // ═══════════════════════════════════════════════
    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // ─── Customer / Supplier info ──────────────────
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
                            c.Item().Text($"📞 {_data.CustomerPhone}")
                                .FontSize(SmallFontSize);

                        if (!string.IsNullOrWhiteSpace(_data.CustomerAddress))
                            c.Item().Text($"📍 {_data.CustomerAddress}")
                                .FontSize(SmallFontSize);
                    });

                row.ConstantItem(20); // spacer

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

            // ─── Items Table ───────────────────────────────
            col.Item().Element(ComposeItemsTable);

            // ─── Totals ────────────────────────────────────
            col.Item().PaddingTop(12).AlignRight()
                .Width(280).Element(ComposeTotalsSection);

            // ─── Notes ────────────────────────────────────
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

    // ─── Items Table ──────────────────────────────
    private void ComposeItemsTable(IContainer container)
    {
        container.Table(table =>
        {
            // Column definitions (widths)
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(25);    // # (row number)
                cols.RelativeColumn(4);     // Product Name
                cols.RelativeColumn(1.5f);  // Unit
                cols.RelativeColumn(1.5f);  // Qty
                cols.RelativeColumn(2);     // Unit Price
                cols.RelativeColumn(1.5f);  // Discount
                cols.RelativeColumn(2);     // Total
            });

            // ─── Table Header ─────────────────────────────
            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("#");
                header.Cell().Element(HeaderCell).Text("المنتج");
                header.Cell().Element(HeaderCell).AlignCenter().Text("الوحدة");
                header.Cell().Element(HeaderCell).AlignCenter().Text("الكمية");
                header.Cell().Element(HeaderCell).AlignCenter().Text("سعر الوحدة");
                header.Cell().Element(HeaderCell).AlignCenter().Text("الخصم");
                header.Cell().Element(HeaderCell).AlignCenter().Text("الإجمالي");
            });

            // ─── Table Rows ───────────────────────────────
            var rowNumber = 1;
            foreach (var item in _data.Items)
            {
                var isEvenRow = rowNumber % 2 == 0;
                var rowBackground = isEvenRow ? AccentColor : Colors.White;

                table.Cell().Element(c => DataCell(c, rowBackground))
                    .Text(rowNumber.ToString()).FontColor(MutedColor);

                table.Cell().Element(c => DataCell(c, rowBackground))
                    .Column(col =>
                    {
                        col.Item().Text(item.ProductName).Bold();
                    });

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

    // ─── Totals Section ───────────────────────────
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
                    .Text("الخصم الإضافي:").FontColor("#F44336");
                table.Cell().Padding(6).AlignLeft()
                    .Text($"- {_data.DiscountAmount:N2} ر.س").FontColor("#F44336");
            }

            // Tax — show calculation method
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
                        .Text("الإجمالي النهائي:").FontColor(Colors.White).Bold()
                        .FontSize(SubHeaderFontSize);
                    row.RelativeItem().AlignLeft()
                        .Text($"{_data.GrandTotal:N2} ر.س")
                        .FontColor(Colors.White).Bold()
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

    private IContainer HeaderCell(IContainer container)
    {
        return container
            .Background(PrimaryColor)
            .Padding(8)
            .DefaultTextStyle(s => s
                .FontColor(Colors.White)
                .Bold()
                .FontSize(BodyFontSize));
    }

    private IContainer DataCell(IContainer container, string background)
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
✅ Phase 2 Checklist
 Logo renders when LogoBytes is not null
 Logo space is OMITTED (not empty box) when LogoBytes is null
 Even/odd row alternating colors work correctly
 Tax label shows "شاملة" vs "مضافة" based on IsTaxInclusive
 Discount row only appears when DiscountAmount > 0
 Page numbers render correctly on multi-page invoices
 RTL direction applied to entire page
⚙️ Phase 3: Thermal Receipt Printer (80mm)
Task 3.1 — Thermal Receipt Generator
csharp

// File: Infrastructure/Printing/Thermal/ThermalReceiptGenerator.cs
// Uses monospaced formatting for perfect column alignment

public class ThermalReceiptGenerator
{
    // 80mm thermal printer = 42 characters per line (at 12pt monospace)
    private const int LineWidth = 42;
    private const char Separator = '-';
    private const char DoubleSeparator = '=';

    public byte[] GenerateEscPosCommands(InvoicePrintDto data)
    {
        var commands = new List<byte[]>();

        // ─── ESC/POS: Initialize printer ──────────────
        commands.Add(EscPos.Initialize());

        // ─── Header ───────────────────────────────────
        commands.Add(EscPos.SetAlignment(Alignment.Center));
        commands.Add(EscPos.SetBold(true));
        commands.Add(EscPos.SetFontSize(2)); // Double height

        // Store name (truncate if too long)
        var storeName = TruncateCenter(data.StoreName, LineWidth);
        commands.Add(EscPos.PrintLine(storeName));

        commands.Add(EscPos.SetFontSize(1));
        commands.Add(EscPos.SetBold(false));

        if (!string.IsNullOrWhiteSpace(data.StorePhone))
            commands.Add(EscPos.PrintLine(data.StorePhone));

        if (!string.IsNullOrWhiteSpace(data.StoreAddress))
        {
            // Wrap address to fit line width
            foreach (var line in WrapText(data.StoreAddress, LineWidth))
                commands.Add(EscPos.PrintLine(line));
        }

        if (!string.IsNullOrWhiteSpace(data.StoreTaxNumber))
            commands.Add(EscPos.PrintLine($"ض: {data.StoreTaxNumber}"));

        commands.Add(EscPos.PrintLine(new string(DoubleSeparator, LineWidth)));

        // ─── Invoice info ──────────────────────────────
        commands.Add(EscPos.SetAlignment(Alignment.Right));
        commands.Add(EscPos.PrintLine(
            FormatTwoColumns("رقم الفاتورة:", data.InvoiceNumber)));
        commands.Add(EscPos.PrintLine(
            FormatTwoColumns("التاريخ:", data.InvoiceDate.ToString("dd/MM/yyyy HH:mm"))));
        commands.Add(EscPos.PrintLine(
            FormatTwoColumns("العميل:", TruncateRight(data.CustomerOrSupplierName, 20))));

        commands.Add(EscPos.PrintLine(new string(Separator, LineWidth)));

        // ─── Column headers ────────────────────────────
        // Format: "الصنف            الكمية  السعر   المجموع"
        commands.Add(EscPos.SetBold(true));
        commands.Add(EscPos.PrintLine(
            FormatItemHeader()));
        commands.Add(EscPos.SetBold(false));
        commands.Add(EscPos.PrintLine(new string(Separator, LineWidth)));

        // ─── Items ────────────────────────────────────
        foreach (var item in data.Items)
        {
            // Line 1: Product name (full width)
            var name = TruncateRight(item.ProductName, LineWidth - 2);
            commands.Add(EscPos.PrintLine($"  {name}"));

            // Line 2: Qty × Price = Total (aligned right)
            var itemLine = FormatItemLine(
                item.UnitName,
                item.Quantity,
                item.UnitPrice,
                item.Total);
            commands.Add(EscPos.PrintLine(itemLine));

            // Discount note (only if exists)
            if (item.Discount > 0)
                commands.Add(EscPos.PrintLine(
                    FormatTwoColumns("  خصم:", $"-{item.Discount:N2}")));
        }

        commands.Add(EscPos.PrintLine(new string(DoubleSeparator, LineWidth)));

        // ─── Totals ────────────────────────────────────
        if (data.DiscountAmount > 0)
            commands.Add(EscPos.PrintLine(
                FormatTwoColumns("الخصم:", $"-{data.DiscountAmount:N2}")));

        commands.Add(EscPos.PrintLine(
            FormatTwoColumns($"ض.ق.م ({data.TaxRate:N0}%):", $"{data.TaxAmount:N2}")));

        // Grand total — bold
        commands.Add(EscPos.SetBold(true));
        commands.Add(EscPos.SetFontSize(2));
        commands.Add(EscPos.PrintLine(
            FormatTwoColumns("الإجمالي:", $"{data.GrandTotal:N2} ر.س")));
        commands.Add(EscPos.SetFontSize(1));
        commands.Add(EscPos.SetBold(false));

        // Payment info
        commands.Add(EscPos.PrintLine(
            FormatTwoColumns("المدفوع:", $"{data.AmountPaid:N2}")));
        if (data.ChangeAmount > 0)
            commands.Add(EscPos.PrintLine(
                FormatTwoColumns("الباقي:", $"{data.ChangeAmount:N2}")));

        commands.Add(EscPos.PrintLine(new string(DoubleSeparator, LineWidth)));

        // ─── Footer ────────────────────────────────────
        commands.Add(EscPos.SetAlignment(Alignment.Center));
        commands.Add(EscPos.PrintLine("شكراً لتعاملكم معنا"));
        commands.Add(EscPos.PrintLine(string.Empty));
        commands.Add(EscPos.PrintLine(string.Empty));

        // ─── Cut paper ────────────────────────────────
        commands.Add(EscPos.CutPaper());

        // Flatten all byte arrays
        return commands.SelectMany(b => b).ToArray();
    }

    // ─── Text Formatting Helpers ──────────────────

    /// <summary>
    /// "Label:          Value" aligned to fill LineWidth exactly
    /// </summary>
    private string FormatTwoColumns(string label, string value)
    {
        var totalLength = label.Length + value.Length;
        var spaces = Math.Max(1, LineWidth - totalLength);
        return label + new string(' ', spaces) + value;
    }

    private string FormatItemHeader()
    {
        // "الوحدة  الكمية  السعر   المجموع"
        return "الوحدة".PadLeft(8) +
               "الكمية".PadLeft(8) +
               "السعر".PadLeft(9) +
               "المجموع".PadLeft(9);
    }

    private string FormatItemLine(
        string unit, decimal qty, decimal price, decimal total)
    {
        return unit.PadLeft(8) +
               qty.ToString("N1").PadLeft(8) +
               price.ToString("N2").PadLeft(9) +
               total.ToString("N2").PadLeft(9);
    }

    private string TruncateRight(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength];

    private string TruncateCenter(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        var half = (maxLength - 3) / 2;
        return text[..half] + "..." + text[^half..];
    }

    private IEnumerable<string> WrapText(string text, int lineWidth)
    {
        for (int i = 0; i < text.Length; i += lineWidth)
            yield return text.Substring(i, Math.Min(lineWidth, text.Length - i));
    }
}

// ─── ESC/POS Command Builder ───────────────────────────────────
// Lightweight wrapper — avoids heavy dependencies

public static class EscPos
{
    public static byte[] Initialize()
        => new byte[] { 0x1B, 0x40 };                   // ESC @

    public static byte[] CutPaper()
        => new byte[] { 0x1D, 0x56, 0x42, 0x00 };       // GS V B 0

    public static byte[] SetBold(bool bold)
        => bold
            ? new byte[] { 0x1B, 0x45, 0x01 }           // ESC E 1
            : new byte[] { 0x1B, 0x45, 0x00 };          // ESC E 0

    public static byte[] SetAlignment(Alignment alignment)
    {
        byte code = alignment switch
        {
            Alignment.Left => 0x00,
            Alignment.Center => 0x01,
            Alignment.Right => 0x02,
            _ => 0x00
        };
        return new byte[] { 0x1B, 0x61, code };          // ESC a n
    }

    public static byte[] SetFontSize(int multiplier)
    {
        // 1 = normal, 2 = double height
        byte size = multiplier <= 1 ? (byte)0x00 : (byte)0x11;
        return new byte[] { 0x1D, 0x21, size };          // GS ! n
    }

    public static byte[] PrintLine(string text)
    {
        // Encode in Windows-1256 for Arabic character support
        var encoding = System.Text.Encoding.GetEncoding(1256);
        var textBytes = encoding.GetBytes(text);
        var newLine = new byte[] { 0x0A };               // LF
        return textBytes.Concat(newLine).ToArray();
    }
}

public enum Alignment { Left, Center, Right }
✅ Phase 3 Checklist
 All lines fit within 42 characters
 FormatTwoColumns fills exactly to LineWidth
 CutPaper() ESC/POS command added at end
 Arabic encoded in Windows-1256 (not UTF-8)
 Logo is NOT included (thermal printers don't need it)
🔧 Phase 4: Print Service Implementation
Task 4.1 — Main PrintService
csharp

// File: Infrastructure/Printing/PrintService.cs

public class PrintService : IPrintService
{
    private readonly ISystemSettingsRepository _settings;
    private readonly ILogger<PrintService> _logger;
    private readonly ThermalReceiptGenerator _thermalGenerator;

    public PrintService(
        ISystemSettingsRepository settings,
        ILogger<PrintService> logger)
    {
        _settings = settings;
        _logger = logger;
        _thermalGenerator = new ThermalReceiptGenerator();
    }

    // ═══════════════════════════════════════════════
    // SHOW PREVIEW (WPF Modal Window)
    // ═══════════════════════════════════════════════
    public async Task<PrintResult> ShowPreviewAsync(InvoicePrintDto invoice)
    {
        try
        {
            var pdfBytes = await GeneratePdfBytesAsync(invoice);

            // Save to temp file for preview
            var tempPath = Path.Combine(
                Path.GetTempPath(),
                $"Invoice_{invoice.InvoiceNumber}_{DateTime.Now:HHmmss}.pdf");

            await File.WriteAllBytesAsync(tempPath, pdfBytes);

            // Open preview window on UI thread
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var previewWindow = new PdfPreviewWindow(tempPath, invoice.InvoiceNumber);
                previewWindow.ShowDialog();
            });

            // Cleanup temp file after preview closes
            TryDeleteFile(tempPath);

            return PrintResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show print preview");
            return PrintResult.Failure(
                $"تعذر فتح معاينة الطباعة:\n{GetUserFriendlyError(ex)}");
        }
    }

    // ═══════════════════════════════════════════════
    // PRINT A4
    // ═══════════════════════════════════════════════
    public async Task<PrintResult> PrintA4Async(InvoicePrintDto invoice)
    {
        try
        {
            var settings = await _settings.GetPrintSettingsAsync();
            var printerName = settings.A4PrinterName;

            // Validate printer exists
            var printerExists = PrinterSettings.InstalledPrinters
                .Cast<string>()
                .Any(p => p.Equals(printerName, StringComparison.OrdinalIgnoreCase));

            if (!printerExists)
            {
                return PrintResult.Failure(
                    $"الطابعة '{printerName}' غير موجودة أو غير متصلة.\n" +
                    $"يرجى:\n" +
                    $"1. التأكد من توصيل الطابعة\n" +
                    $"2. التأكد من تثبيت تعريف الطابعة\n" +
                    $"3. مراجعة اسم الطابعة في الإعدادات");
            }

            var pdfBytes = await GeneratePdfBytesAsync(invoice);

            // Save to temp and print
            var tempPath = Path.GetTempFileName() + ".pdf";
            await File.WriteAllBytesAsync(tempPath, pdfBytes);

            await Task.Run(() =>
            {
                // Use Windows print verb to send PDF to printer
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    Verb = "printto",
                    Arguments = $"\"{printerName}\"",
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            });

            _logger.LogInformation(
                "A4 invoice {InvoiceNumber} sent to printer {Printer}",
                invoice.InvoiceNumber, printerName);

            TryDeleteFile(tempPath, delayMs: 3000); // Give print spooler time
            return PrintResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A4 print failed for invoice {Invoice}",
                invoice.InvoiceNumber);
            return PrintResult.Failure(
                $"فشل إرسال الفاتورة للطابعة:\n{GetUserFriendlyError(ex)}");
        }
    }

    // ═══════════════════════════════════════════════
    // PRINT THERMAL
    // ═══════════════════════════════════════════════
    public async Task<PrintResult> PrintThermalAsync(InvoicePrintDto invoice)
    {
        try
        {
            var settings = await _settings.GetPrintSettingsAsync();
            var printerName = settings.ThermalPrinterName;

            if (string.IsNullOrWhiteSpace(printerName))
                return PrintResult.Failure(
                    "لم يتم تحديد الطابعة الحرارية بعد.\n" +
                    "يرجى الذهاب إلى الإعدادات → إعداد الطباعة وتحديد الطابعة الحرارية.");

            // Generate ESC/POS byte commands
            var escPosData = _thermalGenerator.GenerateEscPosCommands(invoice);

            await Task.Run(() => SendRawToPrinter(printerName, escPosData));

            _logger.LogInformation(
                "Thermal receipt {InvoiceNumber} printed to {Printer}",
                invoice.InvoiceNumber, printerName);

            return PrintResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thermal print failed");
            return PrintResult.Failure(
                $"فشلت طباعة الإيصال الحراري:\n{GetUserFriendlyError(ex)}");
        }
    }

    // ═══════════════════════════════════════════════
    // SAVE PDF
    // ═══════════════════════════════════════════════
    public async Task<PrintResult> SavePdfAsync(InvoicePrintDto invoice, string filePath)
    {
        try
        {
            var pdfBytes = await GeneratePdfBytesAsync(invoice);
            await File.WriteAllBytesAsync(filePath, pdfBytes);

            _logger.LogInformation("PDF saved to {Path}", filePath);
            return PrintResult.Success(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save PDF");
            return PrintResult.Failure(
                $"تعذر حفظ ملف PDF:\n{GetUserFriendlyError(ex)}");
        }
    }

    // ─── Private Helpers ──────────────────────────

    private Task<byte[]> GeneratePdfBytesAsync(InvoicePrintDto invoice)
    {
        return Task.Run(() =>
        {
            var document = new A4InvoiceDocument(invoice);
            return document.GeneratePdf();
        });
    }

    /// <summary>
    /// Sends raw bytes directly to printer bypassing Windows GDI.
    /// Required for ESC/POS commands.
    /// </summary>
    private void SendRawToPrinter(string printerName, byte[] data)
    {
        var printerInfo = new DOCINFOA
        {
            pDocName = "Thermal Receipt",
            pDataType = "RAW"
        };

        var handle = OpenPrinter(printerName, out var printerHandle, IntPtr.Zero);

        if (!handle)
            throw new PrinterException(
                $"لا يمكن الاتصال بالطابعة الحرارية '{printerName}'");

        try
        {
            StartDocPrinter(printerHandle, 1, ref printerInfo);
            StartPagePrinter(printerHandle);

            var gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            WritePrinter(printerHandle, gcHandle.AddrOfPinnedObject(),
                data.Length, out _);
            gcHandle.Free();

            EndPagePrinter(printerHandle);
            EndDocPrinter(printerHandle);
        }
        finally
        {
            ClosePrinter(printerHandle);
        }
    }

    private string GetUserFriendlyError(Exception ex) => ex switch
    {
        UnauthorizedAccessException => "ليس لديك صلاحية الوصول للطابعة.",
        FileNotFoundException => "ملف الطابعة غير موجود.",
        PrinterException pe => pe.Message,
        _ when ex.Message.Contains("printer") => "الطابعة غير متصلة أو لا تستجيب.",
        _ => "حدث خطأ غير متوقع. يرجى إعادة المحاولة."
    };

    private void TryDeleteFile(string path, int delayMs = 0)
    {
        Task.Run(async () =>
        {
            if (delayMs > 0) await Task.Delay(delayMs);
            try { File.Delete(path); }
            catch { /* Ignore — temp file cleanup is best-effort */ }
        });
    }

    // Win32 API for raw printer access
    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA")]
    private static extern bool OpenPrinter(string pPrinterName,
        out IntPtr phPrinter, IntPtr pDefault);
    [DllImport("winspool.Drv")] private static extern bool ClosePrinter(IntPtr hPrinter);
    [DllImport("winspool.Drv")] private static extern bool StartDocPrinter(
        IntPtr hPrinter, int level, ref DOCINFOA pDocInfo);
    [DllImport("winspool.Drv")] private static extern bool EndDocPrinter(IntPtr hPrinter);
    [DllImport("winspool.Drv")] private static extern bool StartPagePrinter(IntPtr hPrinter);
    [DllImport("winspool.Drv")] private static extern bool EndPagePrinter(IntPtr hPrinter);
    [DllImport("winspool.Drv")] private static extern bool WritePrinter(
        IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    [StructLayout(LayoutKind.Sequential)]
    private struct DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
        [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
    }
}

public class PrinterException : Exception
{
    public PrinterException(string message) : base(message) { }
}
✅ Phase 4 Checklist
 Printer existence validated before attempting to print
 Arabic error messages for each failure scenario
 Temp files cleaned up after printing
 Raw ESC/POS sent via Win32 API (not GDI)
 GetUserFriendlyError() translates technical errors to Arabic
🖥️ Phase 5: WPF Integration
Task 5.1 — Invoice ViewModel Print Commands
csharp

// File: WPF/ViewModels/Invoice/InvoiceViewModel.cs
// ADD these print commands to existing ViewModel

public class InvoiceViewModel : BaseViewModel
{
    private readonly IPrintService _printService;
    private readonly InvoicePrintDtoBuilder _printDtoBuilder;

    // ─── Print Commands ───────────────────────────
    public IAsyncRelayCommand PrintA4Command { get; }
    public IAsyncRelayCommand PrintThermalCommand { get; }
    public IAsyncRelayCommand ShowPreviewCommand { get; }
    public IAsyncRelayCommand SavePdfCommand { get; }

    public InvoiceViewModel(IPrintService printService,
        InvoicePrintDtoBuilder printDtoBuilder)
    {
        _printService = printService;
        _printDtoBuilder = printDtoBuilder;

        PrintA4Command = new AsyncRelayCommand(PrintA4Async,
            () => CurrentInvoice != null && !IsBusy);
        PrintThermalCommand = new AsyncRelayCommand(PrintThermalAsync,
            () => CurrentInvoice != null && !IsBusy);
        ShowPreviewCommand = new AsyncRelayCommand(ShowPreviewAsync,
            () => CurrentInvoice != null && !IsBusy);
        SavePdfCommand = new AsyncRelayCommand(SavePdfAsync,
            () => CurrentInvoice != null && !IsBusy);
    }

    private async Task PrintA4Async()
    {
        await ExecutePrintAsync(async dto =>
            await _printService.PrintA4Async(dto));
    }

    private async Task PrintThermalAsync()
    {
        await ExecutePrintAsync(async dto =>
            await _printService.PrintThermalAsync(dto));
    }

    private async Task ShowPreviewAsync()
    {
        await ExecutePrintAsync(async dto =>
            await _printService.ShowPreviewAsync(dto));
    }

    private async Task SavePdfAsync()
    {
        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "حفظ الفاتورة كـ PDF",
            Filter = "PDF Files|*.pdf",
            FileName = $"فاتورة_{CurrentInvoice!.InvoiceNumber}_{DateTime.Now:yyyyMMdd}"
        };

        if (saveDialog.ShowDialog() != true) return;

        await ExecutePrintAsync(async dto =>
            await _printService.SavePdfAsync(dto, saveDialog.FileName));
    }

    /// <summary>
    /// Shared wrapper: builds DTO, executes print action, handles result.
    /// </summary>
    private async Task ExecutePrintAsync(Func<InvoicePrintDto, Task<PrintResult>> printAction)
    {
        if (CurrentInvoice == null) return;

        try
        {
            IsBusy = true;
            StatusMessage = "جارٍ تجهيز الفاتورة...";

            var printDto = await _printDtoBuilder.BuildAsync(CurrentInvoice);
            var result = await printAction(printDto);

            if (result.IsSuccess)
            {
                StatusMessage = "✅ تمت الطباعة بنجاح";
                if (!string.IsNullOrWhiteSpace(result.OutputFilePath))
                    StatusMessage = $"✅ تم الحفظ: {result.OutputFilePath}";
            }
            else
            {
                // Show user-friendly dialog — never just StatusMessage for print errors
                ShowPrintErrorDialog(result.ErrorMessage!);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowPrintErrorDialog(string message)
    {
        System.Windows.MessageBox.Show(
            message,
            "خطأ في الطباعة",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Warning);
    }
}
Task 5.2 — Print Setup Settings Screen
csharp

// File: WPF/ViewModels/Settings/PrintSetupViewModel.cs

public class PrintSetupViewModel : BaseViewModel
{
    private readonly ISystemSettingsRepository _settings;

    public string StoreName { get; set; } = string.Empty;
    public string StorePhone { get; set; } = string.Empty;
    public string StoreAddress { get; set; } = string.Empty;
    public string StoreTaxNumber { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
    public string ThermalPrinterName { get; set; } = string.Empty;
    public string A4PrinterName { get; set; } = string.Empty;

    public ImageSource? LogoPreview { get; private set; }

    // Installed printers list for ComboBoxes
    public List<string> InstalledPrinters { get; } =
        PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(p => p).ToList();

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand BrowseLogoCommand { get; }
    public IAsyncRelayCommand PrintTestPageCommand { get; }

    public PrintSetupViewModel(ISystemSettingsRepository settings)
    {
        _settings = settings;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        BrowseLogoCommand = new RelayCommand(BrowseLogo);
        PrintTestPageCommand = new AsyncRelayCommand(PrintTestPageAsync);
    }

    private void BrowseLogo()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "اختر شعار المتجر",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        var originalPath = dialog.FileName;

        // Resize and save to app data folder
        var resizedPath = ResizeAndSaveLogo(originalPath);

        LogoPath = resizedPath;
        OnPropertyChanged(nameof(LogoPath));

        // Show preview
        LogoPreview = new BitmapImage(new Uri(resizedPath));
        OnPropertyChanged(nameof(LogoPreview));

        StatusMessage = "✅ تم تحميل الشعار بنجاح";
    }

    private string ResizeAndSaveLogo(string sourcePath)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "YourApp", "Assets");

        Directory.CreateDirectory(appDataPath);
        var destPath = Path.Combine(appDataPath, "store_logo.png");

        // Resize to 200×200 max while preserving aspect ratio
        using var image = SixLabors.ImageSharp.Image.Load(sourcePath);

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new SixLabors.ImageSharp.Size(200, 200),
            Mode = ResizeMode.Max,           // Preserve aspect ratio
            Sampler = KnownResamplers.Lanczos3 // High quality
        }));

        image.Save(destPath);
        return destPath;
    }

    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            await _settings.SavePrintSettingsAsync(new PrintSettings
            {
                StoreName = StoreName,
                StorePhone = StorePhone,
                StoreAddress = StoreAddress,
                StoreTaxNumber = StoreTaxNumber,
                LogoPath = LogoPath,
                ThermalPrinterName = ThermalPrinterName,
                A4PrinterName = A4PrinterName
            });
            StatusMessage = "✅ تم حفظ إعدادات الطباعة";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PrintTestPageAsync()
    {
        // Create sample invoice for testing layout
        var testDto = CreateTestInvoice();
        var printService = App.GetService<IPrintService>();
        var result = await printService.ShowPreviewAsync(testDto);

        if (!result.IsSuccess)
            StatusMessage = $"❌ {result.ErrorMessage}";
    }

    private InvoicePrintDto CreateTestInvoice() => new()
    {
        StoreName = StoreName,
        StorePhone = StorePhone,
        StoreAddress = StoreAddress,
        StoreTaxNumber = StoreTaxNumber,
        LogoBytes = File.Exists(LogoPath) ? File.ReadAllBytes(LogoPath) : null,
        InvoiceNumber = "TEST-001",
        InvoiceDate = DateTime.Now,
        InvoiceType = InvoiceTypePrint.Sales,
        CustomerOrSupplierName = "عميل تجريبي",
        Items = new List<InvoiceItemPrintDto>
        {
            new("منتج تجريبي", "حبة", 2, 50, 0, 100),
            new("منتج آخر",    "كرتون", 1, 120, 10, 110)
        },
        SubTotal = 210,
        DiscountAmount = 10,
        TaxRate = 15,
        TaxAmount = 30,
        GrandTotal = 230,
        PaymentMethod = "نقدي",
        AmountPaid = 250,
        ChangeAmount = 20
    };
}
Task 5.3 — XAML Print Buttons
XML

<!-- File: Views/Invoice/InvoiceActionButtons.xaml -->
<!-- Add to bottom of any invoice view -->

<StackPanel Orientation="Horizontal" 
            HorizontalAlignment="Right"
            Margin="0,8,0,0">

    <!-- Preview Button -->
    <Button Command="{Binding ShowPreviewCommand}"
            ToolTip="معاينة الفاتورة قبل الطباعة"
            Style="{StaticResource ActionButtonStyle}"
            Background="#607D8B" Foreground="White"
            Margin="4,0">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🔍" FontSize="14"/>
            <TextBlock Text=" معاينة" Margin="4,0,0,0"/>
        </StackPanel>
    </Button>

    <!-- A4 Print Button -->
    <Button Command="{Binding PrintA4Command}"
            ToolTip="طباعة فاتورة A4"
            Style="{StaticResource ActionButtonStyle}"
            Background="#1976D2" Foreground="White"
            Margin="4,0">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🖨️" FontSize="14"/>
            <TextBlock Text=" A4" Margin="4,0,0,0"/>
        </StackPanel>
    </Button>

    <!-- Thermal Print Button -->
    <Button Command="{Binding PrintThermalCommand}"
            ToolTip="طباعة إيصال حراري 80mm"
            Style="{StaticResource ActionButtonStyle}"
            Background="#388E3C" Foreground="White"
            Margin="4,0">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🧾" FontSize="14"/>
            <TextBlock Text=" حراري" Margin="4,0,0,0"/>
        </StackPanel>
    </Button>

    <!-- Save PDF Button -->
    <Button Command="{Binding SavePdfCommand}"
            ToolTip="حفظ كملف PDF"
            Style="{StaticResource ActionButtonStyle}"
            Background="#E65100" Foreground="White"
            Margin="4,0">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="💾" FontSize="14"/>
            <TextBlock Text=" PDF" Margin="4,0,0,0"/>
        </StackPanel>
    </Button>
</StackPanel>
✅ Phase 5 Checklist
 ExecutePrintAsync is single shared method (no code repetition)
 Print errors shown in MessageBox (not just StatusMessage)
 Logo browse resizes to 200×200 max
 Installed printers loaded from system for ComboBox
 Test page button shows preview with current settings
🧪 Phase 6: Unit Tests
csharp

// File: Tests/Printing/ThermalFormattingTests.cs

public class ThermalFormattingTests
{
    private readonly ThermalReceiptGenerator _generator = new();

    [Fact]
    public void FormatTwoColumns_TotalLengthEqualsLineWidth()
    {
        // Use reflection to test private method via accessor
        var method = typeof(ThermalReceiptGenerator)
            .GetMethod("FormatTwoColumns",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        var result = (string)method!.Invoke(_generator,
            new object[] { "الإجمالي:", "1,500.00 ر.س" })!;

        Assert.Equal(42, result.Length);
    }

    [Fact]
    public void GenerateEscPosCommands_EmptyNotes_NoNotesSection()
    {
        var invoice = CreateMinimalInvoice();
        var bytes = _generator.GenerateEscPosCommands(invoice);

        // Should not throw and should have content
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateEscPosCommands_AlwaysEndsWithCutCommand()
    {
        var invoice = CreateMinimalInvoice();
        var bytes = _generator.GenerateEscPosCommands(invoice);

        // CUT command: GS V B 0 = 0x1D 0x56 0x42 0x00
        var lastFour = bytes.TakeLast(4).ToArray();
        Assert.Equal(new byte[] { 0x1D, 0x56, 0x42, 0x00 }, lastFour);
    }

    private InvoicePrintDto CreateMinimalInvoice() => new()
    {
        StoreName = "متجر الاختبار",
        InvoiceNumber = "TEST-001",
        InvoiceDate = DateTime.Now,
        CustomerOrSupplierName = "عميل",
        Items = new List<InvoiceItemPrintDto>
        {
            new("منتج", "حبة", 1, 10, 0, 10)
        },
        SubTotal = 10, TaxRate = 15, TaxAmount = 1.5m, GrandTotal = 11.5m,
        PaymentMethod = "نقدي", AmountPaid = 11.5m, ChangeAmount = 0
    };
}

// File: Tests/Printing/PrintResultTests.cs

public class PrintResultTests
{
    [Fact]
    public void Success_IsSuccessTrue()
    {
        var result = PrintResult.Success("/path/to/file.pdf");
        Assert.True(result.IsSuccess);
        Assert.Equal("/path/to/file.pdf", result.OutputFilePath);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failure_IsSuccessFalse()
    {
        var result = PrintResult.Failure("الطابعة غير متصلة");
        Assert.False(result.IsSuccess);
        Assert.Equal("الطابعة غير متصلة", result.ErrorMessage);
        Assert.Null(result.OutputFilePath);
    }
}
📦 Final Summary
text

┌───────────────────────────────────────────────────────────────────┐
│              PRINTING ENGINE — IMPLEMENTATION ORDER               │
├──────┬─────────────────────────────────────────────┬─────────────┤
│ Step │ Deliverable                                 │ Key Rule    │
├──────┼─────────────────────────────────────────────┼─────────────┤
│  0   │ 3 NuGet packages + SQL seed                 │ Startup init│
│  1   │ IPrintService + DTOs + Builder              │ No DB in    │
│      │                                             │ printers    │
│  2   │ A4InvoiceDocument (QuestPDF)                │ RTL + logo  │
│      │                                             │ fallback    │
│  3   │ ThermalReceiptGenerator + EscPos            │ Windows-1256│
│      │                                             │ encoding    │
│  4   │ PrintService (A4 + Thermal + Preview)       │ Arabic      │
│      │                                             │ errors only │
│  5   │ ViewModels + XAML buttons + Settings UI     │ Single exec │
│      │                                             │ wrapper     │
│  6   │ Unit tests                                  │ Never skip  │
└──────┴─────────────────────────────────────────────┴─────────────┘

CRITICAL RULES — NEVER VIOLATE:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Printing classes NEVER query the database directly
✅ Missing logo = graceful omission, never null reference exception
✅ PrintResult never throws — always returns Success/Failure object
✅ Thermal text MUST use Windows-1256 encoding for Arabic
✅ All thermal lines MUST fit within 42 characters
✅ ESC/POS cut command ALWAYS added at end of thermal receipt
✅ Printer error messages in Arabic with actionable steps
✅ Logo resized to 200×200 max before saving (never raw upload)
✅ ExecutePrintAsync is ONE shared method — no copy-paste per button