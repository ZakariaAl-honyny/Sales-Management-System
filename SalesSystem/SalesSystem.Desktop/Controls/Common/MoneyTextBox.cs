namespace SalesSystem.Desktop.Controls.Common;

public class MoneyTextBox : TextBox
{
    public decimal DecimalValue => decimal.TryParse(this.Text, out var v) ? v : 0m;

    public MoneyTextBox()
    {
        this.TextAlign = HorizontalAlignment.Left;
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);

        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
        {
            e.Handled = true;
        }

        // only allow one decimal point
        if (e.KeyChar == '.' && this.Text.IndexOf('.') > -1)
        {
            e.Handled = true;
        }
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        if (decimal.TryParse(this.Text, out var val))
        {
            this.Text = val.ToString("F2");
        }
        else
        {
            this.Text = "0.00";
        }
    }
}
