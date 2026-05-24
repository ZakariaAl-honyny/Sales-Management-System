using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using ClosedXML.Excel;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Helpers;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Microsoft.Win32;

namespace SalesSystem.DesktopPWF.ViewModels.Inventory;

public class LowStockViewModel : ViewModelBase
{
    private readonly IReportApiService _reportService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    private ObservableCollection<LowStockReportDto> _items = new();
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private int? _selectedWarehouseId;
    private string _searchText = string.Empty;

    public LowStockViewModel()
        : this(
            App.GetService<IReportApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<INavigationService>())
    {
    }

    public LowStockViewModel(
        IReportApiService reportService,
        IWarehouseApiService warehouseService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _reportService = reportService;
        _warehouseService = warehouseService;
        _dialogService = dialogService;
        _navigationService = navigationService;

        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        ExportCommand = new RelayCommand(ExportToExcel);
        PrintCommand = new RelayCommand(Print);
        
        _ = LoadWarehousesAsync();
        _ = LoadDataAsync();
    }

    public ObservableCollection<LowStockReportDto> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public ObservableCollection<WarehouseDto> Warehouses
    {
        get => _warehouses;
        set => SetProperty(ref _warehouses, value);
    }

    public int? SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set
        {
            if (SetProperty(ref _selectedWarehouseId, value))
            {
                _ = LoadDataAsync();
            }
        }
    }


    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                // Logic for local filtering could go here if needed
            }
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand PrintCommand { get; }

    private async Task LoadWarehousesAsync()
    {
        try
        {
            var result = await _warehouseService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Warehouses.Clear();
                    // Add "All Warehouses" option
                    Warehouses.Add(new WarehouseDto(0, "ظƒظ„ ط§ظ„ظ…ط®ط§ط²ظ†", string.Empty, true, true));
                    foreach (var wh in result.Value)
                        Warehouses.Add(wh);
                    
                    // Auto-select first warehouse or default
                    var defaultOrFirst = result.Value.FirstOrDefault(w => w.IsDefault) ?? result.Value.FirstOrDefault();
                    if (defaultOrFirst != null)
                    {
                        SelectedWarehouseId = defaultOrFirst.Id;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "LowStockViewModel.LoadWarehousesAsync", "[LowStockViewModel.LoadWarehousesAsync] Error loading warehouses.");
        }
    }

    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _reportService.GetLowStockReportAsync(SelectedWarehouseId);
            if (result.IsSuccess)
            {
                Items = new ObservableCollection<LowStockReportDto>(result.Value ?? new List<LowStockReportDto>());
            }
            else
            {
                await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ طھط­ظ…ظٹظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ", result.Error ?? "ظپط´ظ„ ظپظٹ طھط­ظ…ظٹظ„ طھظ‚ط±ظٹط± ط§ظ„ظ…ط®ط²ظˆظ† ط§ظ„ظ…ظ†ط®ظپط¶");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("Failed to load low stock report", "LowStockViewModel.LoadDataAsync", ex);
            await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ طھط­ظ…ظٹظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ", "ط­ط¯ط« ط®ط·ط£ ط؛ظٹط± ظ…طھظˆظ‚ط¹ ط£ط«ظ†ط§ط، طھط­ظ…ظٹظ„ ط§ظ„طھظ‚ط±ظٹط±. ظٹط±ط¬ظ‰ ط§ظ„ظ…ط­ط§ظˆظ„ط© ظ…ط±ط© ط£ط®ط±ظ‰.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void ExportToExcel()
    {
        if (Items.Count == 0)
        {
            await _dialogService.ShowWarningAsync("طھظ†ط¨ظٹظ‡", "ظ„ط§ طھظˆط¬ط¯ ط¨ظٹط§ظ†ط§طھ ظ„طھطµط¯ظٹط±ظ‡ط§");
            return;
        }

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"LowStockReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Low Stock");
                    
                    // Headers
                    worksheet.Cell(1, 1).Value = "ظƒظˆط¯ ط§ظ„ظ…ظ†طھط¬";
                    worksheet.Cell(1, 2).Value = "ط§ط³ظ… ط§ظ„ظ…ظ†طھط¬";
                    worksheet.Cell(1, 3).Value = "ط§ظ„ظپط¦ط©";
                    worksheet.Cell(1, 4).Value = "ط§ظ„ظ…ط³طھظˆط¯ط¹";
                    worksheet.Cell(1, 5).Value = "ط§ظ„ظ…ط®ط²ظˆظ† ط§ظ„ط­ط§ظ„ظٹ (طھط¬ط²ط¦ط©)";
                    worksheet.Cell(1, 6).Value = "طھظ†ط¨ظٹظ‡ ط§ظ„ظ…ط®ط²ظˆظ† (طھط¬ط²ط¦ط©)";
                    worksheet.Cell(1, 7).Value = "ط§ظ„ط¹ط¬ط² (طھط¬ط²ط¦ط©)";
                    worksheet.Cell(1, 8).Value = "ط§ظ„ط·ظ„ط¨ ط§ظ„ظ…ظ‚طھط±ط­ (ط¬ظ…ظ„ط©)";
                    worksheet.Cell(1, 9).Value = "ط§ظ„ط·ظ„ط¨ ط§ظ„ظ…ظ‚طھط±ط­ (طھط¬ط²ط¦ط©)";

                    // Data
                    for (int i = 0; i < Items.Count; i++)
                    {
                        var item = Items[i];
                        // worksheet.Cell(i + 2, 1).Value = item.ProductCode;
                        worksheet.Cell(i + 2, 2).Value = item.ProductName;
                        worksheet.Cell(i + 2, 3).Value = item.CategoryName;
                        worksheet.Cell(i + 2, 4).Value = item.WarehouseName;
                        worksheet.Cell(i + 2, 5).Value = item.CurrentRetailQty;
                        worksheet.Cell(i + 2, 6).Value = item.ReorderLevelRetailQty;
                        worksheet.Cell(i + 2, 7).Value = item.DeficitRetailQty;
                        worksheet.Cell(i + 2, 8).Value = item.SuggestedWholesaleBoxes;
                        worksheet.Cell(i + 2, 9).Value = item.SuggestedRetailRemainder;
                    }

                    // Styling
                    var headerRange = worksheet.Range(1, 1, 1, 9);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }
                await _dialogService.ShowInfoAsync("ظ†ط¬ط§ط­", "طھظ… طھطµط¯ظٹط± ط§ظ„ظ…ظ„ظپ ط¨ظ†ط¬ط§ط­");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("Failed to export low stock report to Excel", "LowStockViewModel.ExportToExcel", ex);
            await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ طھطµط¯ظٹط± ط§ظ„ظ…ظ„ظپ", "ط­ط¯ط« ط®ط·ط£ ط؛ظٹط± ظ…طھظˆظ‚ط¹ ط£ط«ظ†ط§ط، طھطµط¯ظٹط± ط§ظ„ظ…ظ„ظپ. ظٹط±ط¬ظ‰ ط§ظ„ظ…ط­ط§ظˆظ„ط© ظ…ط±ط© ط£ط®ط±ظ‰.");
        }
    }

    private async void Print()
    {
        // Printing logic using a print service or simple PDF export
        await _dialogService.ShowInfoAsync("ظ…ط¹ظ„ظˆظ…ط§طھ", "ط¬ط§ط±ظٹ طھط¬ظ‡ظٹط² ط§ظ„ط·ط¨ط§ط¹ط©...");
    }
}




