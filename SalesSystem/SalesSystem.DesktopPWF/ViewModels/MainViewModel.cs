using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Enums;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels.CashBoxes;
using SalesSystem.DesktopPWF.ViewModels.Categories;
using SalesSystem.DesktopPWF.ViewModels.Customers;
using SalesSystem.DesktopPWF.ViewModels.Inventory;
using SalesSystem.DesktopPWF.ViewModels.Payments;
using SalesSystem.DesktopPWF.ViewModels.Products;
using SalesSystem.DesktopPWF.ViewModels.Purchases;
using SalesSystem.DesktopPWF.ViewModels.Reports;
using SalesSystem.DesktopPWF.ViewModels.Returns;
using SalesSystem.DesktopPWF.ViewModels.Sales;
using SalesSystem.DesktopPWF.ViewModels.Suppliers;
using SalesSystem.DesktopPWF.ViewModels.Transfers;
using SalesSystem.DesktopPWF.ViewModels.Units;
using SalesSystem.DesktopPWF.ViewModels.Users;
using SalesSystem.DesktopPWF.ViewModels.Settings;
using SalesSystem.DesktopPWF.ViewModels.Taxes;
using SalesSystem.DesktopPWF.ViewModels.Accounting;
using SalesSystem.DesktopPWF.ViewModels.Accounts;
using SalesSystem.DesktopPWF.ViewModels.JournalEntries;
using SalesSystem.DesktopPWF.ViewModels.Currencies;
using SalesSystem.DesktopPWF.ViewModels.Audit;
using SalesSystem.DesktopPWF.ViewModels.Permissions;

using SalesSystem.DesktopPWF.ViewModels.Warehouses;
using SalesSystem.DesktopPWF.ViewModels.InventoryOperations;

