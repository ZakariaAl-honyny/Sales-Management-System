using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.InventoryAdjustment;

/// <summary>
/// ViewModel for Inventory Adjustments List View
/// </summary>
public class InventoryAdjustmentListViewModel : ViewModelBase
{
    private readonly IInventoryAdjustmentApiService _adjustmentService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<InventoryAdjustmentDto> _adjustments = new();
    private ICollectionView? _adjustmentsView;
    private InventoryAdjustmentDto? _selectedAdjustment;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;

    public InventoryAdjustmentListViewModel()
    {
        _adjustmentService = App.GetService<IInventoryAdjustmentApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();

        InitializeCommands();
    }

    public InventoryAdjustmentListViewModel(
        IInventoryAdjustmentApiService adjustmentService,
        IEventBus eventBus,
        IDialogService dialogService,
        IScreenWindowService screenWindowService,
        IToastNotificationService toastService)
    {
        _adjustmentService = adjustmentService ?? throw new ArgumentNullException(nameof(adjustmentService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadAdjustmentsAsync);
        AddCommand = new RelayCommand(AddAdjustment);
        EditCommand = new RelayCommand(EditAdjustment);
        PostCommand = new AsyncRelayCommand(PostAdjustmentAsync);
        CancelCommand = new AsyncRelayCommand(CancelAdjustmentAsync);
        SearchCommand = new RelayCommand(Search);

        _eventBus.Subscribe<InventoryAdjustmentChangedMessage>(OnAdjustmentChanged);
    }

    #region Properties
    public ObservableCollection<InventoryAdjustmentDto> Adjustments
    {
        get => _adjustments;
        set => SetProperty(ref _adjustments, value);
    }

    public ICollectionView? AdjustmentsView
    {
        get => _adjustmentsView;
        private set => SetProperty(ref _adjustmentsView, value);
    }

    public InventoryAdjustmentDto? SelectedAdjustment
    {
        get => _selectedAdjustment;
        set => SetProperty(ref _selectedAdjustment, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                AdjustmentsView?.Refresh();
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

    public int AdjustmentsCount => Adjustments.Count;

    public string? AdjustmentTypeName => SelectedAdjustment?.AdjustmentType switch
    {
        1 => "إضافة",
        2 => "خصم",
        3 => "تصحيح",
        _ => "غير معروف"
    };
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand PostCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;
    #endregion

    #region Methods
    public async Task LoadAdjustmentsAsync()
    {
        await ExecuteAsync(LoadAdjustmentsOperationAsync);
    }

    private async Task LoadAdjustmentsOperationAsync()
    {
        ErrorMessage = null;
        var result = await _adjustmentService.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Adjustments.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Adjustments.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Adjustments.Count == 0;
                OnPropertyChanged(nameof(AdjustmentsCount));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل التسويات", "InventoryAdjustmentListViewModel.LoadAdjustmentsAsync", "[InventoryAdjustmentListViewModel.LoadAdjustmentsAsync] Failed to load adjustments.");
            IsEmpty = Adjustments.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        AdjustmentsView = new ListCollectionView(Adjustments);
        AdjustmentsView.Filter = FilterAdjustments;
    }

    private bool FilterAdjustments(object obj)
    {
        if (obj is not InventoryAdjustmentDto adj) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            if (!adj.AdjustmentNo.ToString().Contains(searchLower) &&
                (adj.WarehouseName?.ToLower().Contains(searchLower) ?? false) == false)
                return false;
        }

        return true;
    }

    private void AddAdjustment()
    {
        var editorVm = App.GetService<InventoryAdjustmentEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تسوية مخزون جديدة",
            Width = 900,
            Height = 700,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadAdjustmentsAsync());
            }
        });
    }

    private void EditAdjustment()
    {
        if (SelectedAdjustment == null)
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار تسوية");
            return;
        }

        var editorVm = new InventoryAdjustmentEditorViewModel(SelectedAdjustment);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل تسوية المخزون",
            Width = 900,
            Height = 700,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadAdjustmentsAsync());
            }
        });
    }

    public void EditAdjustmentFromDoubleClick()
    {
        if (SelectedAdjustment != null)
            EditAdjustment();
    }

    public async Task PostAdjustmentAsync()
    {
        if (SelectedAdjustment == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار تسوية");
            return;
        }

        if (SelectedAdjustment.Status != 1)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "لا يمكن ترحيل التسوية إلا في حالة مسودة");
            return;
        }

        await ExecuteAsync(PostAdjustmentOperationAsync);
    }

    private async Task PostAdjustmentOperationAsync()
    {
        ErrorMessage = null;
        var result = await _adjustmentService.PostAsync(SelectedAdjustment!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new InventoryAdjustmentChangedMessage(SelectedAdjustment.Id));
            await LoadAdjustmentsAsync();
            _toastService.ShowSuccess("تم ترحيل التسوية بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في ترحيل التسوية", "InventoryAdjustmentListViewModel.PostAdjustmentAsync", $"[InventoryAdjustmentListViewModel.PostAdjustmentAsync] Failed to post adjustment {SelectedAdjustment.Id}.");
        }
    }

    public async Task CancelAdjustmentAsync()
    {
        if (SelectedAdjustment == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار تسوية");
            return;
        }

        if (SelectedAdjustment.Status != 1)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "لا يمكن إلغاء التسوية إلا في حالة مسودة");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء", "هل أنت متأكد من إلغاء التسوية؟");
        if (!confirmed) return;

        await ExecuteAsync(CancelAdjustmentOperationAsync);
    }

    private async Task CancelAdjustmentOperationAsync()
    {
        ErrorMessage = null;
        var result = await _adjustmentService.CancelAsync(SelectedAdjustment!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new InventoryAdjustmentChangedMessage(SelectedAdjustment.Id));
            await LoadAdjustmentsAsync();
            _toastService.ShowSuccess("تم إلغاء التسوية بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء التسوية", "InventoryAdjustmentListViewModel.CancelAdjustmentAsync", $"[InventoryAdjustmentListViewModel.CancelAdjustmentAsync] Failed to cancel adjustment {SelectedAdjustment.Id}.");
        }
    }

    private void Search()
    {
        AdjustmentsView?.Refresh();
    }

    private void OnAdjustmentChanged(InventoryAdjustmentChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () => await LoadAdjustmentsAsync());
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<InventoryAdjustmentChangedMessage>(OnAdjustmentChanged);
    }
    #endregion
}
