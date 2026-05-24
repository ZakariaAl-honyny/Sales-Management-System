using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class ProductUnitsListViewModel : ViewModelBase
{
    private readonly IProductUnitApiService _unitService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<ProductUnitDto> _units = new();
    private ProductUnitDto? _selectedUnit;
    private string? _errorMessage;
    private bool _isEmpty;
    private int _productId;

    public ProductUnitsListViewModel()
        : this(
            App.GetService<IProductUnitApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IScreenWindowService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public ProductUnitsListViewModel(
        IProductUnitApiService unitService,
        IDialogService dialogService,
        IEventBus eventBus,
        IScreenWindowService screenWindowService,
        IToastNotificationService? toastService = null)
    {
        _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadUnitsAsync);
        AddCommand = new RelayCommand(AddUnit);
        EditCommand = new RelayCommand(EditUnit, () => SelectedUnit != null);
        DeleteCommand = new AsyncRelayCommand(DeleteUnitAsync, () => SelectedUnit != null);

        _eventBus.Subscribe<ProductChangedMessage>(OnProductChanged);
    }

    #region Properties

    public ObservableCollection<ProductUnitDto> Units
    {
        get => _units;
        set => SetProperty(ref _units, value);
    }

    public ProductUnitDto? SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (SetProperty(ref _selectedUnit, value))
            {
                (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
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

    public async Task LoadUnitsAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _unitService.GetByProductIdAsync(ProductId);

            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Units.Clear();
                    foreach (var item in result.Value.OrderBy(x => x.ConversionFactor))
                    {
                        Units.Add(item);
                    }
                    IsEmpty = Units.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل وحدات القياس", "ProductUnitsListViewModel.LoadUnitsAsync", "[ProductUnitsListViewModel.LoadUnitsAsync] Failed to load product units from API.");
                IsEmpty = Units.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "ProductUnitsListViewModel.LoadUnitsAsync", "[ProductUnitsListViewModel.LoadUnitsAsync] Failed to load product units from API.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AddUnit()
    {
        var editorVm = new ProductUnitEditorViewModel(ProductId);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "إضافة وحدة قياس جديدة",
            OnClosed = (vm) =>
            {
                if (vm is ProductUnitEditorViewModel editor && editor.UnitId.HasValue)
                {
                    _eventBus.Publish(new ProductChangedMessage(ProductId));
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadUnitsAsync());
                }
            }
        });
    }

    private void EditUnit()
    {
        if (SelectedUnit == null) return;

        var editorVm = new ProductUnitEditorViewModel(ProductId, SelectedUnit.Id);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل وحدة القياس",
            OnClosed = (vm) =>
            {
                _eventBus.Publish(new ProductChangedMessage(ProductId));
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadUnitsAsync());
            }
        });
    }

    private async Task DeleteUnitAsync()
    {
        if (SelectedUnit == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"الوحدة: {SelectedUnit.UnitName}");

        if (strategy == DeleteStrategy.Cancel) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var deleteResult = await _unitService.DeleteUnitAsync(ProductId, SelectedUnit.Id, strategy);

            if (deleteResult.IsSuccess)
            {
                _eventBus.Publish(new ProductChangedMessage(ProductId));
                await LoadUnitsAsync();
                _toastService.ShowSuccess("تم حذف الوحدة بنجاح");
            }
            else
            {
                var error = deleteResult.Error ?? "فشل في حذف الوحدة";
                ErrorMessage = error;
                _toastService.ShowError(error);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "ProductUnitsListViewModel.DeleteUnitAsync", $"[ProductUnitsListViewModel.DeleteUnitAsync] Failed to delete unit with ID {SelectedUnit.Id}.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnProductChanged(ProductChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadUnitsAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<ProductChangedMessage>(OnProductChanged);
    }

    #endregion
}
