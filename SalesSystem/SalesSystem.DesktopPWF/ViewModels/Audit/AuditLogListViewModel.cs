using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.ViewModels.Base;

namespace SalesSystem.DesktopPWF.ViewModels.Audit;

public class AuditLogListViewModel : AdminOnlyViewModel
{
    private readonly IAuditLogApiService _auditLogService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    private ObservableCollection<AuditLogDto> _logs = new();
    private string _searchText = string.Empty;
    private string? _selectedAction;
    private string? _selectedEntityType;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private int _page = 1;
    private int _totalPages = 1;
    private int _totalRecords;
    private string? _errorMessage;
    private bool _isEmpty;

    public AuditLogListViewModel()
        : this(App.GetService<ISessionService>())
    {
    }

    public AuditLogListViewModel(ISessionService sessionService)
        : base(sessionService)
    {
        _auditLogService = App.GetService<IAuditLogApiService>();
        _dialogService = App.GetService<IDialogService>();
        _sessionService = sessionService;
        SetDialogService(_dialogService);

        ActionFilters = new ObservableCollection<string>
        {
            "Login", "Logout", "Create", "Update", "Delete", "Post", "Cancel", "Print"
        };
        EntityTypeFilters = new ObservableCollection<string>
        {
            "User", "SalesInvoice", "PurchaseInvoice", "Product", "Customer",
            "Supplier", "Payment", "WarehouseTransfer", "Currency", "Tax"
        };

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        SearchCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadLogsOperationAsync)));
        NextPageCommand = new RelayCommand(NextPage, () => Page < TotalPages);
        PrevPageCommand = new RelayCommand(PrevPage, () => Page > 1);
        RefreshCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadLogsOperationAsync)));
        ViewUserHistoryCommand = new AsyncRelayCommand(ViewUserHistoryAsync);
        ViewLoginHistoryCommand = new AsyncRelayCommand(ViewLoginHistoryAsync);
    }

    #region Properties

    public ObservableCollection<AuditLogDto> Logs
    {
        get => _logs;
        set => SetProperty(ref _logs, value);
    }

    public ObservableCollection<string> ActionFilters { get; }
    public ObservableCollection<string> EntityTypeFilters { get; }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string? SelectedAction
    {
        get => _selectedAction;
        set => SetProperty(ref _selectedAction, value);
    }

    public string? SelectedEntityType
    {
        get => _selectedEntityType;
        set => SetProperty(ref _selectedEntityType, value);
    }

    public DateTime? FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public int Page
    {
        get => _page;
        set
        {
            if (SetProperty(ref _page, value))
            {
                OnPropertyChanged(nameof(PageDisplay));
                (PrevPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        set
        {
            if (SetProperty(ref _totalPages, value))
            {
                OnPropertyChanged(nameof(PageDisplay));
                (PrevPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalRecords
    {
        get => _totalRecords;
        set => SetProperty(ref _totalRecords, value);
    }

    public string PageDisplay => $"الصفحة {Page} من {TotalPages} — {TotalRecords} سجل";

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    #endregion

    #region Commands

    public ICommand SearchCommand { get; private set; } = null!;
    public ICommand NextPageCommand { get; private set; } = null!;
    public ICommand PrevPageCommand { get; private set; } = null!;
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand ViewUserHistoryCommand { get; private set; } = null!;
    public ICommand ViewLoginHistoryCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public Task LoadLogsAsync() => ExecuteAsync(LoadLogsOperationAsync);

    private async Task LoadLogsOperationAsync()
    {
        ErrorMessage = null;

        var query = new AuditLogQuery
        {
            Action = SelectedAction,
            EntityType = SelectedEntityType,
            From = FromDate,
            To = ToDate,
            Page = Page,
            PageSize = 50
        };

        var result = await _auditLogService.QueryAsync(query);

        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(() =>
            {
                Logs.Clear();
                foreach (var item in result.Value.Items)
                {
                    Logs.Add(item);
                }
                TotalPages = result.Value.TotalPages > 0 ? result.Value.TotalPages : 1;
                TotalRecords = result.Value.TotalCount;
                IsEmpty = Logs.Count == 0;
                return System.Threading.Tasks.Task.CompletedTask;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل سجل الأحداث", "AuditLogListViewModel.LoadLogsOperationAsync");
            IsEmpty = Logs.Count == 0;
        }
    }

    private void NextPage()
    {
        if (Page < TotalPages)
        {
            Page++;
            _ = LoadLogsAsync();
        }
    }

    private void PrevPage()
    {
        if (Page > 1)
        {
            Page--;
            _ = LoadLogsAsync();
        }
    }

    private async Task ViewUserHistoryAsync()
    {
        var userId = _sessionService.GetUserId();
        if (!userId.HasValue) return;

        _ = _dialogService.ShowInfoAsync("سجل نشاط المستخدم",
            "يتم عرض سجل نشاط المستخدم الحالي — سيتم تفعيل التصفية حسب المستخدم في القادم");
    }

    private async Task ViewLoginHistoryAsync()
    {
        await ExecuteAsync(LoadLoginHistoryOperationAsync);
    }

    private async Task LoadLoginHistoryOperationAsync()
    {
        ErrorMessage = null;
        var userId = _sessionService.GetUserId();
        var result = await _auditLogService.GetLoginHistoryAsync(userId);

        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(() =>
            {
                Logs.Clear();
                foreach (var item in result.Value)
                {
                    Logs.Add(item);
                }
                IsEmpty = Logs.Count == 0;
                TotalRecords = Logs.Count;
                TotalPages = 1;
                Page = 1;
                return System.Threading.Tasks.Task.CompletedTask;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل سجل الدخول", "AuditLogListViewModel.LoadLoginHistoryOperationAsync");
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}
