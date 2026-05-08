using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Common;

public partial class SummaryCardControl : UserControl
{
    private Label _lblTitle;
    private Label _lblValue;
    private Color _accentColor = Color.FromArgb(33, 150, 243);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title { get => _lblTitle.Text; set => _lblTitle.Text = value; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Value { get => _lblValue.Text; set => _lblValue.Text = value; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor { get => _accentColor; set { _accentColor = value; Invalidate(); } }

    public SummaryCardControl()
    {
        this.Size = new Size(160, 80);
        this.BackColor = Color.White;
        this.Padding = new Padding(10, 10, 10, 10);

        _lblTitle = new Label
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.Gray,
            Height = 20
        };

        _lblValue = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.Black,
            TextAlign = ContentAlignment.BottomLeft
        };

        this.Controls.Add(_lblValue);
        this.Controls.Add(_lblTitle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var brush = new SolidBrush(_accentColor);
        e.Graphics.FillRectangle(brush, 0, 0, 4, this.Height);
    }
}
