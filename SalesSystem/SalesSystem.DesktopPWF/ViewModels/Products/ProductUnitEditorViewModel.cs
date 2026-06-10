using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

/// <summary>
/// ViewModel for ProductUnit Editor — allows adding/editing a unit of measure linked to a product.
/// Phase 25: Uses UnitId dropdown (from Units table) instead of free-text UnitName.
/// Pricing is managed separately via ProductPrices entity.
/// </summary>
public class ProductUnitEditorViewModel : ViewModelBase
{
    private readonly IProductUnitApiService _unitService;
    private readonly IUnitApiService _lookupService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private int _productId;
    private int? _unitId;
    private int? _selectedUnitLookupId;
    private decimal _conversionFactor = 1;
    private bool _isBaseUnit;
    private bool _isEditMode;
    private string? _errorMessage;
    private ObservableCollection<UnitDto> _availableUnits = new();

    public ProductUnitEditorViewModel()
        : this(
            App.GetService<IProductUnitApiService>(),
            App.GetService<IUnitApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public ProductUnitEditorViewModel(
        IProductUnitApiService unitService,
        IUnitApiService lookupService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService? toastService = null)
    {
        _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
        _lookupService = lookupService ?? throw new ArgumentNullException(nameof(lookupService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadAvailableUnitsAsync();
    }

    public ProductUnitEditorViewModel(int productId)
        : this()
    {
        _productId = productId;
    }

    public ProductUnitEditorViewModel(int productId, int unitId)
        : this()
    {
        _productId = productId;
        _unitId = unitId;
        _isEditMode = true;
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الوحدة...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
    }

    public int? UnitId
    {
        get => _unitId;
        private set => SetProperty(ref _unitId, value);
    }

    /// <summary>
    /// Selected Unit from the lookup dropdown (Units table).
    /// </summary>
    public int? SelectedUnitLookupId
    {
        get => _selectedUnitLookupId;
        set
        {
            if (SetProperty(ref _selectedUnitLookupId, value))
            {
                if (!value.HasValue || value.Value <= 0)
                    AddError(nameof(SelectedUnitLookupId), "يجب اختيار وحدة قياس");
                else
                    ClearErrors(nameof(SelectedUnitLookupId));
            }
        }
    }

    public decimal ConversionFactor
    {
        get => _conversionFactor;
        set
        {
            if (SetProperty(ref _conversionFactor, value))
            {
                if (value <= 0)
                    AddError(nameof(ConversionFactor), "عامل التحويل يجب أن يكون أكبر من صفر");
                else
                    ClearErrors(nameof(ConversionFactor));
            }
        }
    }

    public bool IsBaseUnit
    {
        get => _isBaseUnit;
        set => SetProperty(ref _isBaseUnit, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        private set => SetProperty(ref _isEditMode, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Available units from the Units table for the UnitId dropdown.
    /// </summary>
    public ObservableCollection<UnitDto> AvailableUnits
    {
        get => _availableUnits;
        set => SetProperty(ref _availableUnits, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    private async Task LoadAvailableUnitsAsync()
    {
        try
        {
            var result = await _lookupService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                await InvokeOnUIThreadAsync(async () =>
                {
                    AvailableUnits.Clear();
                    foreach (var unit in result.Value)
                    {
                        AvailableUnits.Add(unit);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل وحدات القياس", "ProductUnitEditorViewModel.LoadAvailableUnitsAsync", ex);
        }
    }

    private bool Validate()
    {
        ClearAllErrors();

        if (!SelectedUnitLookupId.HasValue || SelectedUnitLookupId.Value <= 0)
            AddError(nameof(SelectedUnitLookupId), "يجب اختيار وحدة قياس");

        if (ConversionFactor <= 0)
            AddError(nameof(ConversionFactor), "عامل التحويل يجب أن يكون أكبر من صفر");

        if (HasErrors)
        {
            _ = _dialogService.ShowWarningAsync("بيانات غير مكتملة", "يرجى إكمال البيانات الإلزامية التالية:\n\n" +
                string.Join("\n", GetErrors(string.Empty).Cast<string>()));
            return false;
        }

        return true;
    }

    private async Task SaveOperationAsync()
    {
        if (!Validate()) return;

        if (IsEditMode && UnitId.HasValue)
        {
            // Update: only UnitId is updateable per new UpdateProductUnitRequest
            var request = new UpdateProductUnitRequest(SelectedUnitLookupId!.Value);
            var result = await _unitService.UpdateUnitAsync(ProductId, UnitId.Value, request);

            if (result.IsSuccess)
            {
                _eventBus.Publish(new ProductChangedMessage(ProductId));
                _toastService.ShowSuccess("تم تعديل الوحدة بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في تحديث الوحدة";
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", ErrorMessage);
            }
        }
        else
        {
            var request = new AddProductUnitRequest(
                UnitId: SelectedUnitLookupId!.Value,
                ConversionFactor: ConversionFactor,
                IsBaseUnit: IsBaseUnit);
            var result = await _unitService.AddUnitAsync(ProductId, request);

            if (result.IsSuccess)
            {
                UnitId = result.Value?.Id;
                _eventBus.Publish(new ProductChangedMessage(ProductId));
                _toastService.ShowSuccess("تم إضافة الوحدة بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في إضافة الوحدة";
                await _dialogService.ShowErrorAsync("خطأ في الحفظ", ErrorMessage);
            }
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    #endregion
}
