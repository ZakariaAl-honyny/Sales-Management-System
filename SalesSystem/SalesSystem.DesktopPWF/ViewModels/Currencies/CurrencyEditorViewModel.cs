using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SalesSystem.DesktopPWF.ViewModels.Currencies;

public class CurrencyEditorViewModel : ViewModelBase
{
    private readonly ICurrencyApiService _currencyService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly ISoundService _soundService;
    private string _name = string.Empty;
    private string _code = string.Empty;
    private string _symbol = string.Empty;
    private decimal _exchangeRateToBase;
    private bool _isBaseCurrency;
    private string? _fractionName;
    private string? _errorMessage;
    private string _windowTitle = "إضافة عملة جديدة";
    private CurrencyDto? _currencyDto;
    private ObservableCollection<ExchangeRateHistoryDto> _rateHistory = new();

    public CurrencyEditorViewModel()
    {
        _currencyService = App.GetService<ICurrencyApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _soundService = App.GetService<ISoundService>();
        SetDialogService(_dialogService);
        InitializeCommands();
    }

    public void LoadCurrency(CurrencyDto currency)
    {
        _currencyDto = currency;
        Name = currency.Name;
        Code = currency.Code;
        Symbol = currency.Symbol;
        ExchangeRateToBase = currency.ExchangeRateToBase;
        IsBaseCurrency = currency.IsBaseCurrency;
        FractionName = currency.FractionName;
        WindowTitle = $"تعديل العملة: {currency.Name} ({currency.Code})";

        // Load rate history in background
        _ = LoadRateHistoryAsync();
    }

    private async Task LoadRateHistoryAsync()
    {
        if (_currencyDto == null) return;

        var result = await _currencyService.GetRateHistoryAsync(_currencyDto.Id);
        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(async () =>
            {
                RateHistory.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.EffectiveDate))
                {
                    RateHistory.Add(item);
                }
            });
        }
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ العملة...")));
        CancelCommand = new RelayCommand(() => RequestClose());
    }

    #region Properties

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "اسم العملة مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public string Code
    {
        get => _code;
        set
        {
            if (SetProperty(ref _code, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Code), "رمز العملة (ISO) مطلوب");
                else if (value.Trim().Length != 3)
                    AddError(nameof(Code), "رمز ISO يجب أن يكون ثلاثي الأحرف — مثال: USD");
                else
                    ClearErrors(nameof(Code));
            }
        }
    }

    public string Symbol
    {
        get => _symbol;
        set
        {
            if (SetProperty(ref _symbol, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Symbol), "رمز العملة مطلوب");
                else
                    ClearErrors(nameof(Symbol));
            }
        }
    }

    public decimal ExchangeRateToBase
    {
        get => _exchangeRateToBase;
        set
        {
            if (SetProperty(ref _exchangeRateToBase, value))
            {
                if (value <= 0)
                    AddError(nameof(ExchangeRateToBase), "سعر الصرف يجب أن يكون أكبر من صفر");
                else
                    ClearErrors(nameof(ExchangeRateToBase));
            }
        }
    }

    public bool IsBaseCurrency
    {
        get => _isBaseCurrency;
        set => SetProperty(ref _isBaseCurrency, value);
    }

    public string? FractionName
    {
        get => _fractionName;
        set => SetProperty(ref _fractionName, value);
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

    public ObservableCollection<ExchangeRateHistoryDto> RateHistory
    {
        get => _rateHistory;
        set => SetProperty(ref _rateHistory, value);
    }

    public bool IsEditing => _currencyDto != null;

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        // If setting as base currency, show confirmation
        if (IsBaseCurrency && (_currencyDto == null || !_currencyDto.IsBaseCurrency))
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "تعيين عملة أساسية",
                "سيتم تعيين هذه العملة كعملة أساسية للنظام. العملة الأساسية السابقة ستصبح غير أساسية.\n\nهل أنت متأكد؟");
            if (!confirmed) return;
        }

        Result<CurrencyDto> result;
        if (_currencyDto == null)
        {
            var request = new CreateCurrencyRequest(Name, Code, Symbol, ExchangeRateToBase, IsBaseCurrency, FractionName);
            result = await _currencyService.CreateAsync(request);
        }
        else
        {
            var request = new UpdateCurrencyRequest(Name, Symbol, ExchangeRateToBase, IsBaseCurrency, FractionName, _currencyDto.IsActive);
            result = await _currencyService.UpdateAsync(_currencyDto.Id, request);
        }

        if (result.IsSuccess && result.Value != null)
        {
            _eventBus.Publish(new CurrencyChangedMessage(result.Value.Id));
            _soundService.PlaySuccess();
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ العملة", "CurrencyEditorViewModel.SaveAsync");
            await _dialogService.ShowErrorAsync("خطأ في حفظ العملة", ErrorMessage!);
            _soundService.PlayError();
        }
    }

    private async Task<bool> ValidateAsync()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("• اسم العملة مطلوب");
        if (string.IsNullOrWhiteSpace(Code))
            errors.Add("• رمز العملة (ISO) مطلوب");
        else if (Code.Trim().Length != 3)
            errors.Add("• رمز ISO يجب أن يكون ثلاثي الأحرف — مثال: USD");
        if (string.IsNullOrWhiteSpace(Symbol))
            errors.Add("• رمز العملة مطلوب");
        if (ExchangeRateToBase <= 0)
            errors.Add("• سعر الصرف يجب أن يكون أكبر من صفر");

        if (errors.Any())
        {
            await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", errors);
            RequestFocusFirstInvalidField();
            return false;
        }
        return true;
    }

    #endregion
}
