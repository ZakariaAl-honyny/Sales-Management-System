using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Suppliers;

public class SupplierContactEditorViewModel : ViewModelBase
{
    private readonly ISupplierContactApiService _contactService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int _contactId;
    private int _supplierId;
    private string _supplierName = string.Empty;
    private string _name = string.Empty;
    private string? _phone;
    private string? _email;
    private string? _position;
    private string? _notes;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;

    public SupplierContactEditorViewModel()
    {
        _contactService = App.GetService<ISupplierContactApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ جهة الاتصال...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public string Title => IsEditMode ? "تعديل جهة اتصال" : "إضافة جهة اتصال جديدة";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public string SupplierName
    {
        get => _supplierName;
        set => SetProperty(ref _supplierName, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "اسم جهة الاتصال مطلوب");
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

    public string? Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
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

    public void InitializeForSupplier(int supplierId, string supplierName)
    {
        _supplierId = supplierId;
        _supplierName = supplierName;
        _isEditMode = false;
    }

    public void LoadContact(SupplierContactDto contact)
    {
        _contactId = contact.Id;
        _supplierId = contact.SupplierId;
        _supplierName = contact.SupplierName ?? string.Empty;
        _name = contact.Name;
        _phone = contact.Phone;
        _email = contact.Email;
        _position = contact.Position;
        _notes = contact.Notes;
        _isActive = contact.IsActive;
        _isEditMode = true;
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم جهة الاتصال مطلوب");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        if (IsEditMode)
        {
            var request = new UpdateSupplierContactRequest(
                Name,
                string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                string.IsNullOrWhiteSpace(Email) ? null : Email,
                string.IsNullOrWhiteSpace(Position) ? null : Position,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes);

            var result = await _contactService.UpdateAsync(_contactId, request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم تحديث جهة الاتصال بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث جهة الاتصال", "SupplierContactEditorViewModel.SaveAsync");
            }
        }
        else
        {
            var request = new CreateSupplierContactRequest(
                _supplierId,
                Name,
                string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                string.IsNullOrWhiteSpace(Email) ? null : Email,
                string.IsNullOrWhiteSpace(Position) ? null : Position,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes);

            var result = await _contactService.CreateAsync(request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم إضافة جهة الاتصال بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إضافة جهة الاتصال", "SupplierContactEditorViewModel.SaveAsync");
            }
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    #endregion
}
