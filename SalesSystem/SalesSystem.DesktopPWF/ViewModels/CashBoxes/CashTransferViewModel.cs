using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.CashBoxes;

public class CashTransferViewModel : ViewModelBase
{
    private readonly ICashBoxApiService _cashBoxService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<CashBoxDto> _sourceCashBoxes = new();
    private ObservableCollection<CashBoxDto> _destinationCashBoxes = new();
    private CashBoxDto? _selectedSourceCashBox;
    private CashBoxDto? _selectedDestinationCashBox;
    private decimal _amount;
    private string? _notes;
    private string? _errorMessage;

    public CashTransferViewModel()
        : this(
            App.GetService<ICashBoxApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public CashTransferViewModel(
        ICashBoxApiService cashBoxService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService? toastService = null)
    {
        _cashBoxService = cashBoxService ?? throw new ArgumentNullException(nameof(cashBoxService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        SetDialogService(_dialogService);
        InitializeCommands();
    }

    private void InitializeCommands()
    {
        TransferCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(TransferOperationAsync, "جاري تحويل الأموال...")));
        CancelCommand = new RelayCommand(Cancel);
        LoadCommand = new AsyncRelayCommand(LoadDataAsync);
    }

    #region Properties

    public ObservableCollection<CashBoxDto> SourceCashBoxes
    {
        get => _sourceCashBoxes;
        set => SetProperty(ref _sourceCashBoxes, value);
    }

    public ObservableCollection<CashBoxDto> DestinationCashBoxes
    {
        get => _destinationCashBoxes;
        set => SetProperty(ref _destinationCashBoxes, value);
    }

    public CashBoxDto? SelectedSourceCashBox
    {
        get => _selectedSourceCashBox;
        set => SetProperty(ref _selectedSourceCashBox, value);
    }

    public CashBoxDto? SelectedDestinationCashBox
    {
        get => _selectedDestinationCashBox;
        set => SetProperty(ref _selectedDestinationCashBox, value);
    }

    public decimal Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region Commands

    public ICommand TransferCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand LoadCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadDataAsync()
    {
        await ExecuteAsync(LoadDataOperationAsync);
    }

    private async Task LoadDataOperationAsync()
    {
        ErrorMessage = null;
        var result = await _cashBoxService.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            var activeBoxes = result.Value.Where(b => b.IsActive).ToList();

            InvokeOnUIThread(() =>
            {
                SourceCashBoxes.Clear();
                DestinationCashBoxes.Clear();
                foreach (var box in activeBoxes)
                {
                    SourceCashBoxes.Add(box);
                    DestinationCashBoxes.Add(box);
                }
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الخزائن", "CashTransferViewModel.LoadDataOperationAsync", "[CashTransferViewModel.LoadDataOperationAsync] Failed to load cash boxes from API.");
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (SelectedSourceCashBox == null)
            AddError(nameof(SelectedSourceCashBox), "يجب اختيار الخزنة المصدر");

        if (SelectedDestinationCashBox == null)
            AddError(nameof(SelectedDestinationCashBox), "يجب اختيار الخزنة الهدف");

        if (SelectedSourceCashBox != null && SelectedDestinationCashBox != null &&
            SelectedSourceCashBox.Id == SelectedDestinationCashBox.Id)
            AddError(nameof(SelectedDestinationCashBox), "لا يمكن التحويل لنفس الخزنة");

        if (Amount <= 0)
            AddError(nameof(Amount), "مبلغ التحويل يجب أن يكون أكبر من صفر");

        if (SelectedSourceCashBox != null && Amount > 0 &&
            SelectedSourceCashBox.CurrentBalance < Amount)
            AddError(nameof(Amount), "الرصيد غير كافٍ لإتمام التحويل");

        return await ValidateAllAsync();
    }

    private async Task TransferOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        var request = new CashTransferRequest(
            SelectedSourceCashBox!.Id,
            SelectedDestinationCashBox!.Id,
            Amount,
            Notes);

        var result = await _cashBoxService.TransferAsync(request);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new CashBoxChangedMessage(SelectedSourceCashBox.Id));
            _eventBus.Publish(new CashBoxChangedMessage(SelectedDestinationCashBox.Id));
            _toastService.ShowSuccess("تم التحويل بنجاح");
            RequestClose();
        }
        else
        {
            var error = HandleFailure(result.Error ?? "فشل في إتمام التحويل", "CashTransferViewModel.TransferOperationAsync", "[CashTransferViewModel.TransferOperationAsync] Failed to transfer cash.");
            ErrorMessage = error;
            _toastService.ShowError(ErrorMessage);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    #endregion
}
