using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class ProductUnitEditorViewModel : ViewModelBase
{
    private readonly IProductUnitApiService _unitService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private int _productId;
    private int? _unitId;
    private string _unitName = string.Empty;
    private decimal _conversionFactor = 1;
    private decimal _retailPrice;
    private decimal _wholesalePrice;
    private bool _isBaseUnit;
    private bool _isEditMode;
    private string? _errorMessage;
    private ObservableCollection<string> _barcodes = new();
    private string _newBarcode = string.Empty;

    public ProductUnitEditorViewModel()
        : this(
            App.GetService<IProductUnitApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public ProductUnitEditorViewModel(
        IProductUnitApiService unitService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService? toastService = null)
    {
        _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
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
        AddBarcodeCommand = new RelayCommand(AddBarcode);
        RemoveBarcodeCommand = new RelayCommand(RemoveBarcode, () => SelectedBarcode != null);
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

    public string UnitName
    {
        get => _unitName;
        set
        {
            if (SetProperty(ref _unitName, value))
            {
                ValidateField(() => !string.IsNullOrWhiteSpace(value), nameof(UnitName), "اسم الوحدة مطلوب");
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
                ValidateField(() => value > 0, nameof(ConversionFactor), "عامل التحويل يجب أن يكون أكبر من صفر");
            }
        }
    }

    public decimal RetailPrice
    {
        get => _retailPrice;
        set
        {
            if (SetProperty(ref _retailPrice, value))
            {
                ValidateField(() => value >= 0, nameof(RetailPrice), "السعر لا يمكن أن يكون سالباً");
            }
        }
    }

    public decimal WholesalePrice
    {
        get => _wholesalePrice;
        set
        {
            if (SetProperty(ref _wholesalePrice, value))
            {
                ValidateField(() => value >= 0, nameof(WholesalePrice), "السعر لا يمكن أن يكون سالباً");
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

    public ObservableCollection<string> Barcodes
    {
        get => _barcodes;
        set => SetProperty(ref _barcodes, value);
    }

    public string NewBarcode
    {
        get => _newBarcode;
        set => SetProperty(ref _newBarcode, value);
    }

    private string? _selectedBarcode;
    public string? SelectedBarcode
    {
        get => _selectedBarcode;
        set
        {
            if (SetProperty(ref _selectedBarcode, value))
            {
                (RemoveBarcodeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand AddBarcodeCommand { get; private set; } = null!;
    public ICommand RemoveBarcodeCommand { get; private set; } = null!;

    #endregion

    #region Methods

    private bool Validate()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(UnitName))
            AddError(nameof(UnitName), "اسم الوحدة مطلوب");

        if (ConversionFactor <= 0)
            AddError(nameof(ConversionFactor), "عامل التحويل يجب أن يكون أكبر من صفر");

        if (RetailPrice < 0)
            AddError(nameof(RetailPrice), "السعر لا يمكن أن يكون سالباً");

        if (WholesalePrice < 0)
            AddError(nameof(WholesalePrice), "السعر لا يمكن أن يكون سالباً");

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
            var request = new UpdateProductUnitRequest(UnitName, RetailPrice, WholesalePrice);
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
                UnitName,
                ConversionFactor,
                RetailPrice,
                WholesalePrice,
                IsBaseUnit,
                Barcodes.ToList());
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

    private void AddBarcode()
    {
        if (string.IsNullOrWhiteSpace(NewBarcode))
        {
            _ = _dialogService.ShowWarningAsync("بيانات غير مكتملة", "يرجى إدخال الباركود");
            return;
        }

        if (!Barcodes.Contains(NewBarcode.Trim()))
        {
            Barcodes.Add(NewBarcode.Trim());
        }

        NewBarcode = string.Empty;
    }

    private void RemoveBarcode()
    {
        if (SelectedBarcode != null)
        {
            Barcodes.Remove(SelectedBarcode);
        }
    }

    #endregion
}
