using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace SalesSystem.DesktopPWF.Printing.Core;

/// <summary>
/// Shared utilities for GDI+ printing with RTL support
/// </summary>
public static class PrintHelper
{
    /// <summary>
    /// Standard RTL string format for Arabic text
    /// </summary>
    /// <summary>
    /// Standard RTL string format for Arabic text (Near = Right)
    /// </summary>
    public static readonly StringFormat RTLFormatRight = new()
    {
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.DirectionRightToLeft
    };

    /// <summary>
    /// LTR string format for English text or numbers
    /// </summary>
    public static readonly StringFormat LTRFormat = new()
    {
        Alignment = StringAlignment.Near,
        LineAlignment = StringAlignment.Center,
        FormatFlags = 0
    };

    /// <summary>
    /// RTL string format for centered Arabic text
    /// </summary>
    public static readonly StringFormat RTLCenterFormat = new()
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.DirectionRightToLeft
    };

    /// <summary>
    /// RTL string format for left-aligned Arabic text (Far in RTL context)
    /// </summary>
    public static readonly StringFormat RTLLeftFormat = new()
    {
        Alignment = StringAlignment.Far,
        LineAlignment = StringAlignment.Center,
        FormatFlags = StringFormatFlags.DirectionRightToLeft
    };

    /// <summary>
    /// Draws a line across the page
    /// </summary>
    public static void DrawLine(Graphics g, float y, float marginLeft, float pageWidth)
    {
        using var pen = new Pen(Color.Black, 1f);
        g.DrawLine(pen, marginLeft, y, pageWidth - marginLeft, y);
    }

    /// <summary>
    /// Loads an image safely from a path
    /// </summary>
    public static Image? LoadImage(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;

        try
        {
            return Image.FromFile(path);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Formats currency to 2 decimal places
    /// </summary>
    public static string FormatCurrency(decimal amount) => amount.ToString("N2");

    /// <summary>
    /// Formats quantity to 3 decimal places
    /// </summary>
    public static string FormatQuantity(decimal qty) => qty.ToString("N3");

    /// <summary>
    /// Converts decimal amount to Arabic words (basic placeholder)
    /// </summary>
    public static string ToWord(decimal amount)
    {
        return $"{amount:N2} ريال فقط لا غير";
    }
}
