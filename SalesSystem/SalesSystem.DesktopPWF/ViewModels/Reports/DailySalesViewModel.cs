using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for Daily Sales Summary Report.
/// </summary>
public class DailySalesViewModel : ViewModelBase
{
    private ISalesReportApiService? _salesReportApiService;
    private ISalesReportApiService SalesReportApiService => _salesReportApiService ??= App.GetService<ISalesReportApiService>();

    private IDialogService D => DialogService!;

    private DateTime _fromDate;
    private DateTime _toDate;
    private string? _errorMessage;
    private decimal _totalAmount;
    private decimal _totalDiscount;
    private decimal _totalNetAmount;
    private bool _hasData;

    public DailySalesViewModel()
    {
        _toDate = DateTime.Today;
        _fromDate = DateTime.Today.AddDays(-30);

        Entries = new ObservableCollection<DailySalesSummaryDto>();

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

    public ObservableCollection<DailySalesSummaryDto> Entries { get; }

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

    public decimal TotalDiscount
    {
        get => _totalDiscount;
        private set
        {
            if (SetProperty(ref _totalDiscount, value))
                OnPropertyChanged(nameof(FormattedTotalDiscount));
        }
    }

    public string FormattedTotalAmount => TotalAmount.ToString("N2");

    public string FormattedTotalDiscount => TotalDiscount.ToString("N2");

    public decimal TotalNetAmount
    {
        get => _totalNetAmount;
        set
        {
            if (SetProperty(ref _totalNetAmount, value))
                OnPropertyChanged(nameof(FormattedTotalNetAmount));
        }
    }

    public string FormattedTotalNetAmount => TotalNetAmount.ToString("N2");

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

        Log.Information("Loading daily sales summary from {FromDate} to {ToDate}", FromDate, ToDate);

        var result = await SalesReportApiService.GetDailySalesSummaryAsync(FromDate, ToDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var item in result.Value.OrderByDescending(x => x.Date))
                {
                    Entries.Add(item);
                }

                TotalAmount = result.Value.Sum(x => x.TotalAmount);
                TotalDiscount = result.Value.Sum(x => x.DiscountAmount);
                TotalNetAmount = result.Value.Sum(x => x.NetAmount);

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Daily sales summary loaded: {Count} days, Total={TotalAmount}", Entries.Count, TotalAmount);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير المبيعات اليومية", "DailySalesViewModel.LoadAsync");
            Log.Warning("Failed to load daily sales summary: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        await D.ShowInfoAsync("تصدير Excel", "سيتم تفعيل تصدير Excel قريباً");
    }

    #endregion
}
