using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Controls.Common;

namespace SalesSystem.Desktop.Controls.Settings;

public partial class SettingsListControl : UserControl
{
    private readonly ISettingsApiService _apiService;
    private readonly INotificationService _notification;
    
    private TextBox txtStoreName = null!;
    private TextBox txtPhone = null!;
    private TextBox txtAddress = null!;
    private NumericUpDown numTaxRate = null!;
    private Button btnSave = null!;

    public SettingsListControl(ISettingsApiService apiService, INotificationService notification)
    {
        _apiService = apiService;
        _notification = notification;
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30) };
        
        var lblTitle = new Label { Text = "إعدادات النظام", Font = new Font("Segoe UI", 16, FontStyle.Bold), Dock = DockStyle.Top, Height = 40 };
        var tbl = new TableLayoutPanel 
        { 
            ColumnCount = 2, 
            RowCount = 5, 
            Dock = DockStyle.Top, 
            Height = 300,
            Padding = new Padding(0, 20, 0, 0)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        txtStoreName = new TextBox { Width = 400 };
        txtPhone = new TextBox { Width = 400 };
        txtAddress = new TextBox { Width = 400 };
        numTaxRate = new NumericUpDown { Width = 100, DecimalPlaces = 2 };
        btnSave = new Button { Text = "حفظ التغييرات", Width = 150, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White };
        btnSave.Click += async (s, e) => await SaveSettings();

        AddRow(tbl, 0, "اسم المتجر:", txtStoreName);
        AddRow(tbl, 1, "رقم الهاتف:", txtPhone);
        AddRow(tbl, 2, "العنوان:", txtAddress);
        AddRow(tbl, 3, "نسبة الضريبة (%):", numTaxRate);
        AddRow(tbl, 4, "", btnSave);

        pnl.Controls.Add(tbl);
        pnl.Controls.Add(lblTitle);
        this.Controls.Add(pnl);
    }

    private void AddRow(TableLayoutPanel tbl, int row, string label, Control control)
    {
        if (!string.IsNullOrEmpty(label))
        {
            tbl.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        }
        tbl.Controls.Add(control, 1, row);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadSettings();
    }

    private async Task LoadSettings()
    {
        try
        {
            var result = await _apiService.GetSettingsAsync();
            if (result.IsSuccess)
            {
                var s = result.Value;
                txtStoreName.Text = s.StoreName;
                txtPhone.Text = s.Phone;
                txtAddress.Text = s.Address;
                numTaxRate.Value = s.DefaultTaxRate;
            }
        }
        catch (Exception ex)
        {
            _notification.ShowError("خطأ في تحميل الإعدادات: " + ex.Message);
        }
    }

    private async Task SaveSettings()
    {
        try
        {
            btnSave.Enabled = false;
            var settings = new UpdateSettingsRequest(txtStoreName.Text, txtAddress.Text, txtPhone.Text, null, "SAR", numTaxRate.Value);
            var result = await _apiService.UpdateSettingsAsync(settings);
            if (result.IsSuccess)
            {
                _notification.ShowSuccess("تم حفظ الإعدادات بنجاح");
            }
            else
            {
                _notification.ShowError(result.Error!);
            }
        }
        catch (Exception ex)
        {
            _notification.ShowError("خطأ في حفظ الإعدادات: " + ex.Message);
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }
}



