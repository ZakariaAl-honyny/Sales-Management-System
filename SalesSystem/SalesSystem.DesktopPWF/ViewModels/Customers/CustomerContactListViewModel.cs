using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Customers;

public class CustomerContactListViewModel : ViewModelBase
{
    private readonly ICustomerContactApiService _contactService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<CustomerContactDto> _contacts = new();
    private CustomerContactDto? _selectedContact;
    private string? _errorMessage;
    private bool _isEmpty;
    private int _customerId;
    private string _customerName = string.Empty;

    public CustomerContactListViewModel()
    {
        _contactService = App.GetService<ICustomerContactApiService>();
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
                ex => ErrorMessage = HandleException(ex, "CustomerContactListViewModel.LoadContactsAsync"))));
        AddCommand = new RelayCommand(AddContact);
        EditCommand = new RelayCommand(EditContact);
        DeleteCommand = new AsyncRelayCommand(DeleteContactAsync);
    }

    #region Properties

    public ObservableCollection<CustomerContactDto> Contacts
    {
        get => _contacts;
        set => SetProperty(ref _contacts, value);
    }

    public CustomerContactDto? SelectedContact
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

    public int CustomerId
    {
        get => _customerId;
        set => SetProperty(ref _customerId, value);
    }

    public string CustomerName
    {
        get => _customerName;
        set => SetProperty(ref _customerName, value);
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public void LoadContacts(int customerId, string customerName)
    {
        CustomerId = customerId;
        CustomerName = customerName;
        _ = LoadContactsAsync();
    }

    public async Task LoadContactsAsync()
    {
        await ExecuteAsync(LoadContactsOperationAsync,
            ex => ErrorMessage = HandleException(ex, "CustomerContactListViewModel.LoadContactsAsync"));
    }

    private async Task LoadContactsOperationAsync()
    {
        ErrorMessage = null;

        var result = await _contactService.GetAllAsync(CustomerId);

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Contacts.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Contacts.Add(item);
                }
                IsEmpty = Contacts.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل جهات الاتصال", "CustomerContactListViewModel.LoadContactsAsync");
            IsEmpty = Contacts.Count == 0;
        }
    }

    private void AddContact()
    {
        if (CustomerId <= 0) return;

        var editorVm = App.GetService<CustomerContactEditorViewModel>();
        editorVm.InitializeForCustomer(CustomerId, CustomerName);

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

        var editorVm = App.GetService<CustomerContactEditorViewModel>();
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
            ex => ErrorMessage = HandleException(ex, "CustomerContactListViewModel.DeleteContactAsync"));
    }

    private async Task DeleteContactOperationAsync(DeleteStrategy strategy, CustomerContactDto contact)
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
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء تنشيط جهة الاتصال", "CustomerContactListViewModel.DeleteContactAsync");
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
