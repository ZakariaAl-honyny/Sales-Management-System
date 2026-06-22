using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.InventoryCount;

/// <summary>
/// ViewModel for Inventory Counts List View
/// </summary>
public class InventoryCountListViewModel : ViewModelBase
{
    private readonly IInventoryCountApiService _countService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<InventoryCountDto> _counts = new();
    private ICollectionView? _countsView;
    private InventoryCountDto? _selectedCount;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;

    public InventoryCountListViewModel()
    {
        _countService = App.GetService<IInventoryCountApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();

        InitializeCommands();
    }

    public InventoryCountListViewModel(
        IInventoryCountApiService countService,
        IEventBus eventBus,
        IDialogService dialogService,
        IScreenWindowService screenWindowService,
        IToastNotificationService toastService)
    {
        _countService = countService ?? throw new ArgumentNullException(nameof(countService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadCountsAsync);
        AddCommand = new RelayCommand(AddCount);
        EditCommand = new RelayCommand(EditCount);
        PostCommand = new AsyncRelayCommand(PostCountAsync);
        CancelCommand = new AsyncRelayCommand(CancelCountAsync);
        SearchCommand = new RelayCommand(Search);

        _eventBus.Subscribe<InventoryCountChangedMessage>(OnCountChanged);
    }

    #region Properties
    public ObservableCollection<InventoryCountDto> Counts
    {
        get => _counts;
        set => SetProperty(ref _counts, value);
    }

    public ICollectionView? CountsView
    {
        get => _countsView;
        private set => SetProperty(ref _countsView, value);
    }

    public InventoryCountDto? SelectedCount
    {
        get => _selectedCount;
        set => SetProperty(ref _selectedCount, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                CountsView?.Refresh();
            }
        }
    }

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

    public int CountsCount => Counts.Count;
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand PostCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;
    #endregion

    #region Methods
    public async Task LoadCountsAsync()
    {
        await ExecuteAsync(LoadCountsOperationAsync);
    }

    private async Task LoadCountsOperationAsync()
    {
        ErrorMessage = null;
        var result = await _countService.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Counts.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Counts.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Counts.Count == 0;
                OnPropertyChanged(nameof(CountsCount));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الجرد", "InventoryCountListViewModel.LoadCountsAsync", "[InventoryCountListViewModel.LoadCountsAsync] Failed to load counts.");
            IsEmpty = Counts.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        CountsView = new ListCollectionView(Counts);
        CountsView.Filter = FilterCounts;
    }

    private bool FilterCounts(object obj)
    {
        if (obj is not InventoryCountDto count) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            if (!count.CountNo.ToString().Contains(searchLower) &&
                (count.WarehouseName?.ToLower().Contains(searchLower) ?? false) == false)
                return false;
        }

        return true;
    }

    private void AddCount()
    {
        var editorVm = App.GetService<InventoryCountEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "جرد جديد",
            Width = 900,
            Height = 700,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCountsAsync());
            }
        });
    }

    private void EditCount()
    {
        if (SelectedCount == null)
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار جرد");
            return;
        }

        var editorVm = new InventoryCountEditorViewModel(SelectedCount);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل الجرد",
            Width = 900,
            Height = 700,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCountsAsync());
            }
        });
    }

    public void EditCountFromDoubleClick()
    {
        if (SelectedCount != null)
            EditCount();
    }

    public async Task PostCountAsync()
    {
        if (SelectedCount == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار جرد");
            return;
        }

        if (SelectedCount.Status != 1)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "لا يمكن ترحيل الجرد إلا في حالة مسودة");
            return;
        }

        await ExecuteAsync(PostCountOperationAsync);
    }

    private async Task PostCountOperationAsync()
    {
        ErrorMessage = null;
        var result = await _countService.PostAsync(SelectedCount!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new InventoryCountChangedMessage(SelectedCount.Id));
            await LoadCountsAsync();
            _toastService.ShowSuccess("تم ترحيل الجرد بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في ترحيل الجرد", "InventoryCountListViewModel.PostCountAsync", $"[InventoryCountListViewModel.PostCountAsync] Failed to post count {SelectedCount.Id}.");
            await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage!);
        }
    }

    public async Task CancelCountAsync()
    {
        if (SelectedCount == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار جرد");
            return;
        }

        if (SelectedCount.Status != 1)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "لا يمكن إلغاء الجرد إلا في حالة مسودة");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء", "هل أنت متأكد من إلغاء الجرد؟");
        if (!confirmed) return;

        await ExecuteAsync(CancelCountOperationAsync);
    }

    private async Task CancelCountOperationAsync()
    {
        ErrorMessage = null;
        var result = await _countService.CancelAsync(SelectedCount!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new InventoryCountChangedMessage(SelectedCount.Id));
            await LoadCountsAsync();
            _toastService.ShowSuccess("تم إلغاء الجرد بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء الجرد", "InventoryCountListViewModel.CancelCountAsync", $"[InventoryCountListViewModel.CancelCountAsync] Failed to cancel count {SelectedCount.Id}.");
            await _dialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage!);
        }
    }

    private void Search()
    {
        CountsView?.Refresh();
    }

    private void OnCountChanged(InventoryCountChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () => await LoadCountsAsync());
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<InventoryCountChangedMessage>(OnCountChanged);
    }
    #endregion
}
