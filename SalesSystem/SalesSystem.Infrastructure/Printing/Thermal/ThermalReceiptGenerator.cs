using System.Text;
using SalesSystem.Application.Printing.Contracts;

namespace SalesSystem.Infrastructure.Printing.Thermal;

/// <summary>
/// Generates ESC/POS commands for 80mm thermal receipt printers.
/// All text encoded in Windows-1256 for Arabic character support.
/// 80mm thermal printer = 42 characters per line (at 12pt monospace).
/// </summary>
public class ThermalReceiptGenerator
{
    private const int LineWidth = 42;
    private const char Separator = '-';
    private const char DoubleSeparator = '=';

    public byte[] GenerateEscPosCommands(InvoicePrintDto data, int escPosCodePage = 1256)
    {
        var encoding = Encoding.GetEncoding(escPosCodePage);
        var commands = new List<byte[]>();

        // Pass encoding to all EscPos.PrintLine calls
        byte[] PrintLine(string text) => EscPos.PrintLine(text, encoding);

        // ─── Initialize printer ─────────────────
        commands.Add(EscPos.Initialize());

        // ─── Header ────────────────────────────
        commands.Add(EscPos.SetAlignment(Alignment.Center));
        commands.Add(EscPos.SetBold(true));
        commands.Add(EscPos.SetFontSize(2));

        // NOTE: ESC/POS raw text printing cannot render bitmap logos.
        // Logo rendering requires printer-specific GS v 0 commands which are not universally supported.
        // When ShowLogo is enabled, the store name is printed as prominent text instead.
        // For logo printing, consider using the A4 print mode or a dedicated POS graphic driver.

        var storeName = TruncateCenter(data.StoreName, LineWidth);
        commands.Add(PrintLine(storeName));

        commands.Add(EscPos.SetFontSize(1));
        commands.Add(EscPos.SetBold(false));

        if (!string.IsNullOrWhiteSpace(data.StorePhone))
            commands.Add(PrintLine(data.StorePhone));

        if (!string.IsNullOrWhiteSpace(data.StoreAddress))
        {
            foreach (var line in WrapText(data.StoreAddress, LineWidth))
                commands.Add(PrintLine(line));
        }

        if (!string.IsNullOrWhiteSpace(data.StoreTaxNumber))
            commands.Add(PrintLine($"ض: {data.StoreTaxNumber}"));

        commands.Add(PrintLine(new string(DoubleSeparator, LineWidth)));

        // ─── Invoice info ──────────────────────
        commands.Add(EscPos.SetAlignment(Alignment.Right));
        commands.Add(PrintLine(
            FormatTwoColumns("رقم الفاتورة:", data.InvoiceNumber)));
        commands.Add(PrintLine(
            FormatTwoColumns("التاريخ:", data.InvoiceDate.ToString("dd/MM/yyyy HH:mm"))));
        commands.Add(PrintLine(
            FormatTwoColumns("العميل:", TruncateRight(data.CustomerOrSupplierName, 20))));

        commands.Add(PrintLine(new string(Separator, LineWidth)));

        // ─── Column headers ────────────────────
        commands.Add(EscPos.SetBold(true));
        commands.Add(PrintLine(FormatItemHeader()));
        commands.Add(EscPos.SetBold(false));
        commands.Add(PrintLine(new string(Separator, LineWidth)));

        // ─── Items ─────────────────────────────
        foreach (var item in data.Items)
        {
            var name = TruncateRight(item.ProductName, LineWidth - 2);
            commands.Add(PrintLine($"  {name}"));

            var itemLine = FormatItemLine(
                item.UnitName,
                item.Quantity,
                item.UnitPrice,
                item.Total);
            commands.Add(PrintLine(itemLine));

            if (item.Discount > 0)
                commands.Add(PrintLine(
                    FormatTwoColumns("  خصم:", $"-{item.Discount:N2}")));

            if (data.ShowExpiryInInvoices && item.ExpiryDate.HasValue)
                commands.Add(PrintLine(
                    FormatTwoColumns("  انتهاء:", item.ExpiryDate.Value.ToString("dd/MM/yyyy"))));

            if (data.PrintBarcode && !string.IsNullOrWhiteSpace(item.Barcode))
                commands.Add(PrintLine(
                    FormatTwoColumns("  باركود:", item.Barcode)));
        }

        commands.Add(PrintLine(new string(DoubleSeparator, LineWidth)));

        // ─── Totals ────────────────────────────
        if (data.DiscountAmount > 0)
            commands.Add(PrintLine(
                FormatTwoColumns("الخصم:", $"-{data.DiscountAmount:N2}")));

        // Other Charges (delivery, shipping — only if exists)
        if (data.OtherCharges > 0)
            commands.Add(PrintLine(
                FormatTwoColumns("مصاريف إضافية:", $"+{data.OtherCharges:N2}")));

        commands.Add(PrintLine(
            FormatTwoColumns($"ض.ق.م ({data.TaxRate:N0}%):", $"{data.TaxAmount:N2}")));

        commands.Add(EscPos.SetBold(true));
        commands.Add(EscPos.SetFontSize(2));
        commands.Add(PrintLine(
            FormatTwoColumns("الإجمالي:", $"{data.GrandTotal:N2} ر.س")));
        commands.Add(EscPos.SetFontSize(1));
        commands.Add(EscPos.SetBold(false));

        if (data.ShowBalanceOnPrint)
        {
            commands.Add(PrintLine(
                FormatTwoColumns("المدفوع:", $"{data.AmountPaid:N2}")));
            if (data.ChangeAmount > 0)
                commands.Add(PrintLine(
                    FormatTwoColumns("الباقي:", $"{data.ChangeAmount:N2}")));
        }

        commands.Add(PrintLine(new string(DoubleSeparator, LineWidth)));

        // ─── Footer ────────────────────────────
        commands.Add(EscPos.SetAlignment(Alignment.Center));
        var footerText = string.IsNullOrWhiteSpace(data.FooterNote)
            ? "شكراً لتعاملكم معنا"
            : data.FooterNote;
        commands.Add(PrintLine(footerText));
        commands.Add(PrintLine(string.Empty));

        // ─── Signature ─────────────────────────
        if (data.PrintSignature)
        {
            commands.Add(PrintLine(string.Empty));
            commands.Add(PrintLine("التوقيع: _______________"));
            commands.Add(PrintLine(string.Empty));
        }

        // ─── Cut paper ─────────────────────────
        commands.Add(EscPos.CutPaper());

        return commands.SelectMany(b => b).ToArray();
    }

