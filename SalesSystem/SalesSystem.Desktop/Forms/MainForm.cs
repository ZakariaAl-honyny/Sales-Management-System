using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Models;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Messaging.Messages;
using SalesSystem.Desktop.Controls.Dashboard;
using SalesSystem.Desktop.Controls.Sales;
using SalesSystem.Desktop.Controls.Purchases;
using SalesSystem.Desktop.Controls.Returns;
using SalesSystem.Desktop.Controls.Inventory;
using SalesSystem.Desktop.Controls.Payments;
using SalesSystem.Desktop.Controls.Reports;
using SalesSystem.Desktop.Controls.Settings;
using SalesSystem.Desktop.Controls.Users;
using SalesSystem.Desktop.Controls.Products;
using SalesSystem.Desktop.Controls.Customers;
using SalesSystem.Desktop.Controls.Suppliers;
using SalesSystem.Desktop.Controls.Warehouses;
using SalesSystem.Desktop.Controls.StockTransfers;

namespace SalesSystem.Desktop.Forms;

public partial class MainForm : Form
{
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IEventBus _eventBus;
    private IDisposable? _sessionExpiredSub;

    public MainForm(
        ISessionService sessionService,
        INavigationService navigationService,
        IEventBus eventBus)
    {
        _sessionService = sessionService;
        _navigationService = navigationService;
        _eventBus = eventBus;
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (!_sessionService.IsAuthenticated)
        {
            Application.Exit();
            return;
        }

        var session = _sessionService.Current!;
        lblUserName.Text = session.FullName;
        lblUserRole.Text = $"({GetRoleArabic(session.Role)})";

        BuildSidebar(session.Role);

        _navigationService.SetContentPanel(pnlContent);
        _navigationService.NavigateTo<DashboardControl>();

        _sessionExpiredSub = _eventBus.Subscribe<SessionExpiredMessage>(msg => {
            this.Invoke(() => HandleSessionExpired());
        });
    }

    private void BuildSidebar(UserRole role)
    {
        flpNav.Controls.Clear();

        var items = new List<NavigationItem>
        {
            new NavigationItem { Label = "لوحة التحكم", ScreenType = typeof(DashboardControl), MinRole = UserRole.Admin },
            new NavigationItem { Label = "المنتجات", ScreenType = typeof(ProductsListControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "العملاء", ScreenType = typeof(CustomersListControl), MinRole = UserRole.Cashier },
            new NavigationItem { Label = "الموردون", ScreenType = typeof(SuppliersListControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "المستودعات", ScreenType = typeof(WarehousesListControl), MinRole = UserRole.Admin },
            new NavigationItem { Label = "فواتير الشراء", ScreenType = typeof(PurchasesListControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "فواتير البيع", ScreenType = typeof(SalesListControl), MinRole = UserRole.Cashier },
            new NavigationItem { Label = "المرتجعـات", ScreenType = typeof(ReturnsListControl), MinRole = UserRole.Cashier },
            new NavigationItem { Label = "حركة المخزون", ScreenType = typeof(InventoryListControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "تحويل المخزون", ScreenType = typeof(StockTransfersListControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "المدفوعات", ScreenType = typeof(PaymentsListControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "التقارير", ScreenType = typeof(ReportsListControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "الإعدادات", ScreenType = typeof(SettingsListControl), MinRole = UserRole.Admin },
            new NavigationItem { Label = "المستخدمون", ScreenType = typeof(UsersListControl), MinRole = UserRole.Admin }
        };

        foreach (var item in items)
        {
            if (item.IsVisible(role))
            {
                var btn = CreateNavButton(item);
                flpNav.Controls.Add(btn);
            }
        }
    }

    private Button CreateNavButton(NavigationItem item)
    {
        var btn = new Button
        {
            Text = item.Label,
            Width = 200,
            Height = 45,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 0, 5),
            Cursor = Cursors.Hand,
            Tag = item
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 55, 65);
        
        btn.Click += (s, e) => {
            NavigateTo(item.ScreenType);
            HighlightButton(btn);
        };

        return btn;
    }

    private void NavigateTo(Type screenType)
    {
        var method = _navigationService.GetType().GetMethod("NavigateTo")!.MakeGenericMethod(screenType);
        method.Invoke(_navigationService, null);
    }

    private void HighlightButton(Button activeBtn)
    {
        foreach (Control ctrl in flpNav.Controls)
        {
            if (ctrl is Button btn)
            {
                btn.BackColor = Color.Transparent;
            }
        }
        activeBtn.BackColor = Color.FromArgb(33, 150, 243);
    }

    private void btnLogout_Click(object sender, EventArgs e)
    {
        _sessionService.SignOut();
        HandleSessionExpired();
    }

    private void HandleSessionExpired()
    {
        // Re-show login form
        Application.Restart();
    }

    private string GetRoleArabic(UserRole role) => role switch
    {
        UserRole.Admin => "مدير النظام",
        UserRole.Manager => "مدير فرع",
        UserRole.Cashier => "كاشير",
        _ => "غير معروف"
    };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sessionExpiredSub?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
