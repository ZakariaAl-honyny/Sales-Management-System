using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for Purchases by Product Report.
/// </summary>
public class PurchasesByProductViewModel : ViewModelBase
{
    private IPurchaseReportApiService? _purchaseReportApiService;
    private IPurchaseReportApiService PurchaseReportApiService => _purchaseReportApiService ??= App.GetService<IPurchaseReportApiService>();

    private IDialogService D => DialogService!;

    private DateTime _fromDate;
    private DateTime _toDate;
    private string? _errorMessage;
    private decimal _totalAmount;
    private decimal _totalCost;
    private bool _hasData;

    public PurchasesByProductViewModel()
    {
        _toDate = DateTime.Today;
        _fromDate = DateTime.Today.AddDays(-30);

        Entries = new ObservableCollection<PurchasesByProductDto>();

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

    public ObservableCollection<PurchasesByProductDto> Entries { get; }

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

    public decimal TotalCost
    {
        get => _totalCost;
        private set
        {
            if (SetProperty(ref _totalCost, value))
                OnPropertyChanged(nameof(FormattedTotalCost));
        }
    }

    public string FormattedTotalAmount => TotalAmount.ToString("N2");
    public string FormattedTotalCost => TotalCost.ToString("N2");

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

        Log.Information("Loading purchases by product report from {FromDate} to {ToDate}", FromDate, ToDate);

        var result = await PurchaseReportApiService.GetPurchasesByProductAsync(FromDate, ToDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var item in result.Value.OrderByDescending(x => x.TotalCost))
                {
                    Entries.Add(item);
                }

                TotalCost = result.Value.Sum(x => x.TotalCost);

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Purchases by product loaded: {Count} products, Total={TotalAmount}, Cost={TotalCost}",
                Entries.Count, TotalAmount, TotalCost);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير المشتريات حسب المنتج", "PurchasesByProductViewModel.LoadAsync");
            Log.Warning("Failed to load purchases by product: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        await D.ShowInfoAsync("تصدير Excel", "سيتم تفعيل تصدير Excel قريباً");
    }

    #endregion
}
