using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Payments;

public partial class PaymentsListControl : UserControl
{
    private readonly ICustomerPaymentApiService _customerPaymentApi;
    private readonly ISupplierPaymentApiService _supplierPaymentApi;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notification;
    
    private TabControl tabPayments = null!;
    private TabPage tpCustomers = null!;
    private TabPage tpSuppliers = null!;
    private DataGridView dgvCustomerPayments = null!;
    private DataGridView dgvSupplierPayments = null!;
    private BindingSource _customerSource = new();
    private BindingSource _supplierSource = new();
    private IDisposable? _sub1, _sub2;

    public PaymentsListControl(
        ICustomerPaymentApiService customerPaymentApi,
        ISupplierPaymentApiService supplierPaymentApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        INotificationService notification)
    {
        _customerPaymentApi = customerPaymentApi;
        _supplierPaymentApi = supplierPaymentApi;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _notification = notification;

        InitializeComponent();
        SetupGrids();
    }

    private void SetupGrids()
    {
        this.RightToLeft = RightToLeft.Yes;
        
        dgvCustomerPayments.DataSource = _customerSource;
        dgvCustomerPayments.AutoGenerateColumns = false;
        dgvCustomerPayments.ReadOnly = true;
        dgvCustomerPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentNumber", HeaderText = "رقم السند", Width = 120 });
        dgvCustomerPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentDate", HeaderText = "التاريخ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvCustomerPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "العميل", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvCustomerPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "المبلغ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", ForeColor = Color.Green, Font = new Font(dgvCustomerPayments.Font, FontStyle.Bold) } });

        dgvSupplierPayments.DataSource = _supplierSource;
        dgvSupplierPayments.AutoGenerateColumns = false;
        dgvSupplierPayments.ReadOnly = true;
        dgvSupplierPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentNumber", HeaderText = "رقم السند", Width = 120 });
        dgvSupplierPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentDate", HeaderText = "التاريخ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvSupplierPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SupplierName", HeaderText = "المورد", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvSupplierPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "المبلغ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", ForeColor = Color.Red, Font = new Font(dgvSupplierPayments.Font, FontStyle.Bold) } });
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _sub1 = _eventBus.Subscribe<CustomerPaymentChangedMessage>(async _ => await LoadCustomerPaymentsAsync());
        _sub2 = _eventBus.Subscribe<SupplierPaymentChangedMessage>(async _ => await LoadSupplierPaymentsAsync());
        await Task.WhenAll(LoadCustomerPaymentsAsync(), LoadSupplierPaymentsAsync());
    }

    private async Task LoadCustomerPaymentsAsync()
    {
        var res = await _customerPaymentApi.GetAllAsync();
        if (res.IsSuccess) _customerSource.DataSource = res.Value.OrderByDescending(x => x.PaymentDate).ToList();
    }

    private async Task LoadSupplierPaymentsAsync()
    {
        var res = await _supplierPaymentApi.GetAllAsync();
        if (res.IsSuccess) _supplierSource.DataSource = res.Value.OrderByDescending(x => x.PaymentDate).ToList();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var pnlTop = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(5) };
        var btnAddCustomer = new Button { Text = "سند قبض جديد", Width = 120, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White };
        btnAddCustomer.Click += (s, e) => { var dlg = _serviceProvider.GetRequiredService<CustomerPaymentDialog>(); dlg.ShowDialog(); };
        var btnAddSupplier = new Button { Text = "سند صرف جديد", Width = 120, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White };
        btnAddSupplier.Click += (s, e) => { var dlg = _serviceProvider.GetRequiredService<SupplierPaymentDialog>(); dlg.ShowDialog(); };
        pnlTop.Controls.AddRange(new Control[] { btnAddSupplier, btnAddCustomer });

        tabPayments = new TabControl { Dock = DockStyle.Fill };
        tpCustomers = new TabPage("سندات القبض (عملاء)");
        tpSuppliers = new TabPage("سندات الصرف (موردين)");
        dgvCustomerPayments = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White };
        dgvSupplierPayments = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White };
        
        tpCustomers.Controls.Add(dgvCustomerPayments);
        tpSuppliers.Controls.Add(dgvSupplierPayments);
        tabPayments.TabPages.AddRange(new TabPage[] { tpCustomers, tpSuppliers });

        mainLayout.Controls.Add(pnlTop, 0, 0);
        mainLayout.Controls.Add(tabPayments, 0, 1);
        this.Controls.Add(mainLayout);
    }

    protected override void Dispose(bool disposing) { if (disposing) { _sub1?.Dispose(); _sub2?.Dispose(); } base.Dispose(disposing); }
}
