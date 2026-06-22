using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Suppliers;

public class SupplierContactListViewModel : ViewModelBase
{
    private readonly ISupplierContactApiService _contactService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<SupplierContactDto> _contacts = new();
    private SupplierContactDto? _selectedContact;
    private string? _errorMessage;
    private bool _isEmpty;
    private int _supplierId;
    private string _supplierName = string.Empty;
    private bool _includeInactive;

    public SupplierContactListViewModel()
    {
        _contactService = App.GetService<ISupplierContactApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadContactsOperationAsync,
                ex => ErrorMessage = HandleException(ex, "SupplierContactListViewModel.LoadContactsAsync"))));
        AddCommand = new RelayCommand(AddContact);
        EditCommand = new RelayCommand(EditContact);
        DeleteCommand = new AsyncRelayCommand(DeleteContactAsync);
    }

    #region Properties

    public ObservableCollection<SupplierContactDto> Contacts
    {
        get => _contacts;
        set => SetProperty(ref _contacts, value);
    }

    public SupplierContactDto? SelectedContact
    {
        get => _selectedContact;
        set => SetProperty(ref _selectedContact, value);
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

    public int SupplierId
    {
        get => _supplierId;
        set => SetProperty(ref _supplierId, value);
    }

    public string SupplierName
    {
        get => _supplierName;
        set => SetProperty(ref _supplierName, value);
    }

    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (SetProperty(ref _includeInactive, value))
            {
                _ = LoadContactsAsync();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public void LoadContacts(int supplierId, string supplierName)
    {
        SupplierId = supplierId;
        SupplierName = supplierName;
        _ = LoadContactsAsync();
    }

    public async Task LoadContactsAsync()
    {
        await ExecuteAsync(LoadContactsOperationAsync,
            ex => ErrorMessage = HandleException(ex, "SupplierContactListViewModel.LoadContactsAsync"));
    }

    private async Task LoadContactsOperationAsync()
    {
        ErrorMessage = null;

        var result = await _contactService.GetAllAsync(SupplierId);

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Contacts.Clear();
                var items = IncludeInactive
                    ? result.Value
                    : result.Value.Where(x => x.IsActive);
                foreach (var item in items.OrderByDescending(x => x.Id))
                {
                    Contacts.Add(item);
                }
                IsEmpty = Contacts.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل جهات الاتصال", "SupplierContactListViewModel.LoadContactsAsync");
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
            IsEmpty = Contacts.Count == 0;
        }
    }

    private void AddContact()
    {
        if (SupplierId <= 0) return;

        var editorVm = App.GetService<SupplierContactEditorViewModel>();
        editorVm.InitializeForSupplier(SupplierId, SupplierName);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "إضافة جهة اتصال",
            Width = 550,
            Height = 550,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadContactsAsync());
            }
        });
    }

    private void EditContact()
    {
        if (SelectedContact == null) return;

        var editorVm = App.GetService<SupplierContactEditorViewModel>();
        editorVm.LoadContact(SelectedContact);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل جهة اتصال",
            Width = 550,
            Height = 550,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadContactsAsync());
            }
        });
    }

    public async Task DeleteContactAsync()
    {
        if (SelectedContact == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"جهة الاتصال: {SelectedContact.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        var contact = SelectedContact;
        await ExecuteAsync(() => DeleteContactOperationAsync(strategy, contact),
            ex => ErrorMessage = HandleException(ex, "SupplierContactListViewModel.DeleteContactAsync"));
    }

    private async Task DeleteContactOperationAsync(DeleteStrategy strategy, SupplierContactDto contact)
    {
        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var result = await _contactService.DeactivateAsync(contact.Id);
            if (result.IsSuccess)
            {
                await LoadContactsAsync();
                _toastService.ShowSuccess("تم إلغاء تنشيط جهة الاتصال بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء تنشيط جهة الاتصال", "SupplierContactListViewModel.DeleteContactAsync");
                await _dialogService.ShowErrorAsync("خطأ في إلغاء تنشيط جهة الاتصال", ErrorMessage!);
            }
        }
        else if (strategy == DeleteStrategy.Permanent)
        {
            await _dialogService.ShowErrorAsync("خطأ", "لا يمكن حذف جهة الاتصال بشكل نهائي. يمكنك إلغاء تنشيطها فقط.");
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}
