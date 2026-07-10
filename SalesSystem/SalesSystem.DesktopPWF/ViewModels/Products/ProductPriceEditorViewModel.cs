using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class ProductPriceEditorViewModel : ViewModelBase
{
    private readonly IProductPriceApiService _priceService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private int? _priceId;
    private int _productUnitId;
    private decimal _priceValue;
    private DateTime _effectiveFrom = DateTime.Today;
    private DateTime? _effectiveTo;
    private bool _hasEffectiveTo;
    private bool _isEditMode;
    private string? _errorMessage;

    public ProductPriceEditorViewModel()
        : this(
            App.GetService<IProductPriceApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public ProductPriceEditorViewModel(
        IProductPriceApiService priceService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService? toastService = null)
    {
        _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    /// <summary>
    /// Constructor for creating a new price for a given product unit.
    /// </summary>
    public ProductPriceEditorViewModel(int productUnitId)
        : this()
    {
        _productUnitId = productUnitId;
    }

    /// <summary>
    /// Constructor for editing an existing price.
    /// </summary>
    public ProductPriceEditorViewModel(int productUnitId, ProductPriceDto existing)
        : this(productUnitId)
    {
        LoadForEdit(existing);
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ السعر...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public int? PriceId
    {
        get => _priceId;
        private set => SetProperty(ref _priceId, value);
    }

    public int ProductUnitId
    {
        get => _productUnitId;
        set => SetProperty(ref _productUnitId, value);
    }

    public string? ProductUnitName { get; set; }

    public decimal PriceValue
    {
        get => _priceValue;
        set
        {
            if (SetProperty(ref _priceValue, value))
            {
                ValidateField(() => value >= 0, nameof(PriceValue), "السعر لا يمكن أن يكون سالباً");
            }
        }
    }

    public DateTime EffectiveFrom
    {
        get => _effectiveFrom;
        set
        {
            if (SetProperty(ref _effectiveFrom, value))
            {
                if (HasEffectiveTo && EffectiveTo.HasValue && value > EffectiveTo.Value)
                {
                    AddError(nameof(EffectiveFrom), "تاريخ البداية لا يمكن أن يكون بعد تاريخ النهاية");
                }
                else
                {
                    ClearErrors(nameof(EffectiveFrom));
                }
            }
        }
    }

    public DateTime? EffectiveTo
    {
        get => _effectiveTo;
        set
        {
            if (SetProperty(ref _effectiveTo, value))
            {
                if (value.HasValue && value.Value < EffectiveFrom)
                {
                    AddError(nameof(EffectiveTo), "تاريخ النهاية لا يمكن أن يكون قبل تاريخ البداية");
                }
                else
                {
                    ClearErrors(nameof(EffectiveTo));
                }
            }
        }
    }

    public bool HasEffectiveTo
    {
        get => _hasEffectiveTo;
        set
        {
            if (SetProperty(ref _hasEffectiveTo, value))
            {
                if (!value)
                {
                    EffectiveTo = null;
                }
            }
        }
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

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public void LoadForEdit(ProductPriceDto existing)
    {
        _priceId = existing.Id;
        _productUnitId = existing.ProductUnitId;
        PriceValue = existing.Price;
        _effectiveFrom = existing.EffectiveFrom;
        _effectiveTo = existing.EffectiveTo;
        _hasEffectiveTo = existing.EffectiveTo.HasValue;
        IsEditMode = true;
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (PriceValue < 0)
            AddError(nameof(PriceValue), "السعر لا يمكن أن يكون سالباً");

        if (PriceValue == 0)
            AddError(nameof(PriceValue), "السعر يجب أن يكون أكبر من صفر");

        if (HasEffectiveTo && EffectiveTo.HasValue && EffectiveTo.Value <= EffectiveFrom)
            AddError(nameof(EffectiveTo), "تاريخ النهاية يجب أن يكون بعد تاريخ البداية");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        if (IsEditMode && PriceId.HasValue)
        {
            await UpdatePriceAsync();
        }
        else
        {
            await CreatePriceAsync();
        }
    }

    private async Task CreatePriceAsync()
    {
        var request = new CreateProductPriceRequest(
            ProductUnitId,
            PriceValue,
            EffectiveFrom,
            HasEffectiveTo ? EffectiveTo : null);

        var result = await _priceService.CreateAsync(request);

        if (result.IsSuccess)
        {
            PriceId = result.Value?.Id;
            _eventBus.Publish(new ProductPriceChangedMessage(PriceId ?? 0));
            _toastService.ShowSuccess("تم إضافة السعر بنجاح");
            RequestClose();
        }
        else
        {
            var error = HandleFailure(result.Error ?? "فشل في إضافة السعر", "ProductPriceEditorViewModel.SaveOperationAsync", "[ProductPriceEditorViewModel.SaveOperationAsync] Failed to create product price.");
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في حفظ السعر", error);
        }
    }

    private async Task UpdatePriceAsync()
    {
        if (!PriceId.HasValue) return;

        var request = new UpdateProductPriceRequest(
            PriceValue,
            EffectiveFrom,
            HasEffectiveTo ? EffectiveTo : null);

        var result = await _priceService.UpdateAsync(PriceId.Value, request);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new ProductPriceChangedMessage(PriceId.Value));
            _toastService.ShowSuccess("تم تحديث السعر بنجاح");
            RequestClose();
        }
        else
        {
            var error = HandleFailure(result.Error ?? "فشل في تحديث السعر", "ProductPriceEditorViewModel.SaveOperationAsync", "[ProductPriceEditorViewModel.SaveOperationAsync] Failed to update product price.");
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في تحديث السعر", error);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    public override void Cleanup()
    {
        _priceId = null;
        IsEditMode = false;
        ErrorMessage = null;
        base.Cleanup();
    }

    #endregion
}


