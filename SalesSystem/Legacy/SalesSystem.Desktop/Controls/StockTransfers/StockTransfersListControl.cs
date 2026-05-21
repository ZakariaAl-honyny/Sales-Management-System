using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Forms;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.StockTransfers;

[System.ComponentModel.DesignerCategory("Code")]
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
                StatusText = x.Status switch { 1 => "مسودة", 2 => "مرحل", 3 => "ملغي", _ => "?" },
                Original = x
            }).ToList();
        }
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.White;

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0), Margin = new Padding(0) };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));

        var topPanel = new Panel { Dock = DockStyle.Fill };
        ThemeHelper.ApplyToolbarStyle(topPanel);

        var toolbar = new FlowLayoutPanel { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        btnAdd = new Button { Text = "تحويل مخزني جديد", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnAdd, ThemeHelper.ButtonType.Primary);
        btnAdd.Click += (s, e) => ShowEditor();

        btnRefresh = new Button { Text = "تحديث", Margin = new Padding(8, 0, 8, 0) };
        ThemeHelper.ApplyButtonStyle(btnRefresh, ThemeHelper.ButtonType.Ghost);
        btnRefresh.Click += async (s, e) => await LoadTransfersAsync();

        toolbar.Controls.AddRange(new Control[] { btnAdd, btnRefresh });
        topPanel.Controls.Add(toolbar);

        dgvTransfers = new DataGridView { Dock = DockStyle.Fill };
        ThemeHelper.ApplyDataGridViewStyle(dgvTransfers);
        
        var lblStatus = new Label { 
            Dock = DockStyle.Fill, 
            TextAlign = ContentAlignment.MiddleLeft, 
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(0),
            Text = "جاهز",
            Font = new Font("Segoe UI", 9F),
            ForeColor = ThemeHelper.TextSecondary,
            BackColor = Color.FromArgb(248, 249, 250)
        };

        mainLayout.Controls.Add(topPanel, 0, 0);
        mainLayout.Controls.Add(dgvTransfers, 0, 1);
        mainLayout.Controls.Add(lblStatus, 0, 2);

        this.Controls.Add(mainLayout);
    }

    private void ShowEditor()
    {
        StockTransferDto? p = null;
        if (dgvTransfers.CurrentRow?.DataBoundItem is object obj)
        {
            dynamic d = obj;
            p = d.Original;
        }
        var form = _serviceProvider.GetRequiredService<StockTransferForm>();
        form.LoadData(p);
        form.ShowDialog();
    }

    protected override void Dispose(bool disposing) { if (disposing) _subscription?.Dispose(); base.Dispose(disposing); }
}
