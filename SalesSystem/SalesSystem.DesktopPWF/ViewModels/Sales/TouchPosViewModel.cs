using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Sales;

/// <summary>
/// ViewModel for the Touch POS product/category selection panel.
/// Handles category browsing and product selection for the touch-friendly interface.
/// </summary>
public class TouchPosViewModel : ViewModelBase
{
    private readonly ICategoryApiService _categoryService;
    private readonly IProductApiService _productService;
    private readonly IInventoryApiService _inventoryService;

    private ObservableCollection<CategoryDto> _categories = new();
    private ObservableCollection<ProductDto> _products = new();
    private CategoryDto? _selectedCategory;
    private string? _errorMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="TouchPosViewModel"/> class.
    /// </summary>
    /// <param name="categoryService">Service for category API operations.</param>
    /// <param name="productService">Service for product API operations.</param>
    /// <param name="inventoryService">Service for inventory API operations (stock lookups).</param>
    /// <exception cref="ArgumentNullException">Thrown when a required dependency is null.</exception>
    public TouchPosViewModel(
        ICategoryApiService categoryService,
        IProductApiService productService,
        IInventoryApiService inventoryService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));

        SelectCategoryCommand = new RelayCommand(OnSelectCategory);
        AddToCartCommand = new RelayCommand((param) =>
        {
            if (param is ProductDto product)
                OnProductSelected?.Invoke(product);
        });

        InitializationTask = ExecuteAsync(LoadCategoriesOperationAsync);
    }

    /// <summary>
    /// Gets the initialization task that loads categories on startup.
    /// Callers should await this before interacting with the ViewModel.
    /// </summary>
    public Task InitializationTask { get; private set; }

    /// <summary>
    /// Callback invoked when a product is selected for adding to the cart.
    /// The parent ViewModel (e.g., SalesInvoiceEditorViewModel in Touch mode)
    /// should wire this up to handle adding items to the invoice.
    /// </summary>
    public Action<ProductDto>? OnProductSelected { get; set; }

    #region Properties

    /// <summary>
    /// Gets or sets the collection of product categories displayed in the touch panel.
    /// </summary>
    public ObservableCollection<CategoryDto> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    /// <summary>
    /// Gets or sets the collection of products for the currently selected category.
    /// </summary>
    public ObservableCollection<ProductDto> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    /// <summary>
    /// Gets or sets the currently selected category.
    /// When changed, products for this category are loaded automatically.
    /// </summary>
    public CategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value) && value != null)
            {
                _ = ExecuteAsync(() => LoadProductsForCategoryOperationAsync(value.Id));
            }
            else if (value == null)
            {
                Products.Clear();
            }
        }
    }

    private int _warehouseId;
    /// <summary>
    /// Gets or sets the warehouse ID for stock lookups.
    /// Should be set by the parent ViewModel before loading products.
    /// </summary>
    public int WarehouseId
    {
        get => _warehouseId;
        set => SetProperty(ref _warehouseId, value);
    }

    private Dictionary<int, decimal> _productStocks = new();

    /// <summary>
    /// Gets the product stock lookup dictionary (ProductId → Quantity).
    /// Updated whenever products are loaded for the selected category.
    /// </summary>
    public Dictionary<int, decimal> ProductStocks => _productStocks;

    /// <summary>
    /// Gets or sets the error message displayed when an operation fails.
    /// Set to null to clear the error display.
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to select a category and load its associated products.
    /// Takes a <see cref="CategoryDto"/> as command parameter.
    /// </summary>
    public ICommand SelectCategoryCommand { get; }

    /// <summary>
    /// Gets the command to add a product to the cart.
    /// Takes a <see cref="ProductDto"/> as command parameter.
    /// Invokes the <see cref="OnProductSelected"/> delegate with the selected product.
    /// </summary>
    public ICommand AddToCartCommand { get; }

    #endregion

    #region Methods

    private void OnSelectCategory(object? param)
    {
        if (param is CategoryDto category)
        {
            SelectedCategory = category;
            _ = ExecuteAsync(() => LoadProductsForCategoryOperationAsync(category.Id));
        }
    }

    /// <summary>
    /// Loads all active categories from the API.
    /// Sets <see cref="Categories"/> with the result and clears any previous error.
    /// </summary>
    private async Task LoadCategoriesOperationAsync()
    {
        ErrorMessage = null;
        var result = await _categoryService.GetAllAsync(includeInactive: false);
        if (result.IsSuccess && result.Value != null)
        {
            Categories = new ObservableCollection<CategoryDto>(result.Value);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل التصنيفات", "LoadCategories");
        }
    }

    /// <summary>
    /// Loads products for the specified category by filtering all active products client-side.
    /// Products are sorted newest-first by ID.
    /// If <see cref="WarehouseId"/> is set, stock quantities are also loaded
    /// and populated on each product's <see cref="ProductDto.CurrentStock"/> field.
    /// </summary>
    /// <param name="categoryId">The category ID to filter by.</param>
    private async Task LoadProductsForCategoryOperationAsync(int categoryId)
    {
        ErrorMessage = null;
        var result = await _productService.GetAllAsync(includeInactive: false);
        if (result.IsSuccess && result.Value != null)
        {
            var filtered = result.Value
                .Where(p => p.CategoryId == categoryId)
                .OrderByDescending(p => p.Id)
                .ToList();

            // Load stock quantities for all products in current warehouse
            if (_warehouseId > 0 && filtered.Count > 0)
            {
                try
                {
                    var stocksResult = await _inventoryService.GetWarehouseStocksAsync(_warehouseId);
                    if (stocksResult.IsSuccess && stocksResult.Value != null)
                    {
                        _productStocks = stocksResult.Value
                            .Where(s => s.ProductId > 0)
                            .ToDictionary(s => s.ProductId, s => s.Quantity);

                        // Apply stock to ProductDto (since it's a record, we create new instances)
                        filtered = filtered.Select(p =>
                        {
                            var stock = _productStocks.GetValueOrDefault(p.Id, 0m);
                            return p with { CurrentStock = stock };
                        }).OrderByDescending(p => p.Id).ToList();
                    }
                }
                catch
                {
                    // Stock loading failure shouldn't block product display
                    _productStocks = new Dictionary<int, decimal>();
                }
            }

            Products = new ObservableCollection<ProductDto>(filtered);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل المنتجات", "LoadProductsForCategory");
        }
    }

    #endregion
}
