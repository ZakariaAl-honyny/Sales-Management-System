using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Common;

public sealed class MoneyTextBox : TextBox
{
    [Browsable(false)]
    public decimal DecimalValue => decimal.TryParse(Text, out var v) ? v : 0m;

    public MoneyTextBox()
    {
        TextAlign = HorizontalAlignment.Left; // Numbers usually left-aligned even in RTL for readability if it's pure decimal
        // Actually PRD says decimal(18,2) or (18,3). Standard is 2.
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);

        // Allow digits, control keys (backspace), and one decimal point
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
        {
            e.Handled = true;
        }

        // Only allow one decimal point
        if (e.KeyChar == '.' && Text.Contains('.'))
        {
            e.Handled = true;
        }
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        if (decimal.TryParse(Text, out var value))
        {
            Text = value.ToString("F2");
        }
        else
        {
            Text = "0.00";
        }
    }
}
