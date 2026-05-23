using System.Collections.ObjectModel;
using System.Windows;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Inventory;

/// <summary>
/// ViewModel for managing and viewing inventory movements and stock levels
/// </summary>
public class InventoryViewModel : ViewModelBase
{
    private readonly IInventoryApiService _inventoryService;
    private ObservableCollection<InventoryMovementDto> _movements = new();
    private ObservableCollection<WarehouseStockDto> _stocks = new();
    private string? _searchText;
    private int _currentPage = 1;
    private int _pageSize = 50;
    private bool _isMovementsEmpty;
    private bool _isStocksEmpty;

    public InventoryViewModel()
    {
        _inventoryService = App.GetService<IInventoryApiService>();
        
        LoadCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        RefreshCommand = new AsyncRelayCommand(async _ => 
        {
            SearchText = string.Empty;
            await LoadDataAsync();
        });

        // Initial load
        _ = LoadDataAsync();
    }

   
#region Properties

    public ObservableCollection<InventoryMovementDto> Movements
    {
        get => _movements;
        set => SetProperty(ref _movements, value);
    }

    public ObservableCollection<WarehouseStockDto> Stocks
    {
        get => _stocks;
        set => SetProperty(ref _stocks, value);
    }


    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public bool IsMovementsEmpty
    {
        get => _isMovementsEmpty;
        private set => SetProperty(ref _isMovementsEmpty, value);
    }

    public bool IsStocksEmpty
    {
        get => _isStocksEmpty;
        private set => SetProperty(ref _isStocksEmpty, value);
    }

   
#endregion

   
#region Commands

    public AsyncRelayCommand LoadCommand {
get;
}
    public AsyncRelayCommand RefreshCommand {
get;
}

   
#endregion

   
#region Logic

    private async Task LoadDataAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            // Load movements
            var movementsResult = await _inventoryService.GetMovementsAsync(page: _currentPage, pageSize: _pageSize);
            if (movementsResult.IsSuccess && movementsResult.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Movements.Clear();
                    foreach (var item in movementsResult.Value)
                    {
                        Movements.Add(item);
                    }
                    IsMovementsEmpty = Movements.Count == 0;
                });
            }

            // Load warehouse stocks
            var stocksResult = await _inventoryService.GetWarehouseStocksAsync(page: _currentPage, pageSize: _pageSize);
            if (stocksResult.IsSuccess && stocksResult.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Stocks.Clear();
                    foreach (var item in stocksResult.Value)
                    {
                        Stocks.Add(item);
                    }
                    IsStocksEmpty = Stocks.Count == 0;
                });
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "InventoryViewModel.LoadDataAsync", "[InventoryViewModel.LoadDataAsync] Failed to load inventory stocks.");
        }
        finally
        {
            IsBusy = false;
        }
    }

   
#endregion
}





