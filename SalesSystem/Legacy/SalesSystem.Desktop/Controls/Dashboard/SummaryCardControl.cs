using System.Drawing.Drawing2D;
using SalesSystem.Desktop.Helpers;

namespace SalesSystem.Desktop.Controls.Dashboard;

public class SummaryCardControl : UserControl
{
    private Label lblTitle = null!;
    private Label lblValue = null!;
    private Color _accentColor;

    public SummaryCardControl(string title, string initialValue, Color accentColor)
    {
        _accentColor = accentColor;
        InitializeComponent(title, initialValue);
    }

    private void InitializeComponent(string title, string initialValue)
    {
        this.Size = new Size(240, 110);
        this.BackColor = Color.White;
        this.Padding = new Padding(15);
        this.Margin = new Padding(10);
        this.DoubleBuffered = true;

        lblTitle = new Label { 
            Text = title, 
            Dock = DockStyle.Top, 
            Height = 25, 
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = ThemeHelper.TextSecondary,
            TextAlign = ContentAlignment.TopRight
        };

        lblValue = new Label { 
            Text = initialValue, 
            Dock = DockStyle.Fill, 
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = ThemeHelper.TextPrimary,
            TextAlign = ContentAlignment.BottomLeft
        };

        this.Controls.Add(lblValue);
        this.Controls.Add(lblTitle);

        this.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Draw accent bar at the bottom
            using var brush = new SolidBrush(_accentColor);
            e.Graphics.FillRectangle(brush, 0, this.Height - 4, this.Width, 4);

            // Draw subtle border
            using var pen = new Pen(Color.FromArgb(230, 233, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        };
    }

    public void UpdateValue(string value)
    {
        if (this.InvokeRequired) this.Invoke(() => lblValue.Text = value);
        else lblValue.Text = value;
    }
}

