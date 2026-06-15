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

namespace SalesSystem.DesktopPWF.ViewModels.CustomerReceipt;

/// <summary>
/// ViewModel for Customer Receipts List View
/// </summary>
public class CustomerReceiptListViewModel : ViewModelBase
{
    private readonly ICustomerReceiptApiService _receiptService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<CustomerReceiptDto> _receipts = new();
    private ICollectionView? _receiptsView;
    private CustomerReceiptDto? _selectedReceipt;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public CustomerReceiptListViewModel()
    {
        _receiptService = App.GetService<ICustomerReceiptApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();

        InitializeCommands();
    }

    public CustomerReceiptListViewModel(
        ICustomerReceiptApiService receiptService,
        IEventBus eventBus,
        IDialogService dialogService,
        IScreenWindowService screenWindowService,
        IToastNotificationService toastService)
    {
        _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadReceiptsAsync);
        AddCommand = new RelayCommand(AddReceipt);
        EditCommand = new RelayCommand(EditReceipt);
        PostCommand = new AsyncRelayCommand(PostReceiptAsync);
        CancelCommand = new AsyncRelayCommand(CancelReceiptAsync);
        SearchCommand = new RelayCommand(Search);

        _eventBus.Subscribe<CustomerReceiptChangedMessage>(OnReceiptChanged);
    }

    #region Properties
    public ObservableCollection<CustomerReceiptDto> Receipts
    {
        get => _receipts;
        set => SetProperty(ref _receipts, value);
    }

    public ICollectionView? ReceiptsView
    {
        get => _receiptsView;
        private set => SetProperty(ref _receiptsView, value);
    }

    public CustomerReceiptDto? SelectedReceipt
    {
        get => _selectedReceipt;
        set => SetProperty(ref _selectedReceipt, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ReceiptsView?.Refresh();
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

    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (SetProperty(ref _includeInactive, value))
            {
                _ = LoadReceiptsAsync();
            }
        }
    }

    public int ReceiptsCount => Receipts.Count;
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
    public async Task LoadReceiptsAsync()
    {
        await ExecuteAsync(LoadReceiptsOperationAsync);
    }

    private async Task LoadReceiptsOperationAsync()
    {
        ErrorMessage = null;
        var result = await _receiptService.GetAllAsync(IncludeInactive);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Receipts.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Receipts.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Receipts.Count == 0;
                OnPropertyChanged(nameof(ReceiptsCount));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل سندات القبض", "CustomerReceiptListViewModel.LoadReceiptsAsync", "[CustomerReceiptListViewModel.LoadReceiptsAsync] Failed to load receipts.");
            IsEmpty = Receipts.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        ReceiptsView = new ListCollectionView(Receipts);
        ReceiptsView.Filter = FilterReceipts;
    }

    private bool FilterReceipts(object obj)
    {
        if (obj is not CustomerReceiptDto receipt) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            if (!receipt.ReceiptNo.ToString().Contains(searchLower) &&
                (receipt.CustomerName?.ToLower().Contains(searchLower) ?? false) == false &&
                (receipt.Notes?.ToLower().Contains(searchLower) ?? false) == false)
                return false;
        }

        return true;
    }

    private void AddReceipt()
    {
        var editorVm = App.GetService<CustomerReceiptEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "سند قبض جديد",
            Width = 700,
            Height = 600,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadReceiptsAsync());
            }
        });
    }

    private void EditReceipt()
    {
        if (SelectedReceipt == null)
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار سند قبض");
            return;
        }

        var editorVm = new CustomerReceiptEditorViewModel(SelectedReceipt);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل سند قبض",
            Width = 700,
            Height = 600,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadReceiptsAsync());
            }
        });
    }

    public void EditReceiptFromDoubleClick()
    {
        if (SelectedReceipt != null)
            EditReceipt();
    }

    public async Task PostReceiptAsync()
    {
        if (SelectedReceipt == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار سند قبض");
            return;
        }

        if (SelectedReceipt.Status != 1) // Draft only
        {
            await _dialogService.ShowWarningAsync("تنبيه", "لا يمكن ترحيل السند إلا في حالة مسودة");
            return;
        }

        await ExecuteAsync(PostReceiptOperationAsync);
    }

    private async Task PostReceiptOperationAsync()
    {
        ErrorMessage = null;
        var result = await _receiptService.PostAsync(SelectedReceipt!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new CustomerReceiptChangedMessage(SelectedReceipt.Id));
            await LoadReceiptsAsync();
            _toastService.ShowSuccess("تم ترحيل سند القبض بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في ترحيل سند القبض", "CustomerReceiptListViewModel.PostReceiptAsync", $"[CustomerReceiptListViewModel.PostReceiptAsync] Failed to post receipt {SelectedReceipt.Id}.");
        }
    }

    public async Task CancelReceiptAsync()
    {
        if (SelectedReceipt == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار سند قبض");
            return;
        }

        if (SelectedReceipt.Status != 1) // Draft only
        {
            await _dialogService.ShowWarningAsync("تنبيه", "لا يمكن إلغاء السند إلا في حالة مسودة");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء", "هل أنت متأكد من إلغاء سند القبض؟");
        if (!confirmed) return;

        await ExecuteAsync(CancelReceiptOperationAsync);
    }

    private async Task CancelReceiptOperationAsync()
    {
        ErrorMessage = null;
        var result = await _receiptService.CancelAsync(SelectedReceipt!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new CustomerReceiptChangedMessage(SelectedReceipt.Id));
            await LoadReceiptsAsync();
            _toastService.ShowSuccess("تم إلغاء سند القبض بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء سند القبض", "CustomerReceiptListViewModel.CancelReceiptAsync", $"[CustomerReceiptListViewModel.CancelReceiptAsync] Failed to cancel receipt {SelectedReceipt.Id}.");
        }
    }

    private void Search()
    {
        ReceiptsView?.Refresh();
    }

    private void OnReceiptChanged(CustomerReceiptChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () => await LoadReceiptsAsync());
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<CustomerReceiptChangedMessage>(OnReceiptChanged);
    }
    #endregion
}
