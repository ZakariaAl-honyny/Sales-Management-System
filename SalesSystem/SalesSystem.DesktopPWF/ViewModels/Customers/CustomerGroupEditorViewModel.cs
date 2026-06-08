using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Collections.Generic;

namespace SalesSystem.DesktopPWF.ViewModels.Customers;

public class CustomerGroupEditorViewModel : ViewModelBase
{
    private readonly ICustomerGroupApiService _groupService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private string _name = string.Empty;
    private string? _description;
    private string? _errorMessage;
    private string _windowTitle = "إضافة مجموعة جديدة";

    public CustomerGroupEditorViewModel()
    {
        _groupService = App.GetService<ICustomerGroupApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        SetDialogService(_dialogService);
        InitializeCommands();
    }

    private CustomerGroupDto? _groupDto;

    public void LoadGroup(CustomerGroupDto group)
    {
        _groupDto = group;
        Name = group.Name;
        Description = group.Description;
        WindowTitle = $"تعديل مجموعة: {group.Name}";
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ مجموعة العملاء...")));
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
                    AddError(nameof(Name), "اسم المجموعة مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public string? Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                if (value?.Length > 250)
                    AddError(nameof(Description), "الوصف لا يمكن أن يتجاوز 250 حرف");
                else
                    ClearErrors(nameof(Description));
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

        Result<CustomerGroupDto> result;
        if (_groupDto == null)
        {
            var request = new CreateCustomerGroupRequest(Name, Description);
            result = await _groupService.CreateAsync(request);
        }
        else
        {
            var request = new UpdateCustomerGroupRequest(Name, Description, _groupDto.IsActive);
            result = await _groupService.UpdateAsync(_groupDto.Id, request);
        }

        if (result.IsSuccess && result.Value != null)
        {
            _eventBus.Publish(new GroupChangedMessage(result.Value.Id));
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المجموعة", "CustomerGroupEditorViewModel.SaveAsync");
            await _dialogService.ShowErrorAsync("خطأ في حفظ المجموعة", ErrorMessage!);
        }
    }

    private async Task<bool> ValidateAsync()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("• اسم المجموعة مطلوب");
        if (Description?.Length > 250)
            errors.Add("• الوصف لا يمكن أن يتجاوز 250 حرف");

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
