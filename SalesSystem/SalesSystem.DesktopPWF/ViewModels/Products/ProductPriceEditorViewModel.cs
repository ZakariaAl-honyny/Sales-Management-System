using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.Domain.Enums;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class ProductPriceEditorViewModel : ViewModelBase
{
    private readonly IProductPriceApiService _priceService;
    private readonly ICurrencyApiService _currencyService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private int? _priceId;
    private int _productUnitId;
    private int _currencyId;
    private PriceLevel _priceLevel = PriceLevel.Retail;
    private decimal _priceValue;
    private DateTime _effectiveFrom = DateTime.Today;
    private DateTime? _effectiveTo;
    private bool _hasEffectiveTo;
    private bool _isEditMode;
    private string? _errorMessage;
    private ObservableCollection<CurrencyDto> _currencies = new();
    private CurrencyDto? _selectedCurrency;

    public ProductPriceEditorViewModel()
        : this(
            App.GetService<IProductPriceApiService>(),
            App.GetService<ICurrencyApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public ProductPriceEditorViewModel(
        IProductPriceApiService priceService,
        ICurrencyApiService currencyService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService? toastService = null)
    {
        _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadCurrenciesAsync();
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

    public int CurrencyId
    {
        get => _currencyId;
        set => SetProperty(ref _currencyId, value);
    }

    public PriceLevel PriceLevel
    {
        get => _priceLevel;
        set
        {
            if (SetProperty(ref _priceLevel, value))
            {
                OnPropertyChanged(nameof(SelectedPriceLevelDisplay));
            }
        }
    }

    /// <summary>
    /// Gets the display name for the currently selected price level.
    /// </summary>
    public string SelectedPriceLevelDisplay => PriceLevel switch
    {
        PriceLevel.Retail => "تجزئة",
        PriceLevel.Wholesale => "جملة",
        PriceLevel.VIP => "VIP",
        PriceLevel.Distributor => "موزع",
        _ => "غير معروف"
    };

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

    public ObservableCollection<CurrencyDto> Currencies
    {
        get => _currencies;
        set => SetProperty(ref _currencies, value);
    }

    public CurrencyDto? SelectedCurrency
    {
        get => _selectedCurrency;
        set
        {
            if (SetProperty(ref _selectedCurrency, value))
            {
                CurrencyId = value?.Id ?? 0;
            }
        }
    }

    /// <summary>
    /// Available price levels for the dropdown.
    /// </summary>
    public ObservableCollection<PriceLevelItem> PriceLevelOptions { get; } = new()
    {
        new PriceLevelItem(PriceLevel.Retail, "تجزئة"),
        new PriceLevelItem(PriceLevel.Wholesale, "جملة"),
        new PriceLevelItem(PriceLevel.VIP, "VIP"),
        new PriceLevelItem(PriceLevel.Distributor, "موزع"),
    };

    /// <summary>
    /// The selected price level as a display item for the ComboBox.
    /// </summary>
    private PriceLevelItem? _selectedPriceLevelItem;
    public PriceLevelItem? SelectedPriceLevelItem
    {
        get => _selectedPriceLevelItem;
        set
        {
            if (SetProperty(ref _selectedPriceLevelItem, value) && value != null)
            {
                PriceLevel = value.Level;
            }
        }
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
        CurrencyId = existing.CurrencyId;
        PriceLevel = existing.PriceLevel;
        PriceValue = existing.Price;
        _effectiveFrom = existing.EffectiveFrom;
        _effectiveTo = existing.EffectiveTo;
        _hasEffectiveTo = existing.EffectiveTo.HasValue;
        IsEditMode = true;

        // Pre-select price level in dropdown
        SelectedPriceLevelItem = PriceLevelOptions.FirstOrDefault(p => p.Level == existing.PriceLevel);

        // Pre-select currency when loaded
        if (Currencies.Any(c => c.Id == existing.CurrencyId))
        {
            SelectedCurrency = Currencies.FirstOrDefault(c => c.Id == existing.CurrencyId);
        }
    }

    public async Task LoadCurrenciesAsync()
    {
        await ExecuteAsync(LoadCurrenciesOperationAsync);
    }

    private async Task LoadCurrenciesOperationAsync()
    {
        var result = await _currencyService.GetAllAsync();
        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Currencies.Clear();
                foreach (var c in result.Value)
                {
                    Currencies.Add(c);
                }

                // Auto-select base currency if none selected
                if (SelectedCurrency == null)
                {
                    var baseCurrency = Currencies.FirstOrDefault(c => c.IsBaseCurrency);
                    if (baseCurrency != null)
                    {
                        SelectedCurrency = baseCurrency;
                    }
                    else if (Currencies.Count > 0)
                    {
                        SelectedCurrency = Currencies[0];
                    }
                }
            });
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (CurrencyId <= 0)
            AddError(nameof(CurrencyId), "يجب اختيار العملة");

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
            CurrencyId,
            PriceLevel,
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
            PriceLevel,
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
        Currencies.Clear();
        base.Cleanup();
    }

    #endregion
}

/// <summary>
/// Helper class for binding PriceLevel enum to ComboBox with display names.
/// </summary>
public class PriceLevelItem
{
    public PriceLevel Level { get; }
    public string DisplayName { get; }

    public PriceLevelItem(PriceLevel level, string displayName)
    {
        Level = level;
        DisplayName = displayName;
    }

    public override bool Equals(object? obj) =>
        obj is PriceLevelItem other && Level == other.Level;

    public override int GetHashCode() => Level.GetHashCode();

    public override string ToString() => DisplayName;
}
