using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Currencies;

public class CurrencyRatesViewModel : ViewModelBase, IDisposable
{
    private readonly ICurrencyApiService _currencyService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly ISoundService _soundService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<CurrencyDto> _currencies = new();
    private CurrencyDto? _selectedCurrency;
    private decimal _newRate;
    private DateTime _effectiveFromDate = DateTime.Today;
    private string? _errorMessage;
    private string _windowTitle = "أسعار العملات";

    public CurrencyRatesViewModel()
        : this(
            App.GetService<ICurrencyApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<ISoundService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public CurrencyRatesViewModel(
        ICurrencyApiService currencyService,
        IDialogService dialogService,
        IEventBus eventBus,
        ISoundService soundService,
        IToastNotificationService toastService)
    {
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        SetDialogService(_dialogService);

        InitializeCommands();

        // Subscribe to EventBus for cross-module updates
        _eventBus.Subscribe<CurrencyRateChangedMessage>(OnCurrencyRateChanged);
        _eventBus.Subscribe<CurrencyChangedMessage>(OnCurrencyChanged);

        // Load currencies on initialization
        _ = LoadCurrenciesAsync();
    }

    #region Properties

    public ObservableCollection<CurrencyDto> Currencies
    {
        get => _currencies;
        set => SetProperty(ref _currencies, value);
    }

    public CurrencyDto? SelectedCurrency
    {
        get => _selectedCurrency;
        set
        {
            if (SetProperty(ref _selectedCurrency, value))
            {
                if (value == null)
                    AddError(nameof(SelectedCurrency), "يجب اختيار العملة");
                else
                    ClearErrors(nameof(SelectedCurrency));

                OnPropertyChanged(nameof(IsCurrencySelected));

            }
        }
    }

    public bool IsCurrencySelected => SelectedCurrency != null;

    public decimal NewRate
    {
        get => _newRate;
        set
        {
            if (SetProperty(ref _newRate, value))
            {
                if (value <= 0)
                    AddError(nameof(NewRate), "سعر الصرف يجب أن يكون أكبر من صفر");
                else
                    ClearErrors(nameof(NewRate));
            }
        }
    }

    public DateTime EffectiveFromDate
    {
        get => _effectiveFromDate;
        set => SetProperty(ref _effectiveFromDate, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string WindowTitle
    {
        get => _windowTitle;
        set => SetProperty(ref _windowTitle, value);
    }

    #endregion

    #region Commands

    public ICommand AddRateCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Command Initialization

    private void InitializeCommands()
    {
        AddRateCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(AddRateOperationAsync,
                ex =>
                {
                    ErrorMessage = HandleException(ex, "CurrencyRatesViewModel.AddRateAsync");
                    _ = _dialogService.ShowErrorAsync("خطأ في إضافة سعر الصرف", ErrorMessage!);
                    _soundService.PlayError();
                }, "جاري إضافة سعر الصرف...")));

        CancelCommand = new RelayCommand(() => RequestClose());
    }

    #endregion

    #region Methods

    public async Task LoadCurrenciesAsync()
    {
        await ExecuteAsync(LoadCurrenciesOperationAsync,
            ex => ErrorMessage = HandleException(ex, "CurrencyRatesViewModel.LoadCurrenciesAsync"));
    }

    private async Task LoadCurrenciesOperationAsync()
    {
        ErrorMessage = null;

        var result = await _currencyService.GetAllAsync(includeInactive: false);

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var selectedId = SelectedCurrency?.Id;
                Currencies.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Currencies.Add(item);
                }

                // Preserve selected currency if still in list
                if (selectedId.HasValue)
                {
                    var preserved = Currencies.FirstOrDefault(c => c.Id == selectedId.Value);
                    if (preserved != null)
                        SelectedCurrency = preserved;
                }
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل العملات", "CurrencyRatesViewModel.LoadCurrenciesAsync");
        }
    }

    private async Task AddRateOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        // Exchange rates are now managed via CurrencyRates table (separate service)
        // TODO: Use new ICurrencyRateApiService when implemented
        await _dialogService.ShowWarningAsync("تحت التطوير", "إدارة أسعار الصرف عبر سجل الأسعار قيد التطوير");
        return;
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (SelectedCurrency == null)
            AddError(nameof(SelectedCurrency), "يجب اختيار العملة");

        if (NewRate <= 0)
            AddError(nameof(NewRate), "سعر الصرف يجب أن يكون أكبر من صفر");

        return await ValidateAllAsync();
    }

    private void OnCurrencyRateChanged(CurrencyRateChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadCurrenciesAsync();
        });
    }

    private void OnCurrencyChanged(CurrencyChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadCurrenciesAsync();
        });
    }

    #endregion

    #region IDisposable

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<CurrencyRateChangedMessage>(OnCurrencyRateChanged);
        _eventBus.Unsubscribe<CurrencyChangedMessage>(OnCurrencyChanged);
    }

    public void Dispose()
    {
        Cleanup();
    }

    #endregion
}
