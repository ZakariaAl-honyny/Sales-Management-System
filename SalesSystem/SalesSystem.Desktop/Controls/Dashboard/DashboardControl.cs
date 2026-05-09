using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Contracts.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace SalesSystem.Desktop.Controls.Dashboard;

public partial class DashboardControl : UserControl
{
    private readonly IDashboardApiService _dashboardApi;
    private readonly IEventBus _eventBus;
    private readonly ISessionService _sessionService;
    
    private FlowLayoutPanel pnlCards = null!;
    private SummaryCardControl cardSales = null!;
    private SummaryCardControl cardPurchases = null!;
    private SummaryCardControl cardCustomers = null!;
    private SummaryCardControl cardLowStock = null!;
    private SummaryCardControl cardReceivables = null!;
    private SummaryCardControl cardPayables = null!;
    
    private IDisposable? _sub1, _sub2, _sub3;

    public DashboardControl(
        IDashboardApiService dashboardApi,
        IEventBus eventBus,
        ISessionService sessionService)
    {
        _dashboardApi = dashboardApi;
        _eventBus = eventBus;
        _sessionService = sessionService;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.RightToLeft = RightToLeft.Yes;
        this.BackColor = Color.FromArgb(245, 246, 250);

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F)); // Cards
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Charts/Recent (Placeholder)

        pnlCards = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), FlowDirection = FlowDirection.RightToLeft };
        
        cardSales = new SummaryCardControl("مبيعات اليوم", "0.00", Color.FromArgb(52, 152, 219));
        cardPurchases = new SummaryCardControl("مشتريات اليوم", "0.00", Color.FromArgb(231, 76, 60));
        cardCustomers = new SummaryCardControl("العملاء النشطون", "0", Color.FromArgb(46, 204, 113));
        cardLowStock = new SummaryCardControl("أصناف منخفضة", "0", Color.FromArgb(241, 196, 15));
        cardReceivables = new SummaryCardControl("إجمالي المديونية (لنا)", "0.00", Color.FromArgb(155, 89, 182));
        cardPayables = new SummaryCardControl("إجمالي المديونية (علينا)", "0.00", Color.FromArgb(52, 73, 94));

        pnlCards.Controls.AddRange(new Control[] { cardSales, cardPurchases, cardCustomers, cardLowStock, cardReceivables, cardPayables });
        
        // Hide purchases/payables for Cashiers
        if (_sessionService.Current?.Role == SalesSystem.Contracts.Enums.UserRole.Cashier) // Cashier
        {
            cardPurchases.Visible = false;
            cardPayables.Visible = false;
        }

        var lblWelcome = new Label { 
            Text = $"مرحباً، {_sessionService.Current?.FullName ?? "المستخدم"}", 
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Dock = DockStyle.Top,
            Padding = new Padding(20, 10, 0, 0),
            Height = 50
        };

        mainLayout.Controls.Add(pnlCards, 0, 0);
        mainLayout.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 1); // Content area

        this.Controls.Add(mainLayout);
        this.Controls.Add(lblWelcome);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        _sub1 = _eventBus.Subscribe<SaleInvoiceChangedMessage>(async _ => await LoadDataAsync());
        _sub2 = _eventBus.Subscribe<PurchaseInvoiceChangedMessage>(async _ => await LoadDataAsync());
        _sub3 = _eventBus.Subscribe<StockChangedMessage>(async _ => await LoadDataAsync());
        
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try {
            var res = await _dashboardApi.GetSummaryAsync();
            if (res.IsSuccess)
            {
                var s = res.Value;
                cardSales.UpdateValue(s.TotalSalesToday.ToString("N2"));
                cardPurchases.UpdateValue(s.TotalPurchasesToday.ToString("N2"));
                cardCustomers.UpdateValue(s.ActiveCustomersCount.ToString());
                cardLowStock.UpdateValue(s.LowStockItemsCount.ToString());
                cardReceivables.UpdateValue(s.TotalReceivables.ToString("N2"));
                cardPayables.UpdateValue(s.TotalPayables.ToString("N2"));
            }
        } catch { /* Fail silently on dashboard refresh */ }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sub1?.Dispose();
            _sub2?.Dispose();
            _sub3?.Dispose();
        }
        base.Dispose(disposing);
    }
}
