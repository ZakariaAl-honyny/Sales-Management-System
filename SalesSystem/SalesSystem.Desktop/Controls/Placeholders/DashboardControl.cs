using SalesSystem.Contracts.DTOs;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Common;

namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class DashboardControl : BaseModuleControl
{
    private readonly IDashboardApiService _apiService;
    private readonly INotificationService _notification;
    
    private FlowLayoutPanel _flpStats;
    private SummaryCardControl _cardSales;
    private SummaryCardControl _cardPurchases;
    private SummaryCardControl _cardLowStock;
    private SummaryCardControl _cardReceivables;

    public DashboardControl(IDashboardApiService apiService, INotificationService notification)
    {
        _apiService = apiService;
        _notification = notification;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this._flpStats = new System.Windows.Forms.FlowLayoutPanel();
        this._cardSales = new SalesSystem.Desktop.Controls.Common.SummaryCardControl();
        this._cardPurchases = new SalesSystem.Desktop.Controls.Common.SummaryCardControl();
        this._cardLowStock = new SalesSystem.Desktop.Controls.Common.SummaryCardControl();
        this._cardReceivables = new SalesSystem.Desktop.Controls.Common.SummaryCardControl();
        
        this.SuspendLayout();

        this._flpStats.Dock = System.Windows.Forms.DockStyle.Fill;
        this._flpStats.Padding = new System.Windows.Forms.Padding(20);
        this._flpStats.AutoScroll = true;
        this._flpStats.RightToLeft = RightToLeft.Yes;

        _cardSales.Title = "مبيعات اليوم"; _cardSales.Value = "0.00"; _cardSales.BackColor = Color.FromArgb(46, 204, 113);
        _cardPurchases.Title = "مشتريات اليوم"; _cardPurchases.Value = "0.00"; _cardPurchases.BackColor = Color.FromArgb(52, 152, 219);
        _cardLowStock.Title = "أصناف منخفضة"; _cardLowStock.Value = "0"; _cardLowStock.BackColor = Color.FromArgb(231, 76, 60);
        _cardReceivables.Title = "إجمالي المديونية"; _cardReceivables.Value = "0.00"; _cardReceivables.BackColor = Color.FromArgb(155, 89, 182);

        this._flpStats.Controls.AddRange(new Control[] { _cardSales, _cardPurchases, _cardLowStock, _cardReceivables });

        this.Controls.Add(this._flpStats);
        this.RightToLeft = RightToLeft.Yes;
        this.Size = new System.Drawing.Size(1000, 700);
        this.ResumeLayout(false);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await RefreshData();
    }

    private async Task RefreshData()
    {
        var result = await _apiService.GetSummaryAsync();
        if (result.IsSuccess)
        {
            var s = result.Value;
            _cardSales.Value = s.TotalSalesToday.ToString("N2");
            _cardPurchases.Value = s.TotalPurchasesToday.ToString("N2");
            _cardLowStock.Value = s.LowStockItemsCount.ToString();
            _cardReceivables.Value = s.TotalReceivables.ToString("N2");
        }
    }

    protected override void RegisterSubscriptions() { }
}
