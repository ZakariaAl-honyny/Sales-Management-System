using System.Drawing;

namespace SalesSystem.Desktop.Printing.Core;

public static class PrintHelper
{
    public static readonly StringFormat RTLFormatRight = new()
    {
        Alignment = StringAlignment.Near, // In RTL, Near is Right
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.DirectionRightToLeft
    };

    public static readonly StringFormat RTLFormatCenter = new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.DirectionRightToLeft
    };

    public static readonly StringFormat RTLFormatLeft = new()
    {
        Alignment = StringAlignment.Far, // In RTL, Far is Left
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.DirectionRightToLeft
    };

    public static void DrawRtlText(Graphics g, string text, Font font, Brush brush, RectangleF rect, StringFormat format)
    {
        g.DrawString(text, font, brush, rect, format);
    }

    public static string FormatCurrency(decimal amount) => amount.ToString("N2");
    public static string FormatQuantity(decimal qty) => qty.ToString("N3");

    public static void DrawLine(Graphics g, Pen pen, float x1, float y, float x2)
    {
        g.DrawLine(pen, x1, y, x2, y);
    }
    
    public static Image? LoadLogo(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        try
        {
            return Image.FromFile(path);
        }
        catch
        {
            return null;
        }
    }
}
