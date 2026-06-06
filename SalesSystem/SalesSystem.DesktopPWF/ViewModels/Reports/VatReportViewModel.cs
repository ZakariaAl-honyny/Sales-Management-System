using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for the VAT Report — displays taxable invoices with tax amounts
/// for a given date range.
/// </summary>
public class VatReportViewModel : ViewModelBase
{
    private IFinancialReportApiService? _reportApiService;
    private IFinancialReportApiService ReportApiService => _reportApiService ??= App.GetService<IFinancialReportApiService>();

    // Non-null helper for DialogService (set via SetDialogService in constructor)
    private IDialogService D => DialogService!;

    private DateTime _dateFrom;
    private DateTime _dateTo;
    private string? _errorMessage;
    private decimal _totalVatAmount;

    public VatReportViewModel()
    {
        _dateTo = DateTime.Today;
        _dateFrom = DateTime.Today.AddDays(-30);

        ReportData = new ObservableCollection<VatReportDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));
    }

    #region Properties

    public DateTime DateFrom
    {
        get => _dateFrom;
        set => SetProperty(ref _dateFrom, value);
    }

    public DateTime DateTo
    {
        get => _dateTo;
        set => SetProperty(ref _dateTo, value);
    }

    public ObservableCollection<VatReportDto> ReportData { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal TotalVatAmount
    {
        get => _totalVatAmount;
        private set
        {
            if (SetProperty(ref _totalVatAmount, value))
                OnPropertyChanged(nameof(FormattedTotalVatAmount));
        }
    }

    public string FormattedTotalVatAmount => TotalVatAmount.ToString("N2");

    /// <summary>
    /// True when there are report rows
    /// </summary>
    public bool HasData => ReportData.Count > 0;

    /// <summary>
    /// True when no data — show empty state
    /// </summary>
    public bool IsEmpty => ReportData.Count == 0;

    #endregion

    #region Commands

    public AsyncRelayCommand GenerateReportCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading VAT report from {DateFrom} to {DateTo}", DateFrom, DateTo);

        var result = await ReportApiService.GetVatReportAsync(DateFrom, DateTo);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                ReportData.Clear();

                foreach (var item in result.Value.OrderByDescending(x => x.InvoiceDate))
                {
                    ReportData.Add(item);
                }

                TotalVatAmount = result.Value.Sum(x => x.TaxAmount);

                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("VAT report loaded: {Count} invoices, Total VAT: {TotalVatAmount}",
                ReportData.Count, TotalVatAmount);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير ضريبة القيمة المضافة", "VatReportViewModel.LoadAsync");
            Log.Warning("Failed to load VAT report: {Error}", result.Error);
        }
    }

    #endregion
}
