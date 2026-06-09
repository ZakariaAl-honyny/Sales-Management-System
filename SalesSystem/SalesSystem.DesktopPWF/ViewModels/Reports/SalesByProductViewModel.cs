using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for Sales by Product Report.
/// </summary>
public class SalesByProductViewModel : ViewModelBase
{
    private ISalesReportApiService? _salesReportApiService;
    private ISalesReportApiService SalesReportApiService => _salesReportApiService ??= App.GetService<ISalesReportApiService>();

    private IDialogService D => DialogService!;

    private DateTime _fromDate;
    private DateTime _toDate;
    private string? _errorMessage;
    private decimal _totalAmount;
    private decimal _totalProfit;
    private bool _hasData;

    public SalesByProductViewModel()
    {
        _toDate = DateTime.Today;
        _fromDate = DateTime.Today.AddDays(-30);

        Entries = new ObservableCollection<SalesByProductDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        ExportExcelCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportExcelAsync()));
    }

    #region Properties

    public DateTime FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public ObservableCollection<SalesByProductDto> Entries { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal TotalAmount
    {
        get => _totalAmount;
        private set
        {
            if (SetProperty(ref _totalAmount, value))
                OnPropertyChanged(nameof(FormattedTotalAmount));
        }
    }

    public decimal TotalProfit
    {
        get => _totalProfit;
        private set
        {
            if (SetProperty(ref _totalProfit, value))
                OnPropertyChanged(nameof(FormattedTotalProfit));
        }
    }

    public string FormattedTotalAmount => TotalAmount.ToString("N2");
    public string FormattedTotalProfit => TotalProfit.ToString("N2");

    public bool HasData
    {
        get => _hasData;
        private set => SetProperty(ref _hasData, value);
    }

    public bool IsEmpty => !HasData;

    #endregion

    #region Commands

    public AsyncRelayCommand GenerateReportCommand { get; }
    public AsyncRelayCommand ExportExcelCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading sales by product report from {FromDate} to {ToDate}", FromDate, ToDate);

        var result = await SalesReportApiService.GetSalesByProductAsync(FromDate, ToDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var item in result.Value.OrderByDescending(x => x.TotalAmount))
                {
                    Entries.Add(item);
                }

                TotalAmount = result.Value.Sum(x => x.TotalAmount);
                TotalProfit = result.Value.Sum(x => x.TotalProfit);

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Sales by product loaded: {Count} products, Total={TotalAmount}, Profit={TotalProfit}",
                Entries.Count, TotalAmount, TotalProfit);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير المبيعات حسب المنتج", "SalesByProductViewModel.LoadAsync");
            Log.Warning("Failed to load sales by product: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        await D.ShowInfoAsync("تصدير Excel", "سيتم تفعيل تصدير Excel قريباً");
    }

    #endregion
}
