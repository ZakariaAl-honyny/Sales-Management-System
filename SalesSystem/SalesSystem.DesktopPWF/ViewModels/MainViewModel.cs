using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Enums;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels.CashBoxes;
using SalesSystem.DesktopPWF.ViewModels.Customers;
using SalesSystem.DesktopPWF.ViewModels.Inventory;
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
using SalesSystem.DesktopPWF.ViewModels.Logs;
using SalesSystem.DesktopPWF.ViewModels.Permissions;
using SalesSystem.DesktopPWF.ViewModels.Warehouses;
using SalesSystem.DesktopPWF.ViewModels.Branch;
using SalesSystem.DesktopPWF.ViewModels.Department;
using SalesSystem.DesktopPWF.ViewModels.Employee;
using SalesSystem.DesktopPWF.ViewModels.Bank;
using SalesSystem.DesktopPWF.ViewModels.Payments;
using SalesSystem.DesktopPWF.ViewModels.Party;
using SalesSystem.DesktopPWF.ViewModels.Expense;
using SalesSystem.DesktopPWF.ViewModels.CustomerReceipt;
using SalesSystem.DesktopPWF.ViewModels.PaymentVouchers;
using SalesSystem.DesktopPWF.ViewModels.InventoryCount;
using SalesSystem.DesktopPWF.ViewModels.InventoryAdjustment;
using SalesSystem.DesktopPWF.ViewModels.Notifications;
using SalesSystem.DesktopPWF.ViewModels.Attachments;
using SalesSystem.DesktopPWF.ViewModels.Roles;
using SalesSystem.DesktopPWF.ViewModels.Sessions;
using SalesSystem.DesktopPWF.ViewModels.CompanySettings;
using SalesSystem.DesktopPWF.ViewModels.DocumentSequences;
using SalesSystem.DesktopPWF.ViewModels.AccountCategories;
using SalesSystem.DesktopPWF.ViewModels.SystemAccountMappings;

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

    private ObservableCollection<BranchDto> _userBranches = new();
    public ObservableCollection<BranchDto> UserBranches
    {
        get => _userBranches;
        set => SetProperty(ref _userBranches, value);
    }

    private BranchDto? _selectedUserBranch;
    public BranchDto? SelectedUserBranch
    {
        get => _selectedUserBranch;
        set => SetProperty(ref _selectedUserBranch, value);
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
        NavigateToSalesReturnsCommand = new RelayCommand(() => NavigateTo<SalesReturnListViewModel>());

        // Purchases section
        NavigateToPurchasesCommand = new RelayCommand(() => NavigateTo<PurchaseInvoiceListViewModel>());
        NavigateToPurchaseOrdersCommand = new RelayCommand(async () =>
            await _dialogService.ShowInfoAsync("تحت التطوير", "هذه الميزة قيد التطوير وسيتم إضافتها في الإصدارات القادمة"));
        NavigateToPurchaseReturnsCommand = new RelayCommand(() => NavigateTo<PurchaseReturnListViewModel>());

        // Finance section
        NavigateToSupplierPaymentsCommand = new RelayCommand(() => NavigateTo<SupplierPaymentsListViewModel>());
        NavigateToCashBoxesCommand = new RelayCommand(() => NavigateTo<CashBoxesListViewModel>());

        // Reports section
        NavigateToReportsCommand = new RelayCommand(() => NavigateTo<ReportsViewModel>());
        NavigateToLowStockCommand = new RelayCommand(() => NavigateTo<LowStockViewModel>());
        NavigateToExpiredProductsCommand = new RelayCommand(() => NavigateTo<ExpiredProductsReportViewModel>());
        NavigateToStockBalanceReportCommand = new RelayCommand(() => NavigateTo<StockBalanceReportViewModel>());
        NavigateToWarehouseMovementReportCommand = new RelayCommand(() => NavigateTo<WarehouseMovementReportViewModel>());

        // Phase 31v2 — New Report Commands
        NavigateToDetailedStockLedgerCommand = new RelayCommand(() => NavigateTo<DetailedStockLedgerViewModel>());
        NavigateToProductProfitabilityCommand = new RelayCommand(() => NavigateTo<ProductProfitabilityViewModel>());
        NavigateToProfitByCustomerCommand = new RelayCommand(() => NavigateTo<ProfitByCustomerViewModel>());
        NavigateToReturnsReportCommand = new RelayCommand(() => NavigateTo<ReturnsReportViewModel>());
        NavigateToAgingReportCommand = new RelayCommand(() => NavigateTo<AgingReportViewModel>());
        NavigateToWorkingCapitalCommand = new RelayCommand(() => NavigateTo<WorkingCapitalViewModel>());
        NavigateToAccountBalancesCommand = new RelayCommand(() => NavigateTo<AccountBalancesViewModel>());

        // Financial Reports
        NavigateToIncomeStatementCommand = new RelayCommand(() => NavigateTo<Reports.IncomeStatementViewModel>());
        NavigateToCashFlowReportCommand = new RelayCommand(() => NavigateTo<Reports.CashFlowReportViewModel>());
        NavigateToVatReportCommand = new RelayCommand(() => NavigateTo<Reports.VatReportViewModel>());
        NavigateToAccountStatementCommand = new RelayCommand(() => NavigateTo<Reports.AccountStatementViewModel>());

        // Phase 31v2 — More Report Commands (13 missing reports)
        NavigateToBalanceSheetCommand = new RelayCommand(() => NavigateTo<BalanceSheetViewModel>());
        NavigateToTrialBalanceCommand = new RelayCommand(() => NavigateTo<TrialBalanceViewModel>());
        NavigateToGeneralLedgerCommand = new RelayCommand(() => NavigateTo<GeneralLedgerViewModel>());
        NavigateToCashBoxSummaryCommand = new RelayCommand(() => NavigateTo<CashBoxSummaryViewModel>());
        NavigateToDailySalesCommand = new RelayCommand(() => NavigateTo<DailySalesViewModel>());
        NavigateToSalesByCategoryCommand = new RelayCommand(() => NavigateTo<SalesByCategoryViewModel>());
        NavigateToSalesByProductCommand = new RelayCommand(() => NavigateTo<SalesByProductViewModel>());
        NavigateToSalesByCustomerCommand = new RelayCommand(() => NavigateTo<SalesByCustomerViewModel>());
        NavigateToPurchasesByProductCommand = new RelayCommand(() => NavigateTo<PurchasesByProductViewModel>());
        NavigateToPurchasesBySupplierCommand = new RelayCommand(() => NavigateTo<PurchasesBySupplierViewModel>());
        NavigateToLoginHistoryCommand = new RelayCommand(() => NavigateTo<LoginHistoryViewModel>());
        NavigateToUserActivityCommand = new RelayCommand(() => NavigateTo<UserActivityViewModel>());

        // Settings section
        NavigateToProductsCommand = new RelayCommand(() => NavigateTo<ProductListViewModel>());
        NavigateToCustomersCommand = new RelayCommand(() => NavigateTo<CustomerListViewModel>());
        NavigateToSuppliersCommand = new RelayCommand(() => NavigateTo<SupplierListViewModel>());
        NavigateToWarehousesCommand = new RelayCommand(() => NavigateTo<WarehouseListViewModel>());
        NavigateToUnitsCommand = new RelayCommand(() => NavigateTo<UnitListViewModel>());
        NavigateToUsersCommand = new RelayCommand(() => NavigateTo<UserListViewModel>());
        NavigateToAuditLogCommand = new RelayCommand(() => NavigateTo<AuditLogListViewModel>());
        NavigateToPermissionsCommand = new RelayCommand(() => NavigateTo<PermissionManagementViewModel>());
        NavigateToRolesCommand = new RelayCommand(() => NavigateTo<RoleListViewModel>());
        NavigateToSessionsCommand = new RelayCommand(() => NavigateTo<UserSessionListViewModel>());
        NavigateToSettingsCommand = new RelayCommand(() => NavigateTo<SettingsViewModel>());
        NavigateToSystemSettingsCommand = new RelayCommand(() => NavigateTo<SystemSettingsViewModel>());
        NavigateToBackupCommand = new RelayCommand(() => NavigateTo<BackupViewModel>());
        NavigateToSystemLogsCommand = new RelayCommand(() => NavigateTo<SystemLogListViewModel>());
        NavigateToWarehouseTransfersCommand = new RelayCommand(() => NavigateTo<WarehouseTransfersListViewModel>());
        NavigateToInventoryCommand = new RelayCommand(() => NavigateTo<InventoryViewModel>());
        NavigateToInventoryTransactionsCommand = new RelayCommand(() => NavigateTo<InventoryTransactionListViewModel>());
        NavigateToInventoryIssueCommand = new AsyncRelayCommand(() => OpenInventoryTransactionEditor(11));
        NavigateToInventoryReceiptCommand = new AsyncRelayCommand(() => OpenInventoryTransactionEditor(12));
        NavigateToInventoryDamageCommand = new AsyncRelayCommand(() => OpenInventoryTransactionEditor(9));
        NavigateToProductPricesCommand = new RelayCommand(() => NavigateTo<ProductPricesListViewModel>());
        NavigateToInventoryBatchesCommand = new RelayCommand(() => NavigateTo<InventoryBatchesViewModel>());
        NavigateToTaxesCommand = new RelayCommand(() => NavigateTo<TaxesListViewModel>());
        NavigateToCurrenciesCommand = new RelayCommand(() => NavigateTo<CurrenciesListViewModel>());
        NavigateToCurrencyRatesCommand = new RelayCommand(() => NavigateTo<CurrencyRatesViewModel>());
        NavigateToChartOfAccountsCommand = new RelayCommand(() => NavigateTo<AccountsListViewModel>());
        NavigateToJournalEntriesCommand = new RelayCommand(() => NavigateTo<JournalEntriesListViewModel>());
        NavigateToFiscalYearsCommand = new RelayCommand(() => NavigateTo<FiscalYearListViewModel>());
        NavigateToCompanySettingsCommand = new RelayCommand(() => NavigateTo<CompanySettingsViewModel>());
        NavigateToDocumentSequencesCommand = new RelayCommand(() => NavigateTo<DocumentSequenceListViewModel>());
        NavigateToAccountCategoriesCommand = new RelayCommand(() => NavigateTo<AccountCategoryListViewModel>());
        NavigateToSystemAccountMappingsCommand = new RelayCommand(() => NavigateTo<SystemAccountMappingListViewModel>());

        // Organization Management section
        NavigateToBranchesCommand = new RelayCommand(() => NavigateTo<BranchListViewModel>());
        NavigateToDepartmentsCommand = new RelayCommand(() => NavigateTo<DepartmentListViewModel>());
        NavigateToEmployeesCommand = new RelayCommand(() => NavigateTo<EmployeeListViewModel>());
        NavigateToBanksCommand = new RelayCommand(() => NavigateTo<BankListViewModel>());
        NavigateToPartiesCommand = new RelayCommand(() => NavigateTo<PartyListViewModel>());
        NavigateToExpensesCommand = new RelayCommand(() => NavigateTo<ExpenseListViewModel>());

        // Customer Receipts
        NavigateToCustomerReceiptsCommand = new RelayCommand(() => NavigateTo<CustomerReceiptListViewModel>());

        // Receipt Vouchers (Accounting)
        NavigateToReceiptVouchersCommand = new RelayCommand(() => NavigateTo<ReceiptVoucherListViewModel>());

        // Payment Vouchers
        NavigateToPaymentVouchersCommand = new RelayCommand(() => NavigateTo<PaymentVoucherListViewModel>());

        // Inventory Operations
        NavigateToInventoryCountsCommand = new RelayCommand(() => NavigateTo<InventoryCountListViewModel>());
        NavigateToInventoryAdjustmentsCommand = new RelayCommand(() => NavigateTo<InventoryAdjustmentListViewModel>());

        // Notifications & Attachments
        NavigateToNotificationsCommand = new RelayCommand(() => NavigateTo<NotificationListViewModel>());
        NavigateToAttachmentsCommand = new RelayCommand(() => NavigateTo<AttachmentListViewModel>());

        // Products section commands
        NavigateToProductUnitsCommand = new RelayCommand(() => NavigateTo<ProductUnitsListViewModel>());
        NavigateToProductImportCommand = new RelayCommand(() => NavigateTo<ProductImportViewModel>());

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
        _ = LoadUserBranchesAsync();
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
    // View Mode Properties
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true when the user has Advanced mode (Admin/Manager roles).
    /// Advanced mode shows accounting screens (دليل الحسابات, قيود يومية, سنوات مالية, تقارير مالية).
    /// Determined from the user's role at login time via <see cref="ISessionService.GetViewMode"/>.
    /// </summary>
    public bool IsAdvancedMode => _sessionService.GetViewMode() == Enums.ViewMode.Advanced;

    /// <summary>
    /// Returns true when the user has Basic mode (Cashier/Observer/other roles).
    /// Basic mode shows only operational screens (مبيعات, مشتريات, أصناف, عملاء, موردون, مخزون).
    /// This is the inverse of <see cref="IsAdvancedMode"/>.
    /// </summary>
    public bool IsBasicMode => !IsAdvancedMode;

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

    /// <summary>نقل إلى مرتجعات المبيعات</summary>
    public ICommand NavigateToSalesReturnsCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Purchases Section Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى فواتير المشتريات</summary>
    public ICommand NavigateToPurchasesCommand { get; }

    /// <summary>نقل إلى أوامر الشراء — عرض وإدارة أوامر التوريد للموردين</summary>
    public ICommand NavigateToPurchaseOrdersCommand { get; }

    /// <summary>نقل إلى مرتجعات المشتريات</summary>
    public ICommand NavigateToPurchaseReturnsCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Finance Section Commands
    // ═══════════════════════════════════════════════════════════════

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

        /// <summary>نقل إلى كشف حساب مفصل للمخزون — عرض حركات المخزون التفصيلية مع أرصدة الفترات</summary>
        public ICommand NavigateToDetailedStockLedgerCommand { get; }

        /// <summary>نقل إلى تقرير ربحية المنتج — عرض هامش الربح لكل منتج مع إجمالي المبيعات والتكلفة</summary>
        public ICommand NavigateToProductProfitabilityCommand { get; }

        /// <summary>نقل إلى تقرير الربح حسب العميل — عرض إجمالي المبيعات والتكلفة والربح لكل عميل</summary>
        public ICommand NavigateToProfitByCustomerCommand { get; }

        /// <summary>نقل إلى تقرير المرتجعات — عرض جميع مرتجعات المبيعات والمشتريات مع التفاصيل</summary>
        public ICommand NavigateToReturnsReportCommand { get; }

        /// <summary>نقل إلى تقرير التقادم (Aging) — عرض توزيع الديون حسب الفترات الزمنية للعملاء والموردين</summary>
        public ICommand NavigateToAgingReportCommand { get; }

        /// <summary>نقل إلى تقرير رأس المال العامل — عرض الأصول المتداولة والخصوم المتداولة والنسبة الحالية</summary>
        public ICommand NavigateToWorkingCapitalCommand { get; }

        /// <summary>نقل إلى تقرير أرصدة الحسابات — عرض الأرصدة المدينة والدائنة وصافي الرصيد لكل حساب</summary>
        public ICommand NavigateToAccountBalancesCommand { get; }

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
    // Phase 31v2 — More Report Commands (13 missing reports)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى الميزانية العمومية — عرض الأصول والخصوم وحقوق الملكية في تاريخ محدد</summary>
    public ICommand NavigateToBalanceSheetCommand { get; }

    /// <summary>نقل إلى ميزان المراجعة — عرض أرصدة جميع الحسابات المدينة والدائنة</summary>
    public ICommand NavigateToTrialBalanceCommand { get; }

    /// <summary>نقل إلى دفتر الأستاذ العام — عرض حركات الحسابات التفصيلية مع الأرصدة</summary>
    public ICommand NavigateToGeneralLedgerCommand { get; }

    /// <summary>نقل إلى ملخص الخزينة — عرض حركات الصندوق والإيرادات والمصروفات والأرصدة</summary>
    public ICommand NavigateToCashBoxSummaryCommand { get; }

    /// <summary>نقل إلى تقرير المبيعات اليومية — عرض إجمالي المبيعات والإيرادات اليومية</summary>
    public ICommand NavigateToDailySalesCommand { get; }

    /// <summary>نقل إلى تقرير المبيعات حسب التصنيف — عرض المبيعات مصنفة حسب تصنيف المنتجات</summary>
    public ICommand NavigateToSalesByCategoryCommand { get; }

    /// <summary>نقل إلى تقرير المبيعات حسب المنتج — عرض كمية وقيمة المبيعات لكل منتج</summary>
    public ICommand NavigateToSalesByProductCommand { get; }

    /// <summary>نقل إلى تقرير المبيعات حسب العميل — عرض إجمالي المبيعات لكل عميل</summary>
    public ICommand NavigateToSalesByCustomerCommand { get; }

    /// <summary>نقل إلى تقرير المشتريات حسب المنتج — عرض كمية وقيمة المشتريات لكل منتج</summary>
    public ICommand NavigateToPurchasesByProductCommand { get; }

    /// <summary>نقل إلى تقرير المشتريات حسب المورد — عرض إجمالي المشتريات لكل مورد</summary>
    public ICommand NavigateToPurchasesBySupplierCommand { get; }

    /// <summary>نقل إلى سجل الدخول — عرض محاولات تسجيل الدخول للمستخدمين والنتائج</summary>
    public ICommand NavigateToLoginHistoryCommand { get; }

    /// <summary>نقل إلى نشاط المستخدمين — عرض سجل الإجراءات والعمليات لكل مستخدم</summary>
    public ICommand NavigateToUserActivityCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Settings Section Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى إدارة المنتجات</summary>
    public ICommand NavigateToProductsCommand { get; }

    /// <summary>نقل إلى إدارة العملاء</summary>
    public ICommand NavigateToCustomersCommand { get; }

    /// <summary>نقل إلى إدارة الموردين</summary>
    public ICommand NavigateToSuppliersCommand { get; }

    /// <summary>نقل إلى إدارة المخازن</summary>
    public ICommand NavigateToWarehousesCommand { get; }

    /// <summary>نقل إلى إدارة الوحدات</summary>
    public ICommand NavigateToUnitsCommand { get; }

    /// <summary>نقل إلى إدارة المستخدمين</summary>
    public ICommand NavigateToUsersCommand { get; }

    /// <summary>نقل إلى سجل الأحداث — عرض جميع الحركات والإجراءات في النظام</summary>
    public ICommand NavigateToAuditLogCommand { get; }

    /// <summary>نقل إلى إدارة الصلاحيات — تعديل صلاحيات الأدوار</summary>
    public ICommand NavigateToPermissionsCommand { get; }

    /// <summary>نقل إلى إدارة الأدوار — إضافة وتعديل وحذف أدوار المستخدمين</summary>
    public ICommand NavigateToRolesCommand { get; }

    /// <summary>نقل إلى جلسات المستخدمين — عرض وإلغاء الجلسات النشطة</summary>
    public ICommand NavigateToSessionsCommand { get; }

    /// <summary>تغيير كلمة المرور للمستخدم الحالي</summary>
    public ICommand ChangePasswordCommand { get; private set; } = null!;

    /// <summary>نقل إلى الإعدادات العامة</summary>
    public ICommand NavigateToSettingsCommand { get; }

    /// <summary>نقل إلى إعدادات النظام — إدارة جميع إعدادات النظام المخزنة في قاعدة البيانات</summary>
    public ICommand NavigateToSystemSettingsCommand { get; }

    /// <summary>نقل إلى إدارة النسخ الاحتياطي — إنشاء واستعادة النسخ الاحتياطية لقاعدة البيانات</summary>
    public ICommand NavigateToBackupCommand { get; }

    /// <summary>نقل إلى سجل أحداث النظام — عرض أخطاء النظام وسجلات التشغيل والتحذيرات</summary>
    public ICommand NavigateToSystemLogsCommand { get; }

    /// <summary>نقل إلى التحويلات المخزنية</summary>
    public ICommand NavigateToWarehouseTransfersCommand { get; }

    /// <summary>نقل إلى شاشة المخزون</summary>
    public ICommand NavigateToInventoryCommand { get; }

    /// <summary>نقل إلى حركات المخزون — عرض تاريخ حركات المخزون والتفاصيل</summary>
    public ICommand NavigateToInventoryTransactionsCommand { get; }

    /// <summary>نقل إلى شاشة صرف مخزني — صرف أصناف من المخزون بدون فاتورة بيع</summary>
    public ICommand NavigateToInventoryIssueCommand { get; }

    /// <summary>نقل إلى شاشة توريد مخزني — إضافة أصناف إلى المخزون بدون فاتورة شراء</summary>
    public ICommand NavigateToInventoryReceiptCommand { get; }

    /// <summary>نقل إلى شاشة تالف وهالك — تسجيل الأصناف التالفة والمنتهية الصلاحية</summary>
    public ICommand NavigateToInventoryDamageCommand { get; }

    /// <summary>نقل إلى إدارة أسعار المنتجات — عرض وتحديث أسعار البيع متعددة العملات لكل وحدة منتج</summary>
    public ICommand NavigateToProductPricesCommand { get; }

    /// <summary>نقل إلى إدارة الدفعات المخزنية — تتبع الكميات حسب تاريخ انتهاء الصلاحية</summary>
    public ICommand NavigateToInventoryBatchesCommand { get; }

    /// <summary>نقل إلى إدارة الضرائب</summary>
    public ICommand NavigateToTaxesCommand { get; }

    /// <summary>نقل إلى إدارة العملات — إضافة وتعديل العملات وأسعار الصرف</summary>
    public ICommand NavigateToCurrenciesCommand { get; }

    /// <summary>نقل إلى إدارة أسعار العملات — عرض وتحديث أسعار صرف العملات</summary>
    public ICommand NavigateToCurrencyRatesCommand { get; }

    /// <summary>نقل إلى دليل الحسابات — عرض وتعديل الحسابات المحاسبية</summary>
    public ICommand NavigateToChartOfAccountsCommand { get; }

    /// <summary>نقل إلى القيود اليومية — عرض جميع قيود اليومية المحاسبية</summary>
    public ICommand NavigateToJournalEntriesCommand { get; }

    /// <summary>نقل إلى السنوات المالية — إنشاء وفتح وإغلاق السنوات المالية</summary>
    public ICommand NavigateToFiscalYearsCommand { get; }
    /// <summary>نقل إلى إعدادات الشركة — تعديل اسم الشركة وبيانات الاتصال والعملة الافتراضية</summary>
    public ICommand NavigateToCompanySettingsCommand { get; }
    /// <summary>نقل إلى تسلسل المستندات — عرض وإعادة تعيين أرقام المستندات التلقائية</summary>
    public ICommand NavigateToDocumentSequencesCommand { get; }
    /// <summary>نقل إلى التصنيفات المحاسبية — إدارة تصنيفات دليل الحسابات</summary>
    public ICommand NavigateToAccountCategoriesCommand { get; }
    /// <summary>نقل إلى حسابات النظام — ربط العمليات التجارية بالحسابات المحاسبية</summary>
    public ICommand NavigateToSystemAccountMappingsCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Organization Management Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى إدارة الفروع — عرض وإضافة وتعديل الفروع</summary>
    public ICommand NavigateToBranchesCommand { get; }

    /// <summary>نقل إلى إدارة الأقسام — عرض وإضافة وتعديل الأقسام</summary>
    public ICommand NavigateToDepartmentsCommand { get; }

    /// <summary>نقل إلى إدارة الموظفين — عرض وإضافة وتعديل الموظفين</summary>
    public ICommand NavigateToEmployeesCommand { get; }

    /// <summary>نقل إلى إدارة البنوك — عرض وإضافة وتعديل بيانات البنوك</summary>
    public ICommand NavigateToBanksCommand { get; }

    /// <summary>نقل إلى إدارة الجهات — عرض وإضافة وتعديل الجهات الخارجية</summary>
    public ICommand NavigateToPartiesCommand { get; }

    /// <summary>نقل إلى إدارة المصروفات — عرض وإضافة وتعديل المصروفات</summary>
    public ICommand NavigateToExpensesCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Customer Receipts Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى سندات القبض — إدارة سندات القبض النقدية والبنكية</summary>
    public ICommand NavigateToCustomerReceiptsCommand { get; }

    /// <summary>نقل إلى سندات القبض المحاسبية — عرض وإدارة سندات القبض (سندات الصندوق)</summary>
    public ICommand NavigateToReceiptVouchersCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Payment Vouchers Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى سندات الصرف — إدارة سندات الصرف النقدية والبنكية</summary>
    public ICommand NavigateToPaymentVouchersCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Inventory Operations Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى الجرد المخزني — إدارة وإجراء جرد المخزون الدوري</summary>
    public ICommand NavigateToInventoryCountsCommand { get; }

    /// <summary>نقل إلى تسويات المخزون — إضافة وتعديل تسويات المخزون</summary>
    public ICommand NavigateToInventoryAdjustmentsCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Notifications & Attachments Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى الإشعارات — عرض إشعارات النظام والتنبيهات</summary>
    public ICommand NavigateToNotificationsCommand { get; }

    /// <summary>نقل إلى المرفقات — عرض وإدارة الملفات المرفقة</summary>
    public ICommand NavigateToAttachmentsCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Products Section Commands
    // ═══════════════════════════════════════════════════════════════

    /// <summary>نقل إلى إدارة وحدات الصنف — عرض وتعديل وحدات القياس لكل صنف</summary>
    public ICommand NavigateToProductUnitsCommand { get; }

    /// <summary>نقل إلى شاشة استيراد الأصناف من Excel</summary>
    public ICommand NavigateToProductImportCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════

    private async Task OpenInventoryTransactionEditor(byte transactionType)
    {
        var editorVm = new InventoryTransactionEditorViewModel();
        editorVm.SetTransactionType(transactionType);
        var screenService = App.GetService<IScreenWindowService>();
        screenService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = editorVm.Title,
            OnClosed = (vm) =>
            {
                if (vm is InventoryTransactionEditorViewModel editor && editor.TransactionId.HasValue)
                {
                    var eventBus = App.GetService<IEventBus>();
                    eventBus.Publish(new InventoryTransactionChangedMessage(editor.TransactionId.Value));
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => NavigateTo<InventoryTransactionListViewModel>());
                }
            }
        });
    }

    public async Task LoadUserBranchesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var branchService = App.GetService<IBranchApiService>();
            var result = await branchService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                UserBranches = new ObservableCollection<BranchDto>(result.Value);
                if (result.Value.Count > 0)
                    SelectedUserBranch = result.Value[0];
            }
        });
    }

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
            "WarehouseTransfers"   => _sessionService.CanAccess(Permission.WarehouseTransfer),
            "Reports"          => _sessionService.CanAccess(Permission.Reports),
            "ExpiredProducts"  => _sessionService.CanAccess(Permission.Reports),
            "LowStock"         => _sessionService.CanAccess(Permission.Reports),
            "Warehouses"       => _sessionService.CanAccess(Permission.WarehouseManagement),
            "Users"            => _sessionService.CanAccess(Permission.UserManagement),
            "Settings"         => _sessionService.CanAccess(Permission.Settings),
            "Units"            => _sessionService.CanAccess(Permission.ProductManagement),
            "ProductUnits"     => _sessionService.CanAccess(Permission.ProductManagement),
            "ProductImport"    => _sessionService.CanAccess(Permission.ProductManagement),
            "Taxes"            => _sessionService.CanAccess(Permission.Settings),
            "Currencies"       => _sessionService.CanAccess(Permission.Currencies),
            "Dashboard"        => true,  // Everyone can see dashboard
            "Sales"            => _sessionService.CanAccess(Permission.SalesInvoice),
            "SalesReturns"     => _sessionService.CanAccess(Permission.SalesReturn),
            "Customers"        => _sessionService.CanAccess(Permission.CustomerView),
            "CashBoxes"        => _sessionService.CanAccess(Permission.CashBoxes),
            "Inventory"        => _sessionService.CanAccess(Permission.ProductManagement),
            "InventoryActivity" => _sessionService.CanAccess(Permission.ProductManagement),
            "Pos"              => _sessionService.CanAccess(Permission.SalesInvoice),
            "CustomerPayments" => _sessionService.CanAccess(Permission.CustomerView),
            _ => false // Deny by default — unknown screens require explicit permission
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
            // PurchaseOrderListViewModel removed — V1-deferred
            nameof(PurchaseReturnListViewModel)     => "PurchaseReturns",
            nameof(SupplierPaymentsListViewModel)   => "SupplierPayments",
            nameof(CashBoxesListViewModel)          => "CashBoxes",
            nameof(ReportsViewModel)                => "Reports",
            nameof(LowStockViewModel)               => "LowStock",
            nameof(ExpiredProductsReportViewModel)  => "ExpiredProducts",
            nameof(StockBalanceReportViewModel)     => "Reports",
            nameof(WarehouseMovementReportViewModel) => "Reports",
            nameof(DetailedStockLedgerViewModel)     => "Reports",
            nameof(ProductProfitabilityViewModel)    => "Reports",
            nameof(ProfitByCustomerViewModel)        => "Reports",
            nameof(ReturnsReportViewModel)           => "Reports",
            nameof(AgingReportViewModel)             => "Reports",
            nameof(WorkingCapitalViewModel)          => "Reports",
            nameof(AccountBalancesViewModel)         => "Reports",
            nameof(IncomeStatementViewModel)         => "Reports",
            nameof(CashFlowReportViewModel)           => "Reports",
            nameof(VatReportViewModel)                => "Reports",
            nameof(AccountStatementViewModel)         => "Reports",
            nameof(ProductListViewModel)            => "Products",
            nameof(CustomerListViewModel)           => "Customers",
            nameof(SupplierListViewModel)           => "Suppliers",
            nameof(WarehouseListViewModel)          => "Warehouses",
            nameof(UnitListViewModel)               => "Units",
            nameof(UserListViewModel)               => "Users",
            nameof(AuditLogListViewModel)           => "Settings",
            nameof(PermissionManagementViewModel)   => "Settings",
            nameof(RoleListViewModel)               => "Settings",
            nameof(RoleEditorViewModel)              => "Settings",
            nameof(RolePermissionViewModel)          => "Settings",
            nameof(UserSessionListViewModel)         => "Settings",
            nameof(SystemLogListViewModel)           => "Settings",
            nameof(SettingsViewModel)               => "Settings",
            nameof(SystemSettingsViewModel)         => "Settings",
            nameof(BackupViewModel)                 => "Settings",
            nameof(WarehouseTransfersListViewModel)     => "WarehouseTransfers",
            nameof(InventoryViewModel)              => "Inventory",
            nameof(InventoryTransactionListViewModel) => "InventoryActivity",
            nameof(ProductPricesListViewModel)      => "Products",
            nameof(InventoryBatchesViewModel)       => "Products",
            nameof(ProductUnitsListViewModel)       => "ProductUnits",
            nameof(ProductImportViewModel)          => "ProductImport",
            nameof(TaxesListViewModel)              => "Taxes",
            nameof(CurrenciesListViewModel)         => "Currencies",
            nameof(CurrencyRatesViewModel)          => "Currencies",
            nameof(AccountsListViewModel)           => "Settings",
            nameof(JournalEntriesListViewModel)     => "Settings",
            nameof(FiscalYearListViewModel)         => "Settings",
            // Organization Management
            nameof(BranchListViewModel)               => "Settings",
            nameof(DepartmentListViewModel)           => "Settings",
            nameof(EmployeeListViewModel)             => "Settings",
            nameof(BankListViewModel)                 => "Settings",
            nameof(PartyListViewModel)                => "Settings",
            nameof(ExpenseListViewModel)              => "Settings",
            // Customer Receipts
            nameof(CustomerReceiptListViewModel)      => "CustomerPayments",
            // Accounting - Receipt Vouchers
            nameof(ReceiptVoucherListViewModel)       => "Settings",
            // Payment Vouchers
            nameof(PaymentVoucherListViewModel)       => "Settings",
            // Inventory Operations
            nameof(InventoryCountListViewModel)       => "Inventory",
            nameof(InventoryAdjustmentListViewModel)  => "Inventory",
            // Notifications & Attachments
            nameof(NotificationListViewModel)         => "Settings",
            nameof(AttachmentListViewModel)           => "Settings",
            // Phase 31 — Reports
            nameof(BalanceSheetViewModel)           => "Reports",
            nameof(TrialBalanceViewModel)           => "Reports",
            nameof(GeneralLedgerViewModel)          => "Reports",
            nameof(SalesByCustomerViewModel)        => "Reports",
            nameof(SalesByProductViewModel)         => "Reports",
            nameof(SalesByCategoryViewModel)        => "Reports",
            nameof(DailySalesViewModel)             => "Reports",
            nameof(PurchasesBySupplierViewModel)    => "Reports",
            nameof(PurchasesByProductViewModel)     => "Reports",
            nameof(CashBoxSummaryViewModel)         => "Reports",
            nameof(UserActivityViewModel)           => "Reports",
            nameof(LoginHistoryViewModel)           => "Reports",
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
