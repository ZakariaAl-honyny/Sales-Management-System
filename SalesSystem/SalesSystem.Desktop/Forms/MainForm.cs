using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Models;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Controls.Placeholders;
using SalesSystem.Desktop.Messages;

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
            new NavigationItem { Label = "المنتجات", ScreenType = typeof(ProductsControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "العملاء", ScreenType = typeof(CustomersControl), MinRole = UserRole.Cashier },
            new NavigationItem { Label = "الموردون", ScreenType = typeof(SuppliersControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "المستودعات", ScreenType = typeof(WarehousesControl), MinRole = UserRole.Admin },
            new NavigationItem { Label = "فواتير الشراء", ScreenType = typeof(PurchasesControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "فواتير البيع", ScreenType = typeof(SalesControl), MinRole = UserRole.Cashier },
            new NavigationItem { Label = "المرتجعـات", ScreenType = typeof(ReturnsControl), MinRole = UserRole.Cashier },
            new NavigationItem { Label = "حركة المخزون", ScreenType = typeof(InventoryControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "تحويل المخزون", ScreenType = typeof(StockTransfersControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "المدفوعات", ScreenType = typeof(PaymentsControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "التقارير", ScreenType = typeof(ReportsControl), MinRole = UserRole.Manager },
            new NavigationItem { Label = "الإعدادات", ScreenType = typeof(SettingsControl), MinRole = UserRole.Admin },
            new NavigationItem { Label = "المستخدمون", ScreenType = typeof(UsersControl), MinRole = UserRole.Admin }
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
