using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Common;

public sealed class LoadingOverlayControl : UserControl
{
    private readonly Label _lblLoading;
    private string _loadingText = "\u062C\u0627\u0631\u064A \u0627\u0644\u062A\u062D\u0645\u064A\u0644...";

    [Category("Appearance")]
    public string LoadingText
    {
        get => _loadingText;
        set 
        { 
            _loadingText = value;
            _lblLoading.Text = value;
        }
    }

    public LoadingOverlayControl()
    {
        Dock = DockStyle.Fill;
        Visible = false;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;

        _lblLoading = new Label
        {
            Text = _loadingText,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 150, 243),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.Add(_lblLoading);
        Resize += (s, e) => CenterLabel();
    }

    private void CenterLabel()
    {
        _lblLoading.Location = new Point(
            (Width - _lblLoading.Width) / 2,
            (Height - _lblLoading.Height) / 2
        );
    }

    public new void Show()
    {
        BringToFront();
        Visible = true;
    }

    public new void Hide()
    {
        Visible = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Draw semi-transparent background
        using var brush = new SolidBrush(Color.FromArgb(180, Color.White));
        e.Graphics.FillRectangle(brush, ClientRectangle);
        base.OnPaint(e);
    }
}
