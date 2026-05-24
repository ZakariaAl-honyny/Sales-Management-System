using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.CashBoxes;

public class CashBoxesListViewModel : ViewModelBase, IDisposable
{
    private readonly ICashBoxApiService _cashBoxService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IToastNotificationService _toastService;
    private readonly Lazy<CashBoxEditorViewModel> _editorVmFactory;
    private readonly Lazy<CashTransferViewModel> _transferVmFactory;
    private readonly Lazy<CashBoxTransactionsViewModel> _transactionsVmFactory;
    private readonly Lazy<DailyClosureViewModel> _closureVmFactory;

    private ObservableCollection<CashBoxDto> _cashBoxes = new();
    private CashBoxDto? _selectedCashBox;
    private string? _errorMessage;
    private bool _isEmpty;

    public CashBoxesListViewModel()
        : this(
            App.GetService<ICashBoxApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IScreenWindowService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public CashBoxesListViewModel(
        ICashBoxApiService cashBoxService,
        IDialogService dialogService,
        IEventBus eventBus,
        IScreenWindowService screenWindowService,
        IToastNotificationService? toastService = null)
    {
        _cashBoxService = cashBoxService ?? throw new ArgumentNullException(nameof(cashBoxService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        _editorVmFactory = new Lazy<CashBoxEditorViewModel>(() => App.GetService<CashBoxEditorViewModel>());
        _transferVmFactory = new Lazy<CashTransferViewModel>(() => App.GetService<CashTransferViewModel>());
        _transactionsVmFactory = new Lazy<CashBoxTransactionsViewModel>(() => App.GetService<CashBoxTransactionsViewModel>());
        _closureVmFactory = new Lazy<DailyClosureViewModel>(() => App.GetService<DailyClosureViewModel>());

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadCashBoxesAsync);
        AddCommand = new RelayCommand(AddCashBox);
        EditCommand = new RelayCommand(EditCashBox);
        DeactivateCommand = new AsyncRelayCommand(DeactivateCashBoxAsync);
        ViewTransactionsCommand = new RelayCommand(ViewTransactions);
        TransferCommand = new RelayCommand(TransferCash);
        CloseDayCommand = new RelayCommand(CloseDay);
    }

    public void OnNavigatedTo()
    {
        _eventBus.Subscribe<CashBoxChangedMessage>(OnCashBoxChanged);
        _ = LoadCashBoxesAsync();
    }

    #region Properties

    public ObservableCollection<CashBoxDto> CashBoxes
    {
        get => _cashBoxes;
        set => SetProperty(ref _cashBoxes, value);
    }

    public CashBoxDto? SelectedCashBox
    {
        get => _selectedCashBox;
        set => SetProperty(ref _selectedCashBox, value);
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

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeactivateCommand { get; private set; } = null!;
    public ICommand ViewTransactionsCommand { get; private set; } = null!;
    public ICommand TransferCommand { get; private set; } = null!;
    public ICommand CloseDayCommand { get; private set; } = null!;

    private void TransferCash()
    {
        var transferVm = _transferVmFactory.Value;
        _screenWindowService.OpenScreen(transferVm, new ScreenWindowOptions
        {
            Title = "تحويل بين الخزائن",
            Width = 600,
            Height = 500,
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCashBoxesAsync());
            }
        });
    }

    private void CloseDay()
    {
        if (SelectedCashBox == null) return;

        var closureVm = _closureVmFactory.Value;
        closureVm.CashBoxId = SelectedCashBox.Id;
        closureVm.CashBoxName = SelectedCashBox.BoxName;
        _screenWindowService.OpenScreen(closureVm, new ScreenWindowOptions
        {
            Title = $"الإغلاق اليومي - {SelectedCashBox.BoxName}",
            Width = 900,
            Height = 700,
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCashBoxesAsync());
            }
        });
    }

    #endregion

    #region Methods

    public async Task LoadCashBoxesAsync()
    {
        await ExecuteAsync(LoadCashBoxesOperationAsync);
    }

    private async Task LoadCashBoxesOperationAsync()
    {
        ErrorMessage = null;
        var result = await _cashBoxService.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                CashBoxes.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    CashBoxes.Add(item);
                }
                IsEmpty = CashBoxes.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الصناديق النقدية", "CashBoxesListViewModel.LoadCashBoxesOperationAsync", "[CashBoxesListViewModel.LoadCashBoxesOperationAsync] Failed to load cash boxes from API.");
            IsEmpty = CashBoxes.Count == 0;
        }
    }

    private void AddCashBox()
    {
        var editorVm = _editorVmFactory.Value;
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "إضافة صندوق نقدي جديد",
            OnClosed = (vm) =>
            {
                if (vm is CashBoxEditorViewModel editor && !string.IsNullOrEmpty(editor.BoxName))
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCashBoxesAsync());
                }
            }
        });
    }

    private void EditCashBox()
    {
        if (SelectedCashBox == null) return;

        var editorVm = _editorVmFactory.Value;
        editorVm.LoadForEdit(SelectedCashBox.BoxName, SelectedCashBox.CurrentBalance);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل الصندوق النقدي",
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCashBoxesAsync());
            }
        });
    }

    public async Task DeactivateCashBoxAsync()
    {
        if (SelectedCashBox == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"الصندوق: {SelectedCashBox.BoxName}");
        if (strategy == DeleteStrategy.Cancel) return;

        var cashBoxId = SelectedCashBox.Id;
        await ExecuteAsync(() => DeactivateCashBoxOperationAsync(cashBoxId));
    }

    private async Task DeactivateCashBoxOperationAsync(int cashBoxId)
    {
        ErrorMessage = null;
        var deleteResult = await _cashBoxService.DeactivateAsync(cashBoxId);

        if (deleteResult.IsSuccess)
        {
            _eventBus.Publish(new CashBoxChangedMessage(cashBoxId));
            await LoadCashBoxesAsync();
            _toastService.ShowSuccess("تم إلغاء تنشيط الصندوق بنجاح");
        }
        else
        {
            var error = deleteResult.Error ?? "فشل في إلغاء تنشيط الصندوق";
            ErrorMessage = HandleFailure(error, "CashBoxesListViewModel.DeactivateCashBoxOperationAsync", "[CashBoxesListViewModel.DeactivateCashBoxOperationAsync] Failed to deactivate cash box.");
            _toastService.ShowError(ErrorMessage);
        }
    }

    private void ViewTransactions()
    {
        if (SelectedCashBox == null) return;

        var transactionsVm = _transactionsVmFactory.Value;
        transactionsVm.CashBoxId = SelectedCashBox.Id;
        transactionsVm.CashBoxName = SelectedCashBox.BoxName;
        _screenWindowService.OpenScreen(transactionsVm, new ScreenWindowOptions
        {
            Title = $"حركات الصندوق - {SelectedCashBox.BoxName}",
            Width = 1000,
            Height = 700,
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCashBoxesAsync());
            }
        });
    }

    private void OnCashBoxChanged(CashBoxChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadCashBoxesAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<CashBoxChangedMessage>(OnCashBoxChanged);
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    #endregion
}
