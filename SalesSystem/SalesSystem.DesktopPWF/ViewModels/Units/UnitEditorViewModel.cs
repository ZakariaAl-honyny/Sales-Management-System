using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Collections.Generic;

namespace SalesSystem.DesktopPWF.ViewModels.Units;

public class UnitEditorViewModel : ViewModelBase
{
    private readonly IUnitApiService _unitService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private string _name = string.Empty;
    private string? _errorMessage;
    private string _windowTitle = "إضافة وحدة جديدة";

    public UnitEditorViewModel()
    {
        _unitService = App.GetService<IUnitApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        SetDialogService(_dialogService);
        InitializeCommands();
    }

    public void LoadUnit(UnitDto unit)
    {
        _unitDto = unit;
        Name = unit.Name;
        WindowTitle = $"تعديل وحدة: {unit.Name}";
    }

    private UnitDto? _unitDto;

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الوحدة...")));
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
                    AddError(nameof(Name), "اسم الوحدة مطلوب");
                else
                    ClearErrors(nameof(Name));
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

        Result<UnitDto> result;
        if (_unitDto == null)
        {
            var request = new CreateUnitRequest(Name, null);
            result = await _unitService.CreateAsync(request);
        }
        else
        {
            var request = new UpdateUnitRequest(Name, _unitDto.Symbol, _unitDto.IsActive);
            result = await _unitService.UpdateAsync(_unitDto.Id, request);
        }

        if (result.IsSuccess && result.Value != null)
        {
            _eventBus.Publish(new UnitChangedMessage(result.Value.Id));
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ الوحدة", "UnitEditorViewModel.SaveAsync");
            await _dialogService.ShowErrorAsync("خطأ في حفظ الوحدة", ErrorMessage!);
        }
    }

    private async Task<bool> ValidateAsync()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("• اسم الوحدة مطلوب");

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