    // ─── Text Formatting Helpers ──────────────

    /// <summary>
    /// "Label:          Value" aligned to fill exactly LineWidth characters.
    /// </summary>
    private string FormatTwoColumns(string label, string value)
    {
        var totalLength = label.Length + value.Length;
        var spaces = Math.Max(1, LineWidth - totalLength);
        return label + new string(' ', spaces) + value;
    }

    private string FormatItemHeader()
    {
        return "الوحدة".PadLeft(8) +
               "الكمية".PadLeft(8) +
               "السعر".PadLeft(9) +
               "المجموع".PadLeft(9);
    }

    private string FormatItemLine(string unit, decimal qty, decimal price, decimal total)
    {
        return unit.PadLeft(8) +
               qty.ToString("N1").PadLeft(8) +
               price.ToString("N2").PadLeft(9) +
               total.ToString("N2").PadLeft(9);
    }

    private static string TruncateRight(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength];

    private static string TruncateCenter(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        var half = (maxLength - 3) / 2;
        return text[..half] + "..." + text[^half..];
    }

    private static IEnumerable<string> WrapText(string text, int lineWidth)
    {
        for (int i = 0; i < text.Length; i += lineWidth)
            yield return text.Substring(i, Math.Min(lineWidth, text.Length - i));
    }
}

// ─── ESC/POS Command Builder ───────────────────

public static class EscPos
{
    public static byte[] Initialize()
        => new byte[] { 0x1B, 0x40 };

    public static byte[] CutPaper()
        => new byte[] { 0x1D, 0x56, 0x42, 0x00 };

    public static byte[] SetBold(bool bold)
        => bold
            ? new byte[] { 0x1B, 0x45, 0x01 }
            : new byte[] { 0x1B, 0x45, 0x00 };

    public static byte[] SetAlignment(Alignment alignment)
    {
        byte code = alignment switch
        {
            Alignment.Left => 0x00,
            Alignment.Center => 0x01,
            Alignment.Right => 0x02,
            _ => 0x00
        };
        return new byte[] { 0x1B, 0x61, code };
    }

    public static byte[] SetFontSize(int multiplier)
    {
        byte size = multiplier <= 1 ? (byte)0x00 : (byte)0x11;
        return new byte[] { 0x1D, 0x21, size };
    }

    public static byte[] PrintLine(string text, Encoding? encoding = null)
    {
        encoding ??= Encoding.GetEncoding(1256);
        var textBytes = encoding.GetBytes(text);
        var newLine = new byte[] { 0x0A };
        return textBytes.Concat(newLine).ToArray();
    }
}

public enum Alignment
{
    Left,
    Center,
    Right
}
