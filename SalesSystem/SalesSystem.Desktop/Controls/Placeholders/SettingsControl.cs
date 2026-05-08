using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;

namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class SettingsControl : BaseModuleControl
{
    private readonly ISettingsApiService _apiService;
    private readonly INotificationService _notification;
    
    private TextBox txtStoreName;
    private TextBox txtPhone;
    private TextBox txtAddress;
    private NumericUpDown numTaxRate;
    private Button btnSave;

    public SettingsControl(ISettingsApiService apiService, INotificationService notification)
    {
        _apiService = apiService;
        _notification = notification;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.txtStoreName = new TextBox();
        this.txtPhone = new TextBox();
        this.txtAddress = new TextBox();
        this.numTaxRate = new NumericUpDown();
        this.btnSave = new Button();
        
        var lblName = new Label { Text = "اسم المتجر:", Location = new Point(20, 20) };
        txtStoreName.Location = new Point(150, 17); txtStoreName.Size = new Size(300, 27);
        
        var lblPhone = new Label { Text = "الهاتف:", Location = new Point(20, 60) };
        txtPhone.Location = new Point(150, 57); txtPhone.Size = new Size(300, 27);
        
        var lblAddr = new Label { Text = "العنوان:", Location = new Point(20, 100) };
        txtAddress.Location = new Point(150, 97); txtAddress.Size = new Size(300, 27);
        
        var lblTax = new Label { Text = "نسبة الضريبة (%):", Location = new Point(20, 140) };
        numTaxRate.Location = new Point(150, 137); numTaxRate.Size = new Size(100, 27); numTaxRate.DecimalPlaces = 2;

        btnSave.Text = "حفظ الإعدادات"; btnSave.Location = new Point(150, 190); btnSave.Size = new Size(150, 35);
        btnSave.BackColor = Color.FromArgb(46, 204, 113); btnSave.ForeColor = Color.White; btnSave.FlatStyle = FlatStyle.Flat;
        btnSave.Click += async (s, e) => await SaveSettings();

        this.Controls.AddRange(new Control[] { lblName, txtStoreName, lblPhone, txtPhone, lblAddr, txtAddress, lblTax, numTaxRate, btnSave });
        this.RightToLeft = RightToLeft.Yes;
        this.Size = new System.Drawing.Size(1000, 700);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
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

    private async Task SaveSettings()
    {
        var settings = new StoreSettingsDto(1, txtStoreName.Text, txtPhone.Text, txtAddress.Text, null, "SAR", numTaxRate.Value, true);
        var result = await _apiService.UpdateSettingsAsync(settings);
        if (result.IsSuccess) _notification.ShowSuccess("تم حفظ الإعدادات بنجاح");
        else _notification.ShowError(result.Error!);
    }

    protected override void RegisterSubscriptions() { }
}
