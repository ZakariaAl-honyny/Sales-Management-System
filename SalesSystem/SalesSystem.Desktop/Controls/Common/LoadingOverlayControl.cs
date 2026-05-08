using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Common;

public partial class LoadingOverlayControl : UserControl
{
    private Label _lblLoading;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string LoadingText
    {
        get => _lblLoading.Text;
        set => _lblLoading.Text = value;
    }

    public LoadingOverlayControl()
    {
        this.Dock = DockStyle.Fill;
        this.Visible = false;
        
        _lblLoading = new Label
        {
            Text = "جاري التحميل...",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 150, 243),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        this.Controls.Add(_lblLoading);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var brush = new SolidBrush(Color.FromArgb(180, Color.White));
        e.Graphics.FillRectangle(brush, this.ClientRectangle);
    }

    public void ShowOverlay() => this.Visible = true;
    public void HideOverlay() => this.Visible = false;
}
