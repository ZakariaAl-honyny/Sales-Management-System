using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.StockTransfers;

public partial class StockTransfersListControl : UserControl
{
    private readonly IStockTransferApiService _transferApi;
    private readonly IEventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationService _notification;
    private readonly BindingSource _bindingSource = new();
    private IDisposable? _subscription;

    private DataGridView dgvTransfers = null!;
    private Button btnAdd = null!;
    private Button btnRefresh = null!;

    public StockTransfersListControl(
        IStockTransferApiService transferApi,
        IEventBus eventBus,
        IServiceProvider serviceProvider,
        INotificationService notification)
    {
        _transferApi = transferApi;
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
        _notification = notification;

        InitializeComponent();
        SetupGrid();
    }

    private void SetupGrid()
    {
        this.RightToLeft = RightToLeft.Yes;
        dgvTransfers.DataSource = _bindingSource;
        dgvTransfers.AutoGenerateColumns = false;
        dgvTransfers.ReadOnly = true;
        dgvTransfers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        
        dgvTransfers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TransferNo", HeaderText = "رقم التحويل", Width = 130 });
        dgvTransfers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TransferDate", HeaderText = "التاريخ", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
        dgvTransfers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FromWarehouseName", HeaderText = "من مستودع", Width = 150 });
        dgvTransfers.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ToWarehouseName", HeaderText = "إلى مستودع", Width = 150 });
        dgvTransfers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "الحالة", Width = 100, DataPropertyName = "StatusText" });
        
        dgvTransfers.CellFormatting += (s, e) => { if (dgvTransfers.Columns[e.ColumnIndex].Name == "Status" && e.Value != null) e.CellStyle.ForeColor = e.Value.ToString() == "مرحل" ? Color.Green : (e.Value.ToString() == "ملغي" ? Color.Red : Color.Blue); };
        dgvTransfers.DoubleClick += (s, e) => ShowEditor();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _subscription = _eventBus.Subscribe<StockTransferChangedMessage>(async _ => await LoadTransfersAsync());
        await LoadTransfersAsync();
    }

    private async Task LoadTransfersAsync()
    {
        var res = await _transferApi.GetAllAsync();
        if (res.IsSuccess)
        {
            _bindingSource.DataSource = res.Value.Select(x => new {
                x.Id, x.TransferNo, x.TransferDate, x.FromWarehouseName, x.ToWarehouseName,
                StatusText = x.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "؟" },
                Original = x
            }).ToList();
        }
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var pnlTop = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(5) };
        btnAdd = new Button { Text = "تحويل جديد", Width = 120, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(46, 204, 113), ForeColor = Color.White };
        btnAdd.Click += (s, e) => ShowEditor();
        btnRefresh = new Button { Text = "تحديث", Width = 100, Height = 35, FlatStyle = FlatStyle.Flat };
        btnRefresh.Click += async (s, e) => await LoadTransfersAsync();
        pnlTop.Controls.AddRange(new Control[] { btnRefresh, btnAdd });

        dgvTransfers = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None };
        layout.Controls.Add(pnlTop, 0, 0);
        layout.Controls.Add(dgvTransfers, 0, 1);
        this.Controls.Add(layout);
    }

    private void ShowEditor()
    {
        StockTransferDto? p = null;
        if (dgvTransfers.CurrentRow?.DataBoundItem is object obj)
        {
            dynamic d = obj;
            p = d.Original;
        }
        var form = ActivatorUtilities.CreateInstance<StockTransferForm>(_serviceProvider, p ?? (object)Type.Missing);
        form.ShowDialog();
    }

    protected override void Dispose(bool disposing) { if (disposing) _subscription?.Dispose(); base.Dispose(disposing); }
}
