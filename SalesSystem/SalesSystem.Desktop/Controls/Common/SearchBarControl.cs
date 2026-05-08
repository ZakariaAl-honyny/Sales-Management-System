using System.ComponentModel;

namespace SalesSystem.Desktop.Controls.Common;

public partial class SearchBarControl : UserControl
{
    private TextBox _txtSearch;
    private System.Windows.Forms.Timer _debounceTimer;
    private string _placeholder = "بحث...";

    public event EventHandler<string>? SearchChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Placeholder 
    { 
        get => _placeholder; 
        set { _placeholder = value; ResetPlaceholder(); } 
    }

    [Browsable(false)]
    public string SearchText => (_txtSearch != null && _txtSearch.Text == _placeholder) ? "" : (_txtSearch?.Text ?? "");

    public SearchBarControl()
    {
        this.Size = new Size(300, 35);
        
        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            Text = _placeholder,
            ForeColor = Color.Gray
        };

        _txtSearch.Enter += (s, e) => { if (_txtSearch.Text == _placeholder) { _txtSearch.Text = ""; _txtSearch.ForeColor = Color.Black; } };
        _txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) { ResetPlaceholder(); } };
        _txtSearch.TextChanged += (s, e) => { if (_txtSearch.Text != _placeholder) { _debounceTimer.Stop(); _debounceTimer.Start(); } };

        _debounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
        _debounceTimer.Tick += (s, e) => { _debounceTimer.Stop(); SearchChanged?.Invoke(this, SearchText); };

        this.Controls.Add(_txtSearch);
    }

    private void ResetPlaceholder()
    {
        if (_txtSearch != null)
        {
            _txtSearch.Text = _placeholder;
            _txtSearch.ForeColor = Color.Gray;
        }
    }
}
