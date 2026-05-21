using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Payments;

public partial class CustomerPaymentsListControl : UserControl
{
    private readonly ICustomerPaymentApiService _paymentApi;
    private readonly ICustomerApiService _customerApi;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly BindingSource _bindingSource = new();
    private IDisposable? _subscription;

    private DataGridView dgvPayments = null!;
    private ComboBox cmbCustomerFilter = null!;
    private DateTimePicker dtpFrom = null!;
    private DateTimePicker dtpTo = null!;
    private Button btnSearch = null!;
    private Button btnNew = null!;

    public CustomerPaymentsListControl(
        ICustomerPaymentApiService paymentApi,
        ICustomerApiService customerApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider)
    {
        _paymentApi = paymentApi;
        _customerApi = customerApi;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;

        InitializeComponent();
        SetupGrid();
    }

    private void SetupGrid()
    {
        this.RightToLeft = RightToLeft.Yes;
        dgvPayments.DataSource = _bindingSource;
        dgvPayments.AutoGenerateColumns = false;
        dgvPayments.ReadOnly = true;
        dgvPayments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentNumber", HeaderText = "—ﬁ„ «·”‰œ", Width = 120 });
        dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentDate", HeaderText = "«· «—ÌŒ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "«·⁄„Ì·", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "«·„»·€", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", ForeColor = Color.Green, Font = new Font(dgvPayments.Font, FontStyle.Bold) } });
        dgvPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNo", HeaderText = "«·›« Ê—…", Width = 120 });
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadCustomers();
        _subscription = _eventBus.Subscribe<CustomerPaymentChangedMessage>(async _ => await LoadPaymentsAsync());
        await LoadPaymentsAsync();
    }

    private async Task LoadCustomers()
    {
        var res = await _customerApi.GetAllAsync();
        if (res.IsSuccess)
        {
            var list = res.Value.ToList();
            list.Insert(0, new CustomerDto(0, "", "«·ﬂ·", "", "", "", 0, 0, 0, true));
            cmbCustomerFilter.DataSource = list;
            cmbCustomerFilter.DisplayMember = "Name";
            cmbCustomerFilter.ValueMember = "Id";
        }
    }

    private async Task LoadPaymentsAsync()
    {
        int? customerId = cmbCustomerFilter.SelectedValue as int? == 0 ? null : (int?)cmbCustomerFilter.SelectedValue;
        var res = await _paymentApi.GetAllAsync(customerId, dtpFrom.Value.Date, dtpTo.Value.Date.AddDays(1));
        if (res.IsSuccess) _bindingSource.DataSource = res.Value.OrderByDescending(x => x.PaymentDate).ToList();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var pnlTop = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(5) };
        btnNew = new Button { Text = "”‰œ ÃœÌœ", Width = 100, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White };
        btnNew.Click += (s, e) => { var dlg = _serviceProvider.GetRequiredService<CustomerPaymentDialog>(); dlg.ShowDialog(); };
        
        btnSearch = new Button { Text = "»ÕÀ", Width = 80, Height = 35, FlatStyle = FlatStyle.Flat };
        btnSearch.Click += async (s, e) => await LoadPaymentsAsync();

        cmbCustomerFilter = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        dtpFrom = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short };
        dtpFrom.Value = DateTime.Today.AddDays(-30);
        dtpTo = new DateTimePicker { Width = 110, Format = DateTimePickerFormat.Short };

        pnlTop.Controls.AddRange(new Control[] { btnNew, btnSearch, dtpTo, new Label { Text = "≈·Ï:", AutoSize = true, Margin = new Padding(0, 7, 0, 0) }, dtpFrom, new Label { Text = "„‰:", AutoSize = true, Margin = new Padding(0, 7, 0, 0) }, cmbCustomerFilter, new Label { Text = "«·⁄„Ì·:", AutoSize = true, Margin = new Padding(0, 7, 0, 0) } });

        dgvPayments = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None };
        layout.Controls.Add(pnlTop, 0, 0);
        layout.Controls.Add(dgvPayments, 0, 1);
        this.Controls.Add(layout);
    }

    protected override void Dispose(bool disposing) { if (disposing) _subscription?.Dispose(); base.Dispose(disposing); }
}
