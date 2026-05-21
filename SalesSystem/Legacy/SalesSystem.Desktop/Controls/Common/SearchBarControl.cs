using SalesSystem.Desktop.Helpers;
using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Common;

public sealed class SearchBarControl : UserControl
{
    private readonly TextBox _txtSearch;
    private readonly Label _lblIcon;
    private readonly System.Windows.Forms.Timer _debounceTimer;
    private string _placeholderText = "\u0627\u0628\u062D\u062B \u0647\u0646\u0627...";

    public event EventHandler<string>? SearchChanged;

    [Category("Appearance")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Placeholder
    {
        get => _placeholderText;
        set 
        { 
            _placeholderText = value;
            if (!ContainsFocus && string.IsNullOrWhiteSpace(_txtSearch.Text))
                SetPlaceholder();
        }
    }

    [Browsable(false)]
    public string SearchText => _txtSearch.Text == _placeholderText ? string.Empty : _txtSearch.Text;

    public SearchBarControl()
    {
        Size = new Size(300, 38);
        Padding = new Padding(12, 8, 12, 8);
        BackColor = Color.White;
        RightToLeft = RightToLeft.Yes;

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10F),
            ForeColor = ThemeHelper.TextPrimary
        };

        _lblIcon = new Label
        {
            Dock = DockStyle.Left,
            Width = 24,
            Text = "\uD83D\uDD0D", // Search icon
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10F),
            ForeColor = ThemeHelper.TextSecondary,
            Margin = new Padding(0, 0, 8, 0)
        };

        _debounceTimer = new System.Windows.Forms.Timer { Interval = 400 };
        _debounceTimer.Tick += (s, e) => 
        {
            _debounceTimer.Stop();
            SearchChanged?.Invoke(this, SearchText);
        };

        _txtSearch.TextChanged += (s, e) =>
        {
            if (_txtSearch.Text != _placeholderText)
            {
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        };

        _txtSearch.Enter += (s, e) =>
        {
            if (_txtSearch.Text == _placeholderText)
            {
                _txtSearch.Text = string.Empty;
                _txtSearch.ForeColor = ThemeHelper.TextPrimary;
            }
            this.Invalidate(); // Redraw border for focus
        };

        _txtSearch.Leave += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_txtSearch.Text))
            {
                SetPlaceholder();
            }
            this.Invalidate(); // Redraw border for blur
        };

        Controls.Add(_txtSearch);
        Controls.Add(_lblIcon);
        
        SetPlaceholder();
    }

    private void SetPlaceholder()
    {
        _txtSearch.Text = _placeholderText;
        _txtSearch.ForeColor = ThemeHelper.TextSecondary;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        
        var borderColor = _txtSearch.Focused ? ThemeHelper.Primary : Color.FromArgb(224, 227, 231);
        using var pen = new Pen(borderColor, 1.5f);
        
        // Draw rounded rectangle border
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        e.Graphics.DrawRectangle(pen, rect);
    }
}



