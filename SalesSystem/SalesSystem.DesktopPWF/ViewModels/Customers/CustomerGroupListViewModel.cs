using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Customers;

public class CustomerGroupListViewModel : ViewModelBase
{
    private readonly ICustomerGroupApiService _groupService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<CustomerGroupItemViewModel> _groups = new();
    private CustomerGroupItemViewModel? _selectedGroup;
    private string? _errorMessage;
    private bool _isEmpty;

    public CustomerGroupListViewModel()
    {
        _groupService = App.GetService<ICustomerGroupApiService>();
        _dialogService = App.GetService<IDialogService>();
        _eventBus = App.GetService<IEventBus>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        _toastService = App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadGroupsAsync);
        AddCommand = new RelayCommand(AddGroup);
        EditCommand = new RelayCommand(EditGroup, () => SelectedGroup != null);
        DeleteCommand = new AsyncRelayCommand(DeleteGroupAsync, () => SelectedGroup != null);

        // Subscribe to group changes
        _eventBus.Subscribe<GroupChangedMessage>(OnGroupChanged);
    }

    #region Properties

    public ObservableCollection<CustomerGroupItemViewModel> Groups
    {
        get => _groups;
        set => SetProperty(ref _groups, value);
    }

    public CustomerGroupItemViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetProperty(ref _selectedGroup, value))
            {
                (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
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

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadGroupsAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _groupService.GetAllAsync();

            if (result.IsSuccess && result.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Groups.Clear();
                    foreach (var dto in result.Value.OrderByDescending(x => x.Id))
                    {
                        Groups.Add(new CustomerGroupItemViewModel(dto));
                    }
                    IsEmpty = Groups.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل مجموعات العملاء", "CustomerGroupListViewModel.LoadGroupsAsync", "[CustomerGroupListViewModel.LoadGroupsAsync] Failed to load customer groups.");
                IsEmpty = Groups.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "CustomerGroupListViewModel.LoadGroupsAsync", "[CustomerGroupListViewModel.LoadGroupsAsync] Failed to load customer groups.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AddGroup()
    {
        var editorVm = App.GetService<CustomerGroupEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "مجموعة عملاء جديدة",
            Width = 500,
            Height = 400,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadGroupsAsync());
            }
        });
    }

    private void EditGroup()
    {
        if (SelectedGroup == null) return;

        var editorVm = App.GetService<CustomerGroupEditorViewModel>();
        var dto = new CustomerGroupDto(SelectedGroup.Id, SelectedGroup.Name, SelectedGroup.Description, SelectedGroup.IsActive);
        editorVm.LoadGroup(dto);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل مجموعة عملاء",
            Width = 500,
            Height = 400,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadGroupsAsync());
            }
        });
    }

    public async Task DeleteGroupAsync()
    {
        if (SelectedGroup == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"مجموعة العملاء: {SelectedGroup.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (strategy == DeleteStrategy.Deactivate)
            {
                var deleteResult = await _groupService.DeleteAsync(SelectedGroup.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadGroupsAsync();
                    _toastService.ShowSuccess($"تم إلغاء تنشيط المجموعة {SelectedGroup.Name} بنجاح");
                }
                else
                {
                    ErrorMessage = deleteResult.Error ?? "فشل في إلغاء تنشيط المجموعة";
                    await _dialogService.ShowErrorAsync("خطأ في الحذف", ErrorMessage);
                }
            }
            else if (strategy == DeleteStrategy.Permanent)
            {
                // CustomerGroupApiService only supports soft delete via DeleteAsync
                var deleteResult = await _groupService.DeleteAsync(SelectedGroup.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadGroupsAsync();
                    _toastService.ShowSuccess($"تم حذف المجموعة {SelectedGroup.Name} بنجاح");
                }
                else
                {
                    ErrorMessage = deleteResult.Error ?? "فشل في حذف المجموعة";
                    await _dialogService.ShowErrorAsync("خطأ في الحذف", ErrorMessage);
                    LogSystemError($"Delete failed for CustomerGroup {SelectedGroup.Id}: {ErrorMessage}", "CustomerGroupListViewModel.DeleteGroupAsync");
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "حدث خطأ غير متوقع أثناء الحذف";
            HandleException(ex, "CustomerGroupListViewModel.DeleteGroupAsync", $"[CustomerGroupListViewModel.DeleteGroupAsync] Failed to delete customer group with ID {SelectedGroup?.Id}.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnGroupChanged(GroupChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadGroupsAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<GroupChangedMessage>(OnGroupChanged);
    }

    #endregion

    #region Inner ViewModel

    /// <summary>
    /// ViewModel for each customer group item displayed in the list.
    /// </summary>
    public class CustomerGroupItemViewModel : ViewModelBase
    {
        public int Id { get; }
        public string Name { get; }
        public string? Description { get; }
        public bool IsActive { get; }
        public string StatusText => IsActive ? "نشط" : "غير نشط";

        public CustomerGroupItemViewModel(CustomerGroupDto dto)
        {
            Id = dto.Id;
            Name = dto.Name;
            Description = dto.Description;
            IsActive = dto.IsActive;
        }
    }

    #endregion
}
