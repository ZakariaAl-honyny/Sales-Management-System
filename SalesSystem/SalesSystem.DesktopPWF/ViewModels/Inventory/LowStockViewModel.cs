using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
        ExportCommand = new AsyncRelayCommand(ExportToExcelAsync);
        PrintCommand = new AsyncRelayCommand(PrintAsync);
        
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
                    Warehouses.Add(new WarehouseDto(0, 0, null, "كل المخازن", null, null, null, true));
                    foreach (var wh in result.Value)
                        Warehouses.Add(wh);
                    
                    // Auto-select first warehouse or default
                    var defaultOrFirst = result.Value.FirstOrDefault();
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
                Items = new ObservableCollection<LowStockReportDto>((result.Value ?? new List<LowStockReportDto>()).OrderBy(x => x.ProductName ?? string.Empty));
            }
            else
            {
                await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", result.Error ?? "فشل في تحميل تقرير المخزون المنخفض");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("Failed to load low stock report", "LowStockViewModel.LoadDataAsync", ex);
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", "حدث خطأ غير متوقع أثناء تحميل التقرير. يرجى المحاولة مرة أخرى.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportToExcelAsync()
    {
        if (Items.Count == 0)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
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
                    worksheet.Cell(1, 1).Value = "رقم المنتج";
                    worksheet.Cell(1, 2).Value = "اسم المنتج";
                    worksheet.Cell(1, 3).Value = "الفئة";
                    worksheet.Cell(1, 4).Value = "المستودع";
                    worksheet.Cell(1, 5).Value = "المخزون الحالي (تجزئة)";
                    worksheet.Cell(1, 6).Value = "تنبيه المخزون (تجزئة)";
                    worksheet.Cell(1, 7).Value = "العجز (تجزئة)";
                    worksheet.Cell(1, 8).Value = "الطلب المقترح (جملة)";
                    worksheet.Cell(1, 9).Value = "الطلب المقترح (تجزئة)";

                    // Data
                    for (int i = 0; i < Items.Count; i++)
                    {
                        var item = Items[i];
                        worksheet.Cell(i + 2, 1).Value = item.ProductId;
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
                await _dialogService.ShowInfoAsync("نجاح", "تم تصدير الملف بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("Failed to export low stock report to Excel", "LowStockViewModel.ExportToExcel", ex);
            await _dialogService.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    private async Task PrintAsync()
    {
        try
        {
            // Printing logic using a print service or simple PDF export
            await _dialogService.ShowInfoAsync("معلومات", "جاري تجهيز الطباعة...");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في طباعة تقرير نواقص المخزون", "LowStockViewModel.PrintAsync", ex);
        }
    }
}




