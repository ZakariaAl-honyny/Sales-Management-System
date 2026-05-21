using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Helpers;
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
        ThemeHelper.ApplyDataGridViewStyle(dgvCustomerPayments);
        dgvCustomerPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentNumber", HeaderText = "رقم السند", Width = 120 });
        dgvCustomerPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentDate", HeaderText = "التاريخ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvCustomerPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "العميل", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        dgvCustomerPayments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Amount", HeaderText = "المبلغ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", ForeColor = Color.Green, Font = new Font(dgvCustomerPayments.Font, FontStyle.Bold) } });

        dgvSupplierPayments.DataSource = _supplierSource;
        dgvSupplierPayments.AutoGenerateColumns = false;
        dgvSupplierPayments.ReadOnly = true;
        ThemeHelper.ApplyDataGridViewStyle(dgvSupplierPayments);
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
        this.BackColor = Color.White;

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0), Margin = new Padding(0) };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var pnlTop = new Panel { Dock = DockStyle.Fill };
        ThemeHelper.ApplyToolbarStyle(pnlTop);

        var toolbar = new FlowLayoutPanel { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft, 
            Padding = new Padding(5),
            WrapContents = false
        };

        var btnAddCustomer = new Button { Text = "سند قبض عميل", Width = 140, Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAddCustomer, ThemeHelper.ButtonType.Success);
        btnAddCustomer.Click += (s, e) => { var dlg = _serviceProvider.GetRequiredService<CustomerPaymentDialog>(); dlg.ShowDialog(); };

        var btnAddSupplier = new Button { Text = "سند صرف مورد", Width = 140, Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAddSupplier, ThemeHelper.ButtonType.Danger);
        btnAddSupplier.Click += (s, e) => { var dlg = _serviceProvider.GetRequiredService<SupplierPaymentDialog>(); dlg.ShowDialog(); };

        toolbar.Controls.AddRange(new Control[] { btnAddCustomer, btnAddSupplier });
        pnlTop.Controls.Add(toolbar);

        tabPayments = new TabControl { 
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F)
        };
        tpCustomers = new TabPage("مقبوضات العملاء") { BackColor = Color.White };
        tpSuppliers = new TabPage("مدفوعات الموردين") { BackColor = Color.White };
        
        dgvCustomerPayments = new DataGridView { Dock = DockStyle.Fill };
        dgvSupplierPayments = new DataGridView { Dock = DockStyle.Fill };
        
        tpCustomers.Controls.Add(dgvCustomerPayments);
        tpSuppliers.Controls.Add(dgvSupplierPayments);
        tabPayments.TabPages.AddRange(new TabPage[] { tpCustomers, tpSuppliers });

        mainLayout.Controls.Add(pnlTop, 0, 0);
        mainLayout.Controls.Add(tabPayments, 0, 1);
        this.Controls.Add(mainLayout);
    }

    protected override void Dispose(bool disposing) { if (disposing) { _sub1?.Dispose(); _sub2?.Dispose(); } base.Dispose(disposing); }
}
