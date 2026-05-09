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
        Size = new Size(300, 40);
        Padding = new Padding(5);
        BackColor = Color.White;

        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.Gray
        };

        _lblIcon = new Label
        {
            Dock = DockStyle.Right,
            Width = 30,
            Text = "\uD83D\uDD0D", // Search icon
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12F)
        };

        _debounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
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
                _txtSearch.ForeColor = Color.Black;
            }
        };

        _txtSearch.Leave += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_txtSearch.Text))
            {
                SetPlaceholder();
            }
        };

        Controls.Add(_txtSearch);
        Controls.Add(_lblIcon);
        
        SetPlaceholder();
    }

    private void SetPlaceholder()
    {
        _txtSearch.Text = _placeholderText;
        _txtSearch.ForeColor = Color.Gray;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.DrawRectangle(Pens.LightGray, 0, 0, Width - 1, Height - 1);
    }
}



