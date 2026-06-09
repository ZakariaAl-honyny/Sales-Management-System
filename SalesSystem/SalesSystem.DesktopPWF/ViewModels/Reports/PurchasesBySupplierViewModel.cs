using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for Purchases by Supplier Report.
/// </summary>
public class PurchasesBySupplierViewModel : ViewModelBase
{
    private IPurchaseReportApiService? _purchaseReportApiService;
    private IPurchaseReportApiService PurchaseReportApiService => _purchaseReportApiService ??= App.GetService<IPurchaseReportApiService>();

    private IDialogService D => DialogService!;

    private DateTime _fromDate;
    private DateTime _toDate;
    private string? _errorMessage;
    private decimal _totalAmount;
    private decimal _totalDue;
    private bool _hasData;

    public PurchasesBySupplierViewModel()
    {
        _toDate = DateTime.Today;
        _fromDate = DateTime.Today.AddDays(-30);

        Entries = new ObservableCollection<PurchasesBySupplierDto>();

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

    public ObservableCollection<PurchasesBySupplierDto> Entries { get; }

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

    public decimal TotalDue
    {
        get => _totalDue;
        private set
        {
            if (SetProperty(ref _totalDue, value))
                OnPropertyChanged(nameof(FormattedTotalDue));
        }
    }

    public string FormattedTotalAmount => TotalAmount.ToString("N2");
    public string FormattedTotalDue => TotalDue.ToString("N2");

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

        Log.Information("Loading purchases by supplier report from {FromDate} to {ToDate}", FromDate, ToDate);

        var result = await PurchaseReportApiService.GetPurchasesBySupplierAsync(FromDate, ToDate);

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
                TotalDue = result.Value.Sum(x => x.DueAmount);

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Purchases by supplier loaded: {Count} suppliers, Total={TotalAmount}", Entries.Count, TotalAmount);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير المشتريات حسب المورد", "PurchasesBySupplierViewModel.LoadAsync");
            Log.Warning("Failed to load purchases by supplier: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        await D.ShowInfoAsync("تصدير Excel", "سيتم تفعيل تصدير Excel قريباً");
    }

    #endregion
}
