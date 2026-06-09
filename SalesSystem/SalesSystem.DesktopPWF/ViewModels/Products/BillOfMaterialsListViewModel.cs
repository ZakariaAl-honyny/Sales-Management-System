using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class BillOfMaterialsListViewModel : ViewModelBase, IDisposable
{
    private readonly IBillOfMaterialApiService _bomService;
    private readonly IProductApiService _productService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<BillOfMaterialDto> _items = new();
    private BillOfMaterialDto? _selectedItem;
    private string? _errorMessage;
    private bool _isEmpty;
    private string _searchText = string.Empty;

    public BillOfMaterialsListViewModel()
        : this(
            App.GetService<IBillOfMaterialApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IScreenWindowService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public BillOfMaterialsListViewModel(
        IBillOfMaterialApiService bomService,
        IProductApiService productService,
        IDialogService dialogService,
        IEventBus eventBus,
        IScreenWindowService screenWindowService,
        IToastNotificationService? toastService = null)
    {
        _bomService = bomService ?? throw new ArgumentNullException(nameof(bomService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _eventBus.Subscribe<BillOfMaterialChangedMessage>(OnBomChanged);
        _ = LoadDataAsync();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        AddCommand = new RelayCommand(AddBom);
        EditCommand = new RelayCommand(EditBom);
        DeleteCommand = new AsyncRelayCommand(DeleteBomAsync);
        ProduceCommand = new RelayCommand(OpenProduce);
    }

    #region Properties

    public ObservableCollection<BillOfMaterialDto> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public BillOfMaterialDto? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _ = FilterItemsAsync();
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
    public ICommand ProduceCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadDataAsync()
    {
        await ExecuteAsync(LoadDataOperationAsync);
    }

    private async Task LoadDataOperationAsync()
    {
        ErrorMessage = null;
        var result = await _bomService.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Items.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Items.Add(item);
                }
                IsEmpty = Items.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل قائمة المكونات",
                "BillOfMaterialsListViewModel.LoadDataOperationAsync",
                "[BillOfMaterialsListViewModel.LoadDataOperationAsync] Failed to load BOM list from API.");
            IsEmpty = Items.Count == 0;
        }
    }

    private async Task FilterItemsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadDataAsync();
            return;
        }

        await ExecuteAsync(() => FilterOperationAsync());
    }

    private async Task FilterOperationAsync()
    {
        ErrorMessage = null;
        var result = await _bomService.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            var filtered = result.Value
                .Where(x =>
                    x.AssemblyProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    x.ComponentProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Id)
                .ToList();

            InvokeOnUIThread(() =>
            {
                Items.Clear();
                foreach (var item in filtered)
                {
                    Items.Add(item);
                }
                IsEmpty = Items.Count == 0;
            });
        }
    }

    private void AddBom()
    {
        var editorVm = App.GetService<BillOfMaterialEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "إضافة مكون جديد",
            Width = 550,
            Height = 500,
            OnClosed = (vm) =>
            {
                if (vm is BillOfMaterialEditorViewModel editor && editor.BomId.HasValue)
                {
                    _eventBus.Publish(new BillOfMaterialChangedMessage(editor.BomId.Value));
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadDataAsync());
                }
            }
        });
    }

    private void EditBom()
    {
        if (SelectedItem == null) return;

        var editorVm = new BillOfMaterialEditorViewModel(SelectedItem);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل المكون",
            Width = 550,
            Height = 500,
            OnClosed = (vm) =>
            {
                _eventBus.Publish(new BillOfMaterialChangedMessage(SelectedItem.Id));
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadDataAsync());
            }
        });
    }

    private async Task DeleteBomAsync()
    {
        if (SelectedItem == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync(
            $"المكون: {SelectedItem.ComponentProductName} للمنتج {SelectedItem.AssemblyProductName}");
        if (strategy == DeleteStrategy.Cancel) return;

        var bomId = SelectedItem.Id;
        await ExecuteAsync(() => DeleteBomOperationAsync(bomId));
    }

    private async Task DeleteBomOperationAsync(int bomId)
    {
        ErrorMessage = null;
        var result = await _bomService.DeleteAsync(bomId);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new BillOfMaterialChangedMessage(bomId));
            await LoadDataAsync();
            _toastService.ShowSuccess("تم حذف المكون بنجاح");
        }
        else
        {
            var error = result.Error ?? "فشل في حذف المكون";
            ErrorMessage = HandleFailure(error,
                "BillOfMaterialsListViewModel.DeleteBomOperationAsync",
                "[BillOfMaterialsListViewModel.DeleteBomOperationAsync] Failed to delete BOM.");
            _toastService.ShowError(ErrorMessage!);
        }
    }

    private void OpenProduce()
    {
        if (SelectedItem == null) return;

        var produceVm = new AssemblyProductionViewModel();
        // Pre-select the assembly product from the selected BOM
        produceVm.AssemblyProductId = SelectedItem.AssemblyProductId;
        produceVm.AssemblyProductName = SelectedItem.AssemblyProductName;

        _screenWindowService.OpenScreen(produceVm, new ScreenWindowOptions
        {
            Title = $"إنتاج: {SelectedItem.AssemblyProductName}",
            Width = 500,
            Height = 550,
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadDataAsync());
            }
        });
    }

    private void OnBomChanged(BillOfMaterialChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadDataAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<BillOfMaterialChangedMessage>(OnBomChanged);
        base.Cleanup();
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    #endregion
}
