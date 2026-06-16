using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Currencies;

public class CurrencyEditorViewModel : ViewModelBase
{
    private readonly ICurrencyApiService _currencyService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly ISoundService _soundService;
    private readonly IToastNotificationService _toastService;
    private string _name = string.Empty;
    private string _code = string.Empty;
    private string _symbol = string.Empty;

    private string? _fractionName;
    private int _decimalPlaces = 2;
    private string? _errorMessage;
    private string _windowTitle = "إضافة عملة جديدة";
    private CurrencyDto? _currencyDto;

    public CurrencyEditorViewModel()
        : this(
            App.GetService<ICurrencyApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<ISoundService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public CurrencyEditorViewModel(
        ICurrencyApiService currencyService,
        IDialogService dialogService,
        IEventBus eventBus,
        ISoundService soundService,
        IToastNotificationService? toastService = null)
    {
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);
        InitializeCommands();
    }

    public void LoadCurrency(CurrencyDto currency)
    {
        _currencyDto = currency;
        Name = currency.Name;
        Code = currency.Code;
        Symbol = currency.Symbol ?? string.Empty;
        FractionName = currency.FractionName;
        DecimalPlaces = currency.DecimalPlaces;
        WindowTitle = $"تعديل العملة: {currency.Name} ({currency.Code})";
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync,
            ex =>
            {
                ErrorMessage = HandleException(ex, "CurrencyEditorViewModel.SaveAsync");
                _ = _dialogService.ShowErrorAsync("خطأ في حفظ العملة", ErrorMessage!);
                _soundService.PlayError();
            }, "جاري حفظ العملة...")));
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

    public string? FractionName
    {
        get => _fractionName;
        set => SetProperty(ref _fractionName, value);
    }

    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set
        {
            if (SetProperty(ref _decimalPlaces, value))
            {
                if (value < 0 || value > 4)
                    AddError(nameof(DecimalPlaces), "عدد المنازل العشرية يجب أن يكون بين 0 و 4");
                else
                    ClearErrors(nameof(DecimalPlaces));
            }
        }
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

    public bool IsBaseCurrencyReadOnly => _currencyDto?.IsBaseCurrency ?? false;

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

        Result<CurrencyDto> result;
        if (_currencyDto == null)
        {
            var request = new CreateCurrencyRequest(Name, Code, Symbol, IsBaseCurrency: false, FractionName ?? string.Empty, (byte)DecimalPlaces);
            result = await _currencyService.CreateAsync(request);
        }
        else
        {
            var request = new UpdateCurrencyRequest(Name, Symbol, FractionName ?? string.Empty, (byte)DecimalPlaces);
            result = await _currencyService.UpdateAsync(_currencyDto.Id, request);
        }

        if (result.IsSuccess)
        {
            var id = result.Value?.Id ?? 0;
            _eventBus.Publish(new CurrencyChangedMessage(id));
            _toastService.ShowSuccess(_currencyDto == null ? "تم إضافة العملة بنجاح" : "تم تعديل العملة بنجاح");
            _soundService.PlaySuccess();
            RequestClose();
        }
        else
        {
            var errorMsg = HandleFailure(result.Error ?? "فشل في حفظ العملة", "CurrencyEditorViewModel.SaveAsync");
            ErrorMessage = errorMsg;
            await _dialogService.ShowErrorAsync("خطأ في حفظ العملة", errorMsg);
            _soundService.PlayError();
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم العملة مطلوب");

        if (string.IsNullOrWhiteSpace(Code))
            AddError(nameof(Code), "رمز العملة (ISO) مطلوب");
        else if (Code.Trim().Length != 3)
            AddError(nameof(Code), "رمز ISO يجب أن يكون ثلاثي الأحرف — مثال: USD");

        if (string.IsNullOrWhiteSpace(Symbol))
            AddError(nameof(Symbol), "رمز العملة مطلوب");

        if (DecimalPlaces < 0 || DecimalPlaces > 4)
            AddError(nameof(DecimalPlaces), "عدد المنازل العشرية يجب أن يكون بين 0 و 4");

        return await ValidateAllAsync();
    }

    #endregion
}
