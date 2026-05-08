using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Common;

public sealed class SummaryCardControl : UserControl
{
    private readonly Label _lblTitle;
    private readonly Label _lblValue;
    private readonly PictureBox _picIcon;
    private Color _accentColor = Color.FromArgb(33, 150, 243);

    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Title { get => _lblTitle.Text; set => _lblTitle.Text = value; }

    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Value { get => _lblValue.Text; set => _lblValue.Text = value; }

    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Image? Icon { get => _picIcon.Image; set => _picIcon.Image = value; }

    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color AccentColor 
    { 
        get => _accentColor; 
        set { _accentColor = value; Invalidate(); } 
    }

    public SummaryCardControl()
    {
        Size = new Size(160, 80);
        BackColor = Color.White;
        Padding = new Padding(10, 5, 5, 5);

        _lblTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.TopRight
        };

        _lblValue = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.Black,
            TextAlign = ContentAlignment.MiddleRight
        };

        _picIcon = new PictureBox
        {
            Dock = DockStyle.Left,
            Width = 40,
            SizeMode = PictureBoxSizeMode.CenterImage
        };

        Controls.Add(_lblValue);
        Controls.Add(_lblTitle);
        Controls.Add(_picIcon);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Draw accent border on the right (since it's RTL)
        using var brush = new SolidBrush(_accentColor);
        e.Graphics.FillRectangle(brush, Width - 4, 0, 4, Height);
        
        // Draw shadow/border
        e.Graphics.DrawRectangle(Pens.LightGray, 0, 0, Width - 1, Height - 1);
    }
}
