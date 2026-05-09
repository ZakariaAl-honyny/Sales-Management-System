using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Controls.Common;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Payments;

public partial class SupplierPaymentsListControl : UserControl
{
    private readonly ISupplierPaymentApiService _apiService;
    private readonly ISupplierApiService _supplierApi;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();
    private IDisposable? _subscription;
    
    private TextBox txtSearch = null!;
    private Button btnSearch = null!;
    private Button btnRefresh = null!;
    private Button btnAdd = null!;
    private DataGridView dgvPayments = null!;
    private Label lblStatusLabel = null!;
    private DateTimePicker dtpFrom = null!;
    private DateTimePicker dtpTo = null!;
    private ComboBox cmbSupplierFilter = null!;

    public SupplierPaymentsListControl(
        ISupplierPaymentApiService apiService,
        ISupplierApiService supplierApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        INotificationService notification)
    {
        _apiService = apiService;
        _supplierApi = supplierApi;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _notification = notification;
        
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
        dgvPayments.DataSource = _bindingSource;
        dgvPayments.ReadOnly = true;
        dgvPayments.AllowUserToAddRows = false;
        dgvPayments.BackgroundColor = Color.White;
        dgvPayments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _subscription = _eventBus.Subscribe<SupplierPaymentChangedMessage>(async _ => await LoadPaymentsAsync());
        await LoadPaymentsAsync();
    }

    private async Task LoadPaymentsAsync()
    {
        try
        {
            SetBusy(true);
            var result = await _apiService.GetAllAsync(null);
            
            if (result.IsSuccess)
            {
                _bindingSource.DataSource = result.Value;
                lblStatusLabel.Text = $"ط¹ط¯ط¯ ط§ظ„ط³ظ†ط¯ط§طھ: {result.Value.Count}";
                FormatGrid();
            }
            else
            {
                _notification.ShowError(result.Error!);
            }
        }
        catch (Exception ex)
        {
            _notification.ShowError("ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، طھط­ظ…ظٹظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void FormatGrid()
    {
        if (dgvPayments.Columns.Count == 0) return;
        var hides = new[] { "SupplierId", "CreatedByUserId" };
        foreach (var h in hides)
        {
            if (dgvPayments.Columns.Contains(h)) dgvPayments.Columns[h].Visible = false;
        }

        SetHeader("VoucherNo", "ط±ظ‚ظ… ط§ظ„ط³ظ†ط¯");
        SetHeader("SupplierName", "ط§ظ„ظ…ظˆط±ط¯");
        SetHeader("Amount", "ط§ظ„ظ…ط¨ظ„ط؛");
        SetHeader("PaymentDate", "ط§ظ„طھط§ط±ظٹط®");
        SetHeader("PaymentMethod", "ط·ط±ظٹظ‚ط© ط§ظ„ط¯ظپط¹");
        SetHeader("Notes", "ظ…ظ„ط§ط­ط¸ط§طھ");
    }

    private void SetHeader(string col, string text)
    {
        if (dgvPayments.Columns.Contains(col)) dgvPayments.Columns[col].HeaderText = text;
    }

    private void SetBusy(bool busy)
    {
        txtSearch.Enabled = !busy;
        btnSearch.Enabled = !busy;
        btnRefresh.Enabled = !busy;
        btnAdd.Enabled = !busy;
        dgvPayments.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;

        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(10, 5, 10, 5) };
        dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100 };
        dtpFrom.Value = DateTime.Today.AddDays(-30);
        dtpTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100 };
        dtpTo.Value = DateTime.Today;
        cmbSupplierFilter = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbSupplierFilter.Items.Add("ط§ظ„ظƒظ„");

        txtSearch = new TextBox { Width = 150, PlaceholderText = "ط¨ط­ط«..." };
        btnSearch = new Button { Text = "ط¨ط­ط«", Width = 60, FlatStyle = FlatStyle.Flat };
        btnSearch.Click += async (_, _) => await LoadPaymentsAsync();

        btnRefresh = new Button { Text = "طھط­ط¯ظٹط«", Width = 60, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (_, _) => await LoadPaymentsAsync();

        var filterFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        filterFlow.Controls.AddRange(new Control[] { btnRefresh, btnSearch, txtSearch, cmbSupplierFilter, new Label { Text = "ط¥ظ„ظ‰:", AutoSize = true }, dtpTo, new Label { Text = "ظ…ظ†:", AutoSize = true }, dtpFrom });
        filterPanel.Controls.Add(filterFlow);

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10, 5, 10, 5) };
        btnAdd = new Button { Text = "ط³ظ†ط¯ طµط±ظپ ط¬ط¯ظٹط¯", Width = 100, FlatStyle = FlatStyle.Flat, BackColor = Color.LightGreen };
        btnAdd.Click += (_, _) => ShowEditor();

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        flow.Controls.AddRange(new Control[] { btnAdd });
        topPanel.Controls.Add(flow);

        dgvPayments = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = true };
        lblStatusLabel = new Label { Dock = DockStyle.Bottom, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Text = "ط¬ط§ظ‡ط²" };

        this.Controls.Add(dgvPayments);
        this.Controls.Add(lblStatusLabel);
        this.Controls.Add(topPanel);
        this.Controls.Add(filterPanel);
    }

    private void ShowEditor()
    {
        using var diag = ActivatorUtilities.CreateInstance<SupplierPaymentDialog>(_serviceProvider);
        if (diag.ShowDialog() == DialogResult.OK) _eventBus.Publish(new SupplierPaymentChangedMessage());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription?.Dispose();
        base.Dispose(disposing);
    }
}





