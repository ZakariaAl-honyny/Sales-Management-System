using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

using SalesSystem.DesktopPWF.Helpers;

namespace SalesSystem.DesktopPWF.ViewModels.Transfers;

/// <summary>
/// ViewModel for Stock Transfer Editor
/// </summary>
public class StockTransferEditorViewModel : ViewModelBase
{
    private readonly IStockTransferApiService _transferService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IProductApiService _productService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly ISoundService _soundService;
    private readonly IInventoryApiService _inventoryService;
    private readonly IBarcodeInputService _barcodeService;
    private readonly ISettingsApiService _settingsService;
    private readonly ITransferPrinter _transferPrinter;

    private int? _transferId;
    private readonly bool _isReadOnly;

    private int _fromWarehouseId;
    private int _toWarehouseId;
    private DateTime _transferDate = DateTime.Today;
    private string _notes = string.Empty;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private string _quickSearchText = string.Empty;
    private InvoiceStatus _status = InvoiceStatus.Draft;

    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ObservableCollection<ProductDto> _products = new();
    private ObservableCollection<TransferItemViewModel> _items = new();

    public StockTransferEditorViewModel(
        IStockTransferApiService transferService,
        IWarehouseApiService warehouseService,
        IProductApiService productService,
        IEventBus eventBus,
        IDialogService dialogService,
        ISoundService soundService,
        IInventoryApiService inventoryService,
        IBarcodeInputService barcodeService,
        ISettingsApiService settingsService,
        ITransferPrinter transferPrinter,
        int? transferId = null,
        bool isReadOnly = false)
    {
        _transferId = transferId;
        _isReadOnly = isReadOnly;
        _transferService = transferService;
        _warehouseService = warehouseService;
        _productService = productService;
        _eventBus = eventBus;
        _dialogService = dialogService;
        _soundService = soundService;
        _inventoryService = inventoryService;
        _barcodeService = barcodeService;
        _settingsService = settingsService;
        _transferPrinter = transferPrinter;

        AddItemCommand = new RelayCommand(_ => OnAddItem());
        RemoveItemCommand = new RelayCommand(p => OnRemoveItem(p as TransferItemViewModel));
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsLoading && !_isReadOnly && Items.Count > 0 && Status == InvoiceStatus.Draft);
        CancelCommand = new RelayCommand(_ => OnCancel());
        PostCommand = new AsyncRelayCommand(PostAsync, () => CanPost);
        CancelTransferCommand = new AsyncRelayCommand(CancelTransferAsync, () => CanCancelTransfer);
        SearchProductCommand = new RelayCommand(SearchProduct);
        SearchProductSingleCommand = new RelayCommand(SearchProductSingle);
        QuickAddProductCommand = new AsyncRelayCommand(QuickAddProductAsync);
        PrintA4Command = new AsyncRelayCommand(PrintA4Async, () => _transferId.HasValue);

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        await LoadWarehousesAsync();
        await LoadProductsAsync();
        if (_transferId.HasValue)
        {
            await LoadTransferAsync();
        }
    }

    public StockTransferEditorViewModel(int? transferId = null, bool isReadOnly = false)
        : this(
            App.GetService<IStockTransferApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            App.GetService<ISoundService>(),
            App.GetService<IInventoryApiService>(),
            App.GetService<IBarcodeInputService>(),
            App.GetService<ISettingsApiService>(),
            App.GetService<ITransferPrinter>(),
            transferId,
            isReadOnly)
    {
    }

    public int FromWarehouseId
    {
        get => _fromWarehouseId;
        set => SetProperty(ref _fromWarehouseId, value);
    }

    public int ToWarehouseId
    {
        get => _toWarehouseId;
        set => SetProperty(ref _toWarehouseId, value);
    }

    public DateTime TransferDate
    {
        get => _transferDate;
        set => SetProperty(ref _transferDate, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string QuickSearchText
    {
        get => _quickSearchText;
        set => SetProperty(ref _quickSearchText, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsReadOnly => _isReadOnly;
    public bool IsEdit => _transferId.HasValue;

    public string WindowTitle => _isReadOnly ? "عرض تحويل المخزون" :
                                 _transferId.HasValue ? "تعديل تحويل المخزون" : "إضافة تحويل مخزون جديد";

    public ObservableCollection<WarehouseDto> Warehouses
    {
        get => _warehouses;
        set => SetProperty(ref _warehouses, value);
    }

    public ObservableCollection<ProductDto> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    public ObservableCollection<TransferItemViewModel> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public InvoiceStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(CanPost));
                OnPropertyChanged(nameof(CanCancelTransfer));
                UpdateCommandStates();
            }
        }
    }

    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand PostCommand { get; }
    public ICommand CancelTransferCommand { get; }
    public ICommand SearchProductCommand { get; }
    public ICommand SearchProductSingleCommand { get; }
    public ICommand QuickAddProductCommand { get; }
    public ICommand PrintA4Command { get; }

    public bool CanPost => Status == InvoiceStatus.Draft && _transferId.HasValue && !IsLoading;
    public bool CanCancelTransfer => Status != InvoiceStatus.Cancelled && _transferId.HasValue && !IsLoading;

    public async Task HandleBarcodeInput(Key key, string? keyText = null)
    {
        var barcode = _barcodeService.ProcessKey(key, keyText);
        if (barcode != null)
        {
            QuickSearchText = barcode;
            await QuickAddProductAsync();
        }
    }


    private async Task LoadWarehousesAsync()
    {
        try
        {
            var result = await _warehouseService.GetAllAsync();
            if (result.IsSuccess)
            {
                Warehouses = new ObservableCollection<WarehouseDto>(result.Value ?? new List<WarehouseDto>());
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "StockTransferEditorViewModel.LoadWarehousesAsync", "[StockTransferEditorViewModel.LoadWarehousesAsync] Failed to load warehouses.");
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var result = await _productService.GetAllAsync();
            if (result.IsSuccess)
            {
                Products = new ObservableCollection<ProductDto>(result.Value ?? new List<ProductDto>());
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "StockTransferEditorViewModel.LoadProductsAsync", "[StockTransferEditorViewModel.LoadProductsAsync] Failed to load products.");
        }
    }

    private async Task LoadTransferAsync()
    {
        if (!_transferId.HasValue) return;

        try
        {
            IsLoading = true;
            var result = await _transferService.GetByIdAsync(_transferId.Value);
            if (result.IsSuccess)
            {
                var transfer = result.Value!;
                FromWarehouseId = transfer.FromWarehouseId;
                ToWarehouseId = transfer.ToWarehouseId;
                TransferDate = transfer.TransferDate;
                Notes = transfer.Notes ?? string.Empty;
                Status = (InvoiceStatus)transfer.Status;

                Items.Clear();
                foreach (var item in transfer.Items)
                {
                    Items.Add(new TransferItemViewModel
                    {
                        Products = Products,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Mode = item.Mode
                    });
                }
                UpdateCommandStates();
            }
            else
            {
                ErrorMessage = result.Error ?? "حدث خطأ غير معروف";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "StockTransferEditorViewModel.LoadTransferAsync", $"[StockTransferEditorViewModel.LoadTransferAsync] Failed to load transfer data for ID: {_transferId}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnAddItem()
    {
        var item = new TransferItemViewModel(_soundService) { Products = this.Products };
        item.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(TransferItemViewModel.ProductId) || e.PropertyName == nameof(TransferItemViewModel.Quantity))
            {
                if (item.ProductId > 0 && FromWarehouseId > 0)
                {
                    var stockResult = await _inventoryService.GetStockAsync(item.ProductId, FromWarehouseId);
                    if (stockResult.IsSuccess && stockResult.Value < item.Quantity)
                    {
                        await _dialogService.ShowWarningAsync("تنبيه", $"المخزون غير كافٍ في المستودع المصدر. المتوفر: {stockResult.Value}");
                    }
                }
            }
        };
        Items.Add(item);
        UpdateCommandStates();
    }

    private void OnRemoveItem(TransferItemViewModel? item)
    {
        if (item != null)
        {
            Items.Remove(item);
            UpdateCommandStates();
        }
    }

    private async Task QuickAddProductAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickSearchText)) return;

        var searchText = QuickSearchText.Trim();
        QuickSearchText = string.Empty; // Clear immediately for responsiveness
        
        // Find product by barcode or code or name locally first
        var product = Products.FirstOrDefault(p => 
            p.Barcode == searchText || 
            p.Code == searchText || 
            p.Name.Equals(searchText, StringComparison.OrdinalIgnoreCase));

        if (product == null)
        {
            // Fallback to API if not found locally (RULE-041 / SPEC-010)
            var apiResult = await _productService.GetByBarcodeAsync(searchText);
            if (apiResult.IsSuccess && apiResult.Value != null)
            {
                product = apiResult.Value;
            }
        }

        if (product != null)
        {
            // ... (rest of the logic)
            if (FromWarehouseId <= 0)
            {
                await _dialogService.ShowWarningAsync("تنبيه", "يجب اختيار المستودع المصدر أولاً");
                return;
            }

            var stockResult = await _inventoryService.GetStockAsync(product.Id, FromWarehouseId);
            decimal currentStock = stockResult.IsSuccess ? stockResult.Value : 0;

            var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
            decimal neededQuantity = (existingItem?.Quantity ?? 0) + 1;

            if (currentStock < neededQuantity)
            {
                await _dialogService.ShowWarningAsync("تنبيه", $"المخزون غير كافٍ في المستودع المصدر. المتوفر: {currentStock}");
                return;
            }

            if (existingItem != null)
            {
                existingItem.Quantity += 1;
            }
            else
            {
                Items.Add(new TransferItemViewModel(_soundService)
                {
                    Products = this.Products,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = 1,
                    Mode = 1 // Retail by default
                });
            }
            _soundService.PlaySuccess();
            UpdateCommandStates();
        }
        else
        {
            _soundService.PlayError();
        }
    }

    private void SearchProduct(object? parameter)
    {
        var vm = new ViewModels.Products.ProductSelectionViewModel(FromWarehouseId);
        
        vm.OnProductSelected += async (product) => 
        {
            if (FromWarehouseId <= 0)
            {
                await _dialogService.ShowWarningAsync("تنبيه", "يجب اختيار المستودع المصدر أولاً");
                return;
            }

            var stockResult = await _inventoryService.GetStockAsync(product.Id, FromWarehouseId);
            decimal currentStock = stockResult.IsSuccess ? stockResult.Value : 0;

            var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
            decimal neededQuantity = (existingItem?.Quantity ?? 0) + 1;

            if (currentStock < neededQuantity)
            {
                await _dialogService.ShowWarningAsync("تنبيه", $"المخزون غير كافٍ في المستودع المصدر. المتوفر: {currentStock}");
                _soundService.PlayError();
                return;
            }

            if (existingItem != null)
            {
                existingItem.Quantity += 1;
            }
            else
            {
                // Remove empty lines
                var emptyLine = Items.FirstOrDefault(i => i.ProductId == 0);
                if (emptyLine != null)
                {
                    Items.Remove(emptyLine);
                }

                Items.Add(new TransferItemViewModel(_soundService)
                {
                    Products = this.Products,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = 1,
                    Mode = 1 // Retail by default
                });
            }
            _soundService.PlaySuccess();
            UpdateCommandStates();
        };

        _dialogService.ShowDialog(vm);

        // After dialog closes, ensure there is an empty line if needed
        if (Items.All(i => i.ProductId != 0))
        {
            OnAddItem();
        }
    }

    private void SearchProductSingle(object? parameter)
    {
        var targetLine = parameter as TransferItemViewModel;
        var vm = new ViewModels.Products.ProductSelectionViewModel(FromWarehouseId);
        bool picked = false;

        vm.OnProductSelected += async (product) =>
        {
            if (picked) return;
            picked = true;

            if (FromWarehouseId <= 0)
            {
                await _dialogService.ShowWarningAsync("تنبيه", "يجب اختيار المستودع المصدر أولاً");
                return;
            }

            var stockResult = await _inventoryService.GetStockAsync(product.Id, FromWarehouseId);
            decimal currentStock = stockResult.IsSuccess ? stockResult.Value : 0;

            if (targetLine != null)
            {
                if (currentStock < 1)
                {
                    await _dialogService.ShowWarningAsync("خطأ في المخزون", $"المخزون غير كافٍ. المتوفر: {currentStock}");
                    _soundService.PlayError();
                }
                else
                {
                    targetLine.ProductId = product.Id;
                    targetLine.ProductName = product.Name;
                    targetLine.Quantity = 1;
                    targetLine.Mode = 1;
                    _soundService.PlaySuccess();
                }
            }
            else
            {
                var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
                decimal neededQuantity = (existingItem?.Quantity ?? 0) + 1;

                if (currentStock < neededQuantity)
                {
                    await _dialogService.ShowWarningAsync("خطأ في المخزون", $"المخزون غير كافٍ. المتوفر: {currentStock}");
                    _soundService.PlayError();
                }
                else if (existingItem != null)
                {
                    existingItem.Quantity += 1;
                    _soundService.PlaySuccess();
                }
                else
                {
                    var emptyLine = Items.FirstOrDefault(i => i.ProductId == 0);
                    if (emptyLine != null) Items.Remove(emptyLine);

                    Items.Add(new TransferItemViewModel(_soundService)
                    {
                        Products = this.Products,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 1,
                        Mode = 1
                    });
                    _soundService.PlaySuccess();
                }
            }
            UpdateCommandStates();

            Application.Current.Dispatcher.Invoke(() => vm.CloseDialog());
        };

        _dialogService.ShowDialog(vm);
    }

    private void UpdateCommandStates()
    {
        (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PostCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CancelTransferCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrintA4Command as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private async Task SaveAsync()
    {
        var errors = new List<string>();

        if (FromWarehouseId == 0)
            errors.Add("• يرجى اختيار المستودع المصدر");

        if (ToWarehouseId == 0)
            errors.Add("• يرجى اختيار المستودع المستهدف");

        if (FromWarehouseId != 0 && ToWarehouseId != 0 && FromWarehouseId == ToWarehouseId)
            errors.Add("• لا يمكن التحويل إلى نفس المستودع");

        if (Items.Count == 0)
            errors.Add("• يرجى إضافة صنف واحد على الأقل");

        if (Items.Any(i => i.ProductId == 0))
            errors.Add("• يرجى اختيار منتج لكل صنف مضاف");

        if (Items.Any(i => i.Quantity <= 0))
            errors.Add("• يرجى إدخال كمية صحيحة (أكبر من 0) لكل صنف");

        if (errors.Any())
        {
            string errorMsg = "يرجى إكمال وتصحيح البيانات التالية:\n\n" + string.Join("\n", errors);
            await _dialogService.ShowWarningAsync("بيانات غير مكتملة", errorMsg);
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var items = Items.Select(i => new CreateStockTransferItemRequest(
                i.ProductId,
                i.Quantity,
                null,
                i.Mode
            )).ToList();

            Result<StockTransferDto> result;

            if (_transferId.HasValue)
            {
                var request = new UpdateStockTransferRequest(
                    FromWarehouseId,
                    ToWarehouseId,
                    TransferDate,
                    Notes,
                    items);
                result = await _transferService.UpdateAsync(_transferId.Value, request);
            }
            else
            {
                var request = new CreateStockTransferRequest(
                    FromWarehouseId,
                    ToWarehouseId,
                    TransferDate,
                    Notes,
                    items);
                result = await _transferService.CreateAsync(request);
            }

            if (result.IsSuccess)
            {
                _transferId = result.Value!.Id;
                Status = (InvoiceStatus)result.Value.Status;
                _eventBus.Publish(new StockTransferChangedMessage(_transferId.Value));
                
                UpdateCommandStates();
                OnPropertyChanged(nameof(IsEdit));
                OnPropertyChanged(nameof(WindowTitle));
                
                await _dialogService.ShowSuccessAsync("نجاح", "تم حفظ المسودة بنجاح. يمكنك الآن الترحيل النهائي إذا أردت.");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ غير معروف", "StockTransferEditorViewModel.SaveAsync", "[StockTransferEditorViewModel.SaveAsync] Failed to save stock transfer.");
                await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "StockTransferEditorViewModel.SaveAsync", "[StockTransferEditorViewModel.SaveAsync] Failed to save stock transfer.");
            await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnCancel()
    {
        RequestClose();
    }

    private async Task PostAsync()
    {
        if (!_transferId.HasValue) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var result = await _transferService.PostAsync(_transferId.Value);

            if (result.IsSuccess)
            {
                _eventBus.Publish(new StockTransferChangedMessage(result.Value!.Id));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ غير معروف", "StockTransferEditorViewModel.PostAsync", "[StockTransferEditorViewModel.PostAsync] Failed to post stock transfer.");
                await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "StockTransferEditorViewModel.PostAsync", "[StockTransferEditorViewModel.PostAsync] Failed to post stock transfer.");
            await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CancelTransferAsync()
    {
        if (!_transferId.HasValue) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var result = await _transferService.CancelAsync(_transferId.Value);

            if (result.IsSuccess)
            {
                _eventBus.Publish(new StockTransferChangedMessage(result.Value!.Id));
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "حدث خطأ غير معروف", "StockTransferEditorViewModel.CancelTransferAsync", "[StockTransferEditorViewModel.CancelTransferAsync] Failed to cancel stock transfer.");
                await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "StockTransferEditorViewModel.CancelTransferAsync", "[StockTransferEditorViewModel.CancelTransferAsync] Failed to cancel stock transfer.");
            await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PrintA4Async()
    {
        if (!_transferId.HasValue) return;

        IsLoading = true;
        try
        {
            var settingsResult = await _settingsService.GetSettingsAsync();
            if (settingsResult.IsSuccess && settingsResult.Value != null)
            {
                var transferResult = await _transferService.GetByIdAsync(_transferId.Value);
                if (transferResult.IsSuccess && transferResult.Value != null)
                {
                    _transferPrinter.PrintPreview(
                        transferResult.Value.ToPrintDto(),
                        transferResult.Value.Items.ToPrintDtos(),
                        settingsResult.Value.ToPrintDto());
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "StockTransferEditorViewModel.PrintA4Async", "[StockTransferEditorViewModel.PrintA4Async] Failed to prepare print.");
            await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>
/// ViewModel for a single transfer item
/// </summary>
public class TransferItemViewModel : ViewModelBase
{
    private readonly ISoundService? _soundService;
    private int _productId;
    private string _productName = string.Empty;
    private decimal _quantity;
    private byte _mode = 1;

    public TransferItemViewModel(ISoundService? soundService = null)
    {
        _soundService = soundService;
    }

    public int ProductId
    {
        get => _productId;
        set
        {
            SetProperty(ref _productId, value);
            if (Products?.Any(p => p.Id == value) == true)
            {
                ProductName = Products?.First(p => p.Id == value).Name ?? string.Empty;
            }
        }
    }

    public string ProductName
    {
        get => _productName;
        set => SetProperty(ref _productName, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                _soundService?.PlaySuccess();
            }
        }
    }

    public byte Mode
    {
        get => _mode;
        set => SetProperty(ref _mode, value);
    }

    public ObservableCollection<ProductDto>? Products { get; set; }
}
