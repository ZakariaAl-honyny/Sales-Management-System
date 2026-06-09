using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for the Balance Sheet Report — displays assets, liabilities, and equity.
/// </summary>
public class BalanceSheetViewModel : ViewModelBase
{
    private IFinancialReportApiService? _reportApiService;
    private IFinancialReportApiService ReportApiService => _reportApiService ??= App.GetService<IFinancialReportApiService>();

    private IDialogService D => DialogService!;

    private DateTime _asOfDate;
    private string? _errorMessage;
    private decimal _totalAssets;
    private decimal _totalLiabilities;
    private decimal _totalEquity;
    private bool _isBalanced;
    private bool _hasData;

    public BalanceSheetViewModel()
    {
        _asOfDate = DateTime.Today;

        AssetSections = new ObservableCollection<BalanceSheetSectionDto>();
        LiabilitySections = new ObservableCollection<BalanceSheetSectionDto>();
        EquitySections = new ObservableCollection<BalanceSheetSectionDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        ExportExcelCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportExcelAsync()));

        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));
    }

    #region Properties

    public DateTime AsOfDate
    {
        get => _asOfDate;
        set => SetProperty(ref _asOfDate, value);
    }

    public ObservableCollection<BalanceSheetSectionDto> AssetSections { get; }
    public ObservableCollection<BalanceSheetSectionDto> LiabilitySections { get; }
    public ObservableCollection<BalanceSheetSectionDto> EquitySections { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal TotalAssets
    {
        get => _totalAssets;
        private set
        {
            if (SetProperty(ref _totalAssets, value))
                OnPropertyChanged(nameof(FormattedTotalAssets));
        }
    }

    public decimal TotalLiabilities
    {
        get => _totalLiabilities;
        private set
        {
            if (SetProperty(ref _totalLiabilities, value))
                OnPropertyChanged(nameof(FormattedTotalLiabilities));
        }
    }

    public decimal TotalEquity
    {
        get => _totalEquity;
        private set
        {
            if (SetProperty(ref _totalEquity, value))
                OnPropertyChanged(nameof(FormattedTotalEquity));
        }
    }

    public bool IsBalanced
    {
        get => _isBalanced;
        private set
        {
            if (SetProperty(ref _isBalanced, value))
                OnPropertyChanged(nameof(BalanceStatusDisplay));
        }
    }

    public string FormattedTotalAssets => TotalAssets.ToString("N2");
    public string FormattedTotalLiabilities => TotalLiabilities.ToString("N2");
    public string FormattedTotalEquity => TotalEquity.ToString("N2");

    public string BalanceStatusDisplay => IsBalanced ? "🟢 متوازن" : "🔴 غير متوازن";

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
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading balance sheet as of {AsOfDate}", AsOfDate);

        var result = await ReportApiService.GetBalanceSheetAsync(AsOfDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                AssetSections.Clear();
                LiabilitySections.Clear();
                EquitySections.Clear();

                if (result.Value.Sections != null)
                    foreach (var section in result.Value.Sections)
                    {
                        if (section.Name.Contains("أصول", StringComparison.Ordinal))
                            AssetSections.Add(section);
                        else if (section.Name.Contains("خصوم", StringComparison.Ordinal))
                            LiabilitySections.Add(section);
                        else
                            EquitySections.Add(section);
                    }

                TotalAssets = result.Value.TotalAssets;
                TotalLiabilities = result.Value.TotalLiabilities;
                TotalEquity = result.Value.TotalEquity;
                IsBalanced = result.Value.IsBalanced;

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Balance sheet loaded: TotalAssets={TotalAssets}, TotalLiabilities={TotalLiabilities}, TotalEquity={TotalEquity}, Balanced={IsBalanced}",
                TotalAssets, TotalLiabilities, TotalEquity, IsBalanced);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الميزانية العمومية", "BalanceSheetViewModel.LoadAsync");
            Log.Warning("Failed to load balance sheet: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        await D.ShowInfoAsync("تصدير Excel", "سيتم تفعيل تصدير Excel قريباً");
    }

    private async Task ExportPdfAsync()
    {
        await D.ShowInfoAsync("تصدير PDF", "سيتم تفعيل تصدير PDF قريباً");
    }

    #endregion
}
