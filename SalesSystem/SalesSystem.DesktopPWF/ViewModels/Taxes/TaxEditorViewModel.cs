using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Collections.Generic;

namespace SalesSystem.DesktopPWF.ViewModels.Taxes;

public class TaxEditorViewModel : ViewModelBase
{
    private readonly ITaxesApiService _taxesService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private string _name = string.Empty;
    private string _code = string.Empty;
    private decimal _rate;
    private byte _taxType = 1;
    private bool _isDefault;
    private string? _errorMessage;
    private string _windowTitle = "إضافة ضريبة جديدة";
    private TaxDto? _taxDto;

    public TaxEditorViewModel()
    {
        _taxesService = App.GetService<ITaxesApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        SetDialogService(_dialogService);
        InitializeCommands();
    }

    public void LoadTax(TaxDto tax)
    {
        _taxDto = tax;
        Name = tax.Name;
        Code = tax.Code;
        Rate = tax.Rate;
        TaxType = tax.TaxType;
        IsDefault = tax.IsDefault;
        WindowTitle = $"تعديل ضريبة: {tax.Name}";
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الضريبة...")));
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
                    AddError(nameof(Name), "اسم الضريبة مطلوب");
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
                    AddError(nameof(Code), "رمز الضريبة مطلوب");
                else
                    ClearErrors(nameof(Code));
            }
        }
    }

    public byte TaxType
    {
        get => _taxType;
        set => SetProperty(ref _taxType, value);
    }

    public decimal Rate
    {
        get => _rate;
        set
        {
            if (SetProperty(ref _rate, value))
            {
                if (value < 0)
                    AddError(nameof(Rate), "نسبة الضريبة لا يمكن أن تكون سالبة");
                else if (value > 100)
                    AddError(nameof(Rate), "نسبة الضريبة يجب أن تكون 100% أو أقل");
                else
                    ClearErrors(nameof(Rate));
            }
        }
    }

    public bool IsDefault
    {
        get => _isDefault;
        set => SetProperty(ref _isDefault, value);
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

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        Result<TaxDto> result;
        if (_taxDto == null)
        {
            var request = new CreateTaxRequest(Name, Code, Rate, TaxType, IsDefault);
            result = await _taxesService.CreateAsync(request);
        }
        else
        {
            var request = new UpdateTaxRequest(Name, Code, Rate, TaxType, IsDefault, _taxDto.IsActive);
            result = await _taxesService.UpdateAsync(_taxDto.Id, request);
        }

        if (result.IsSuccess && result.Value != null)
        {
            _eventBus.Publish(new TaxChangedMessage(result.Value.Id));
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ الضريبة", "TaxEditorViewModel.SaveAsync");
            await _dialogService.ShowErrorAsync("خطأ في حفظ الضريبة", ErrorMessage!);
        }
    }

    private async Task<bool> ValidateAsync()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("• اسم الضريبة مطلوب");
        if (Rate < 0)
            errors.Add("• نسبة الضريبة لا يمكن أن تكون سالبة");
        if (Rate > 100)
            errors.Add("• نسبة الضريبة يجب أن تكون 100% أو أقل");

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