namespace SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Main ViewModel for the application shell — manages sidebar navigation,
/// permission checks, and the currently active screen (CurrentViewModel).
/// Navigation is ViewModel-based using ContentControl binding.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private ViewModelBase? _currentViewModel;
    private readonly ISessionService _sessionService;
    private readonly IDialogService _dialogService;
    private readonly IUserApiService _userService;

    private CurrentUserDto? _currentUser;
    public CurrentUserDto? CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    public MainViewModel(ISessionService sessionService, IDialogService dialogService)
    {
        _sessionService = sessionService;
        _dialogService = dialogService;
        _userService = App.GetService<IUserApiService>();

        // Dashboard
        NavigateToDashboardCommand = new RelayCommand(() => NavigateTo<DashboardViewModel>());

        // Sales section
        NavigateToPosCommand = new RelayCommand(() => 
        {
            var screenService = App.GetService<IScreenWindowService>();
            var editorVm = App.GetService<SalesInvoiceEditorViewModel>();
            editorVm.CurrentViewMode = SalesInvoiceEditorViewModel.SalesViewMode.Touch;
            
            screenService.OpenScreen(editorVm, new ScreenWindowOptions
            {
                Title = "نقطة البيع (الكاشير)",
                OnClosed = (vm) =>
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (vm is SalesInvoiceEditorViewModel editor && editor.InvoiceId.HasValue)
                        {
                            var eventBus = App.GetService<IEventBus>();
                            eventBus.Publish(new SalesSystem.DesktopPWF.Messaging.Messages.SaleInvoiceChangedMessage(editor.InvoiceId.Value));
                        }
                    });
                }
            });
        });
        NavigateToSalesInvoicesCommand = new RelayCommand(() => NavigateTo<SalesInvoiceListViewModel>());
        NavigateToSalesQuotationsCommand = new RelayCommand(() => NavigateTo<SalesQuotationListViewModel>());
        NavigateToSalesReturnsCommand = new RelayCommand(() => NavigateTo<SalesReturnListViewModel>());

        // Purchases section
        NavigateToPurchasesCommand = new RelayCommand(() => NavigateTo<PurchaseInvoiceListViewModel>());
        NavigateToPurchaseOrdersCommand = new RelayCommand(() => NavigateTo<PurchaseOrderListViewModel>());
        NavigateToPurchaseReturnsCommand = new RelayCommand(() => NavigateTo<PurchaseReturnListViewModel>());

        // Finance section
        NavigateToCustomerPaymentsCommand = new RelayCommand(() => NavigateTo<CustomerPaymentsListViewModel>());
        NavigateToSupplierPaymentsCommand = new RelayCommand(() => NavigateTo<SupplierPaymentsListViewModel>());
        NavigateToCashBoxesCommand = new RelayCommand(() => NavigateTo<CashBoxesListViewModel>());

        // Reports section
        NavigateToReportsCommand = new RelayCommand(() => NavigateTo<ReportsViewModel>());
        NavigateToLowStockCommand = new RelayCommand(() => NavigateTo<LowStockViewModel>());
        NavigateToExpiredProductsCommand = new RelayCommand(() => NavigateTo<ExpiredProductsReportViewModel>());
        NavigateToStockBalanceReportCommand = new RelayCommand(() => NavigateTo<StockBalanceReportViewModel>());
        NavigateToWarehouseMovementReportCommand = new RelayCommand(() => NavigateTo<WarehouseMovementReportViewModel>());

        // Financial Reports
        NavigateToIncomeStatementCommand = new RelayCommand(() => NavigateTo<Reports.IncomeStatementViewModel>());
        NavigateToCashFlowReportCommand = new RelayCommand(() => NavigateTo<Reports.CashFlowReportViewModel>());
        NavigateToVatReportCommand = new RelayCommand(() => NavigateTo<Reports.VatReportViewModel>());
        NavigateToAccountStatementCommand = new RelayCommand(() => NavigateTo<Reports.AccountStatementViewModel>());

        // Settings section
        NavigateToProductsCommand = new RelayCommand(() => NavigateTo<ProductListViewModel>());
        NavigateToCustomersCommand = new RelayCommand(() => NavigateTo<CustomerListViewModel>());
        NavigateToCustomerGroupsCommand = new RelayCommand(() => NavigateTo<CustomerGroupListViewModel>());
        NavigateToSuppliersCommand = new RelayCommand(() => NavigateTo<SupplierListViewModel>());
        NavigateToWarehousesCommand = new RelayCommand(() => NavigateTo<WarehouseListViewModel>());
        NavigateToCategoriesCommand = new RelayCommand(() => NavigateTo<CategoryListViewModel>());
        NavigateToUnitsCommand = new RelayCommand(() => NavigateTo<UnitListViewModel>());
        NavigateToUsersCommand = new RelayCommand(() => NavigateTo<UserListViewModel>());
        NavigateToAuditLogCommand = new RelayCommand(() => NavigateTo<AuditLogListViewModel>());
        NavigateToPermissionsCommand = new RelayCommand(() => NavigateTo<PermissionManagementViewModel>());
        NavigateToSettingsCommand = new RelayCommand(() => NavigateTo<SettingsViewModel>());
        NavigateToSystemSettingsCommand = new RelayCommand(() => NavigateTo<SystemSettingsViewModel>());
        NavigateToBackupCommand = new RelayCommand(() => NavigateTo<BackupViewModel>());
        NavigateToStockTransfersCommand = new RelayCommand(() => NavigateTo<StockTransfersListViewModel>());
        NavigateToInventoryCommand = new RelayCommand(() => NavigateTo<InventoryViewModel>());
        NavigateToProductPricesCommand = new RelayCommand(() => NavigateTo<ProductPricesListViewModel>());
        NavigateToProductImagesCommand = new RelayCommand(() => NavigateTo<ProductImagesViewModel>());
        NavigateToBillOfMaterialsCommand = new RelayCommand(() => NavigateTo<BillOfMaterialsListViewModel>());
        NavigateToInventoryBatchesCommand = new RelayCommand(() => NavigateTo<InventoryBatchesViewModel>());
        NavigateToInventoryOperationsCommand = new RelayCommand(() => NavigateTo<InventoryOperationListViewModel>());
        NavigateToTaxesCommand = new RelayCommand(() => NavigateTo<TaxesListViewModel>());
        NavigateToCurrenciesCommand = new RelayCommand(() => NavigateTo<CurrenciesListViewModel>());
        NavigateToChartOfAccountsCommand = new RelayCommand(() => NavigateTo<AccountsListViewModel>());
        NavigateToJournalEntriesCommand = new RelayCommand(() => NavigateTo<JournalEntriesListViewModel>());
        NavigateToFiscalYearsCommand = new RelayCommand(() => NavigateTo<FiscalYearListViewModel>());

        ChangePasswordCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadChangePasswordAsync)));
    }

    private async Task LoadChangePasswordAsync()
    {
        var vm = new PasswordChangeViewModel(
            App.GetService<IAuthApiService>(),
            _dialogService,
            App.GetService<Services.App.Toast.IToastNotificationService>());
        var screenService = App.GetService<IScreenWindowService>();
        screenService.OpenScreen(vm, new ScreenWindowOptions
        {
            Title = "تغيير كلمة المرور",
            Width = 450,
            Height = 300,
            IsModal = true
        });
    }

    public async Task LoadCurrentUserAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _userService.GetCurrentUserAsync();
            if (result.IsSuccess && result.Value != null)
            {
                CurrentUser = result.Value;
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Currently active ViewModel displayed in the main content area.
    /// Setting this property triggers cleanup of the previous ViewModel
    /// and raises PropertyChanged for UI binding.
    /// </summary>
    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    // ═══════════════════════════════════════════════════════════════
    // Dashboard Command
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى لوحة المعلومات الرئيسية — تعرض ملخص سريع للمبيعات والأرباح والمخزون</summary>
    public ICommand NavigateToDashboardCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Sales Section Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى شاشة نقطة البيع (الكاشير)</summary>
    public ICommand NavigateToPosCommand { get; }

    /// <summary>نقل إلى فواتير البيع — عرض وإدارة جميع فواتير البيع (مسودات ومرحلة وملغية)</summary>
    public ICommand NavigateToSalesInvoicesCommand { get; }

    /// <summary>نقل إلى عروض الأسعار — عرض وإدارة عروض الأسعار وتحويلها إلى فواتير</summary>
    public ICommand NavigateToSalesQuotationsCommand { get; }

    /// <summary>نقل إلى مرتجعات المبيعات</summary>
    public ICommand NavigateToSalesReturnsCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Purchases Section Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى فواتير المشتريات</summary>
    public ICommand NavigateToPurchasesCommand { get; }

    /// <summary>نقل إلى أوامر الشراء — عرض وإدارة أوامر الشراء (قيد الانتظار والمستلمة والملغية)</summary>
    public ICommand NavigateToPurchaseOrdersCommand { get; }

    /// <summary>نقل إلى مرتجعات المشتريات</summary>
    public ICommand NavigateToPurchaseReturnsCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Finance Section Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى مدفوعات العملاء</summary>
    public ICommand NavigateToCustomerPaymentsCommand { get; }

    /// <summary>نقل إلى مدفوعات الموردين</summary>
    public ICommand NavigateToSupplierPaymentsCommand { get; }

    /// <summary>نقل إلى إدارة الخزينة (الصناديق)</summary>
    public ICommand NavigateToCashBoxesCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Reports Section Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى التقارير</summary>
    public ICommand NavigateToReportsCommand { get; }

    /// <summary>نقل إلى تقرير نواقص المخزون</summary>
    public ICommand NavigateToLowStockCommand { get; }

        /// <summary>نقل إلى تقرير المنتجات منتهية الصلاحية</summary>
        public ICommand NavigateToExpiredProductsCommand { get; }

        /// <summary>نقل إلى كشف رصيد المخازن — عرض المخزون والقيمة الإجمالية لكل منتج في المستودعات</summary>
        public ICommand NavigateToStockBalanceReportCommand { get; }

        /// <summary>نقل إلى تقرير حركة المخازن — عرض تاريخ حركات المخزون (إضافة/خصم) مع التصفية حسب المستودع والفترة</summary>
        public ICommand NavigateToWarehouseMovementReportCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Financial Reports Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى قائمة الدخل — عرض الإيرادات والتكاليف وصافي الربح</summary>
    public ICommand NavigateToIncomeStatementCommand { get; }

    /// <summary>نقل إلى تقرير التدفق النقدي — عرض الإيرادات والمصروفات والرصيد</summary>
    public ICommand NavigateToCashFlowReportCommand { get; }

    /// <summary>نقل إلى تقرير ضريبة القيمة المضافة — عرض الفواتير الخاضعة للضريبة</summary>
    public ICommand NavigateToVatReportCommand { get; }

    /// <summary>نقل إلى كشف حساب — عرض الحركات المدينة والدائنة للعميل أو المورد</summary>
    public ICommand NavigateToAccountStatementCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Settings Section Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى إدارة المنتجات</summary>
    public ICommand NavigateToProductsCommand { get; }

    /// <summary>نقل إلى إدارة العملاء</summary>
    public ICommand NavigateToCustomersCommand { get; }

    /// <summary>نقل إلى إدارة مجموعات العملاء — تصنيف وترتيب العملاء في مجموعات</summary>
    public ICommand NavigateToCustomerGroupsCommand { get; }

    /// <summary>نقل إلى إدارة الموردين</summary>
    public ICommand NavigateToSuppliersCommand { get; }

    /// <summary>نقل إلى إدارة المخازن</summary>
    public ICommand NavigateToWarehousesCommand { get; }

    /// <summary>نقل إلى إدارة التصنيفات</summary>
    public ICommand NavigateToCategoriesCommand { get; }

    /// <summary>نقل إلى إدارة الوحدات</summary>
    public ICommand NavigateToUnitsCommand { get; }

    /// <summary>نقل إلى إدارة المستخدمين</summary>
    public ICommand NavigateToUsersCommand { get; }

    /// <summary>نقل إلى سجل الأحداث — عرض جميع الحركات والإجراءات في النظام</summary>
    public ICommand NavigateToAuditLogCommand { get; }

    /// <summary>نقل إلى إدارة الصلاحيات — تعديل صلاحيات الأدوار</summary>
    public ICommand NavigateToPermissionsCommand { get; }

    /// <summary>تغيير كلمة المرور للمستخدم الحالي</summary>
    public ICommand ChangePasswordCommand { get; private set; } = null!;

    /// <summary>نقل إلى الإعدادات العامة</summary>
    public ICommand NavigateToSettingsCommand { get; }

    /// <summary>نقل إلى إعدادات النظام — إدارة جميع إعدادات النظام المخزنة في قاعدة البيانات</summary>
    public ICommand NavigateToSystemSettingsCommand { get; }

    /// <summary>نقل إلى إدارة النسخ الاحتياطي — إنشاء واستعادة النسخ الاحتياطية لقاعدة البيانات</summary>
    public ICommand NavigateToBackupCommand { get; }

    /// <summary>نقل إلى التحويلات المخزنية</summary>
    public ICommand NavigateToStockTransfersCommand { get; }

    /// <summary>نقل إلى شاشة المخزون</summary>
    public ICommand NavigateToInventoryCommand { get; }

    /// <summary>نقل إلى إدارة أسعار المنتجات — عرض وتحديث أسعار البيع متعددة العملات لكل وحدة منتج</summary>
    public ICommand NavigateToProductPricesCommand { get; }

    /// <summary>نقل إلى صور المنتجات — إدارة صور متعددة لكل منتج</summary>
    public ICommand NavigateToProductImagesCommand { get; }

    /// <summary>نقل إلى قائمة المكونات (BOM) — إدارة المكونات اللازمة لتصنيع المنتجات المُجمَّعة</summary>
    public ICommand NavigateToBillOfMaterialsCommand { get; }

    /// <summary>نقل إلى إدارة الدفعات المخزنية — تتبع الكميات حسب تاريخ انتهاء الصلاحية</summary>
    public ICommand NavigateToInventoryBatchesCommand { get; }

    /// <summary>نقل إلى العمليات المخزنية — الصرف والإستلام والتسوية</summary>
    public ICommand NavigateToInventoryOperationsCommand { get; }

    /// <summary>نقل إلى إدارة الضرائب</summary>
    public ICommand NavigateToTaxesCommand { get; }

    /// <summary>نقل إلى إدارة العملات — إضافة وتعديل العملات وأسعار الصرف</summary>
    public ICommand NavigateToCurrenciesCommand { get; }

    /// <summary>نقل إلى دليل الحسابات — عرض وتعديل الحسابات المحاسبية</summary>
    public ICommand NavigateToChartOfAccountsCommand { get; }

    /// <summary>نقل إلى القيود اليومية — عرض جميع قيود اليومية المحاسبية</summary>
    public ICommand NavigateToJournalEntriesCommand { get; }

    /// <summary>نقل إلى السنوات المالية — إنشاء وفتح وإغلاق السنوات المالية</summary>
    public ICommand NavigateToFiscalYearsCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Navigation Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Navigates to the ViewModel of type <typeparamref name="T"/>.
    /// Resolves the ViewModel via DI (<see cref="App.GetService{T}"/>),
    /// cleans up the current ViewModel, and sets it as <see cref="CurrentViewModel"/>.
    /// </summary>
    /// <typeparam name="T">The ViewModel type to navigate to. Must extend <see cref="ViewModelBase"/>.</typeparam>
    public void NavigateTo<T>() where T : ViewModelBase
    {
        // Check permission before navigating
        var tag = GetTagForViewModel<T>();
        if (!CanNavigate(tag))
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "ليس لديك صلاحية للوصول إلى هذه الشاشة.");
            return;
        }

        // Clean up current ViewModel (unsubscribe from events, dispose resources)
        CurrentViewModel?.Cleanup();

        // Resolve the new ViewModel from DI and set as current
        CurrentViewModel = App.GetService<T>();
    }

    /// <summary>
    /// Navigates to a ViewModel specified by its <see cref="Type"/>.
    /// Uses reflection to resolve via <see cref="App.GetService{T}"/>.
    /// Falls back silently if the type is not a ViewModelBase.
    /// </summary>
    public void NavigateToViewModel(Type viewModelType)
    {
        if (!typeof(ViewModelBase).IsAssignableFrom(viewModelType))
            return;

        // Check permission before navigating
        var tag = GetTagForViewModel(viewModelType);
        if (!CanNavigate(tag))
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "ليس لديك صلاحية للوصول إلى هذه الشاشة.");
            return;
        }

        // Clean up current ViewModel
        CurrentViewModel?.Cleanup();

        // Resolve via reflection: App.GetService<T>()
        var method = typeof(App).GetMethod(
            nameof(App.GetService),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
        var genericMethod = method.MakeGenericMethod(viewModelType);
        CurrentViewModel = (ViewModelBase)genericMethod.Invoke(null, null)!;
    }

    // ═══════════════════════════════════════════════════════════════
    // Permission Checking
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks whether the current user has permission to navigate to the screen
    /// identified by the given <paramref name="tag"/>.
    /// Mirrors the permission logic from <c>MainWindow.xaml.cs</c>.
    /// </summary>
    private bool CanNavigate(string tag)
    {
        return tag switch
        {
            "Purchases"        => _sessionService.CanAccess(Permission.PurchaseInvoice),
            "PurchaseOrders"   => _sessionService.CanAccess(Permission.PurchaseInvoice),
            "PurchaseReturns"  => _sessionService.CanAccess(Permission.PurchaseReturn),
            "Products"         => _sessionService.CanAccess(Permission.ProductManagement),
            "Suppliers"        => _sessionService.CanAccess(Permission.SupplierManagement),
            "SupplierPayments" => _sessionService.CanAccess(Permission.SupplierManagement),
            "StockTransfers"   => _sessionService.CanAccess(Permission.StockTransfer),
            "Reports"          => _sessionService.CanAccess(Permission.Reports),
            "ExpiredProducts"  => _sessionService.CanAccess(Permission.Reports),
            "LowStock"         => _sessionService.CanAccess(Permission.Reports),
            "Warehouses"       => _sessionService.CanAccess(Permission.WarehouseManagement),
            "Users"            => _sessionService.CanAccess(Permission.UserManagement),
            "Settings"         => _sessionService.CanAccess(Permission.Settings),
            "Categories"       => _sessionService.CanAccess(Permission.ProductManagement),
            "Units"            => _sessionService.CanAccess(Permission.ProductManagement),
            "Taxes"            => _sessionService.CanAccess(Permission.Settings),
            "Currencies"       => _sessionService.CanAccess(Permission.Settings),
            _ => true // Dashboard, Sales, SalesReturns, Customers, CustomerPayments, POS, CashBoxes, Inventory
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // ViewModel-to-Tag Mapping
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps a ViewModel type to a permission tag string.
    /// Delegates to the Type-based overload to avoid duplication.
    /// Used by <see cref="CanNavigate"/> to check access rights.
    /// </summary>
    private static string GetTagForViewModel<T>() where T : ViewModelBase
        => GetTagForViewModel(typeof(T));

    /// <summary>
    /// Non-generic overload of <see cref="GetTagForViewModel{T}"/> for use
    /// with the Type-based navigation method.
    /// </summary>
    private static string GetTagForViewModel(Type viewModelType)
    {
        return viewModelType.Name switch
        {
            nameof(TouchPosViewModel)              => "Pos",
            nameof(SalesInvoiceListViewModel)       => "Sales",
            nameof(SalesReturnListViewModel)        => "SalesReturns",
            nameof(PurchaseInvoiceListViewModel)    => "Purchases",
            nameof(PurchaseOrderListViewModel)      => "PurchaseOrders",
            nameof(PurchaseReturnListViewModel)     => "PurchaseReturns",
            nameof(CustomerPaymentsListViewModel)   => "CustomerPayments",
            nameof(SupplierPaymentsListViewModel)   => "SupplierPayments",
            nameof(CashBoxesListViewModel)          => "CashBoxes",
            nameof(ReportsViewModel)                => "Reports",
            nameof(LowStockViewModel)               => "LowStock",
            nameof(ExpiredProductsReportViewModel)  => "ExpiredProducts",
            nameof(StockBalanceReportViewModel)     => "Reports",
            nameof(WarehouseMovementReportViewModel) => "Reports",
            nameof(IncomeStatementViewModel)         => "Reports",
            nameof(CashFlowReportViewModel)           => "Reports",
            nameof(VatReportViewModel)                => "Reports",
            nameof(AccountStatementViewModel)         => "Reports",
            nameof(ProductListViewModel)            => "Products",
            nameof(CustomerListViewModel)           => "Customers",
            nameof(SupplierListViewModel)           => "Suppliers",
            nameof(WarehouseListViewModel)          => "Warehouses",
            nameof(CategoryListViewModel)           => "Categories",
            nameof(UnitListViewModel)               => "Units",
            nameof(UserListViewModel)               => "Users",
            nameof(AuditLogListViewModel)           => "Settings",
            nameof(PermissionManagementViewModel)   => "Settings",
            nameof(SettingsViewModel)               => "Settings",
            nameof(SystemSettingsViewModel)         => "Settings",
            nameof(BackupViewModel)                 => "Settings",
            nameof(StockTransfersListViewModel)     => "StockTransfers",
            nameof(InventoryViewModel)              => "Inventory",
            nameof(ProductPricesListViewModel)      => "Products",
            nameof(ProductImagesViewModel)          => "Products",
            nameof(BillOfMaterialsListViewModel)   => "Products",
            nameof(InventoryBatchesViewModel)       => "Products",
            nameof(TaxesListViewModel)              => "Taxes",
            nameof(CurrenciesListViewModel)         => "Currencies",
            nameof(AccountsListViewModel)           => "Settings",
            nameof(JournalEntriesListViewModel)     => "Settings",
            nameof(FiscalYearListViewModel)         => "Settings",
            _                                        => viewModelType.Name
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Cleanup
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Cleans up the current ViewModel and resets state.
    /// Called when the MainWindow is closing or a new navigation occurs.
    /// </summary>
    public override void Cleanup()
    {
        CurrentViewModel?.Cleanup();
        CurrentViewModel = null;
        base.Cleanup();
    }
}
