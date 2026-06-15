using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Party;

public class PartyEditorViewModel : ViewModelBase
{
    private readonly IPartyApiService _partyService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int _partyId;
    private string _name = string.Empty;
    private string? _phone;
    private string? _email;
    private string? _address;
    private string? _taxNumber;
    private string? _notes;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;

    public PartyEditorViewModel()
    {
        _partyService = App.GetService<IPartyApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الشخص...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public string Title => IsEditMode ? "تعديل شخص" : "إضافة شخص جديد";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "الاسم مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public string? Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string? TaxNumber
    {
        get => _taxNumber;
        set => SetProperty(ref _taxNumber, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public void LoadParty(PartyDto party)
    {
        _partyId = party.Id;
        _name = party.Name;
        _phone = party.Phone;
        _email = party.Email;
        _address = party.Address;
        _taxNumber = party.TaxNumber;
        _notes = party.Notes;
        _isActive = party.IsActive;
        _isEditMode = true;
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "الاسم مطلوب");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        if (IsEditMode)
        {
            var request = new UpdatePartyRequest(
                Name: Name,
                Phone: Phone,
                Email: Email,
                Address: Address,
                TaxNumber: TaxNumber,
                Notes: Notes);

            var result = await _partyService.UpdateAsync(_partyId, request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم تحديث الشخص بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث الشخص", "PartyEditorViewModel.SaveAsync");
            }
        }
        else
        {
            var request = new CreatePartyRequest(
                Name: Name,
                Phone: Phone,
                Email: Email,
                Address: Address,
                TaxNumber: TaxNumber,
                Notes: Notes);

            var result = await _partyService.CreateAsync(request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم إضافة الشخص بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إضافة الشخص", "PartyEditorViewModel.SaveAsync");
            }
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    #endregion
}
