using System.Collections.ObjectModel;
using ClosedXML.Excel;
using Microsoft.Win32;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.Export;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

public class AgingReportViewModel : ViewModelBase
{
    private IReportApiService? _reportApiService;
    private ICustomerApiService? _customerApiService;
    private ISupplierApiService? _supplierApiService;

    private IReportApiService ReportApiService => _reportApiService ??= App.GetService<IReportApiService>();
    private ICustomerApiService CustomerApiService => _customerApiService ??= App.GetService<ICustomerApiService>();
    private ISupplierApiService SupplierApiService => _supplierApiService ??= App.GetService<ISupplierApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private string _selectedPartyType = "Customers";
    private int _selectedPartyId;
    private string? _errorMessage;
    private bool _hasSearched;
    private ObservableCollection<AgingReportDto> _reportData = new();

    public AgingReportViewModel()
    {
        PartyTypes = new ObservableCollection<string> { "عملاء", "موردين" };
        _selectedPartyType = "Customers";
        Parties = new ObservableCollection<PartySelectionDto>();

        SetDialogService(App.GetService<IDialogService>());

        LoadCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadDataAsync)));

        ExportCommand = new RelayCommand(ExportToExcel);
        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

        _ = ExecuteAsync(LoadDataAsync);
    }

    #region Properties

    public ObservableCollection<string> PartyTypes { get; }

    public string SelectedPartyType
    {
        get => _selectedPartyType;
        set
        {
            if (SetProperty(ref _selectedPartyType, value))
            {
                _selectedPartyId = 0;
                OnPropertyChanged(nameof(SelectedPartyId));
                _ = ExecuteAsync(LoadPartiesCoreAsync);
                _ = ExecuteAsync(LoadDataAsync);
            }
        }
    }

    /// <summary>
    /// The underlying API party type string ("Customers" or "Suppliers").
    /// </summary>
    public string PartyTypeApiValue => SelectedPartyType == "موردين" ? "Suppliers" : "Customers";

    public ObservableCollection<PartySelectionDto> Parties { get; }

    public int SelectedPartyId
    {
        get => _selectedPartyId;
        set
        {
            if (SetProperty(ref _selectedPartyId, value))
                _ = ExecuteAsync(LoadDataAsync);
        }
    }

    public ObservableCollection<AgingReportDto> ReportData
    {
        get => _reportData;
        set
        {
            if (SetProperty(ref _reportData, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(TotalDue));
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    public bool HasSearched
    {
        get => _hasSearched;
        set => SetProperty(ref _hasSearched, value);
    }

    public bool HasData => ReportData.Count > 0;
    public bool IsEmpty => ReportData.Count == 0 && HasSearched;

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal TotalDue => ReportData.Sum(x => x.TotalDue);
    public string SummaryText => $"إجمالي المستحق: {TotalDue:N2} — عدد: {ReportData.Count}";

    #endregion

    #region Commands

    public AsyncRelayCommand LoadCommand { get; }
    public RelayCommand ExportCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadDataAsync()
    {
        ErrorMessage = null;

        var partyId = SelectedPartyId > 0 ? SelectedPartyId : (int?)null;
        Log.Information("Loading aging report (Type: {PartyType}, PartyId: {PartyId})",
            PartyTypeApiValue, partyId);

        var result = await ReportApiService.GetAgingReportAsync(PartyTypeApiValue, partyId);

        if (result.IsSuccess && result.Value != null)
        {
            var sorted = result.Value
                .OrderByDescending(x => x.TotalDue)
                .ToList();

            ReportData = new ObservableCollection<AgingReportDto>(sorted);
            HasSearched = true;

            Log.Information("Aging report loaded: {Count} parties, TotalDue={TotalDue}",
                ReportData.Count, TotalDue);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير الأعمار", "AgingReportViewModel.LoadDataAsync");
            Log.Warning("Failed to load aging report: {Error}", result.Error);
        }
    }

    private async Task LoadPartiesCoreAsync()
    {
        if (PartyTypeApiValue == "Suppliers")
        {
            var result = await SupplierApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Parties.Clear();
                    Parties.Add(new PartySelectionDto(0, "الكل"));
                    foreach (var s in result.Value)
                        Parties.Add(new PartySelectionDto(s.Id, s.Name));
                });
            }
            else
            {
                HandleFailure(result.Error ?? "فشل في تحميل قائمة الموردين", "AgingReportViewModel.LoadPartiesCoreAsync");
            }
        }
        else
        {
            var result = await CustomerApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Parties.Clear();
                    Parties.Add(new PartySelectionDto(0, "الكل"));
                    foreach (var c in result.Value)
                        Parties.Add(new PartySelectionDto(c.Id, c.Name));
                });
            }
            else
            {
                HandleFailure(result.Error ?? "فشل في تحميل قائمة العملاء", "AgingReportViewModel.LoadPartiesCoreAsync");
            }
        }
    }

    #endregion

    #region Export

    private async void ExportToExcel()
    {
        if (ReportData.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"AgingReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("أعمار الديون");

                    worksheet.Cell(1, 1).Value = "الاسم";
                    worksheet.Cell(1, 2).Value = "الرصيد الإجمالي";
                    worksheet.Cell(1, 3).Value = "جاري";
                    worksheet.Cell(1, 4).Value = "1-30 يوم";
                    worksheet.Cell(1, 5).Value = "31-60 يوم";
                    worksheet.Cell(1, 6).Value = "61-90 يوم";
                    worksheet.Cell(1, 7).Value = "أكثر من 90 يوم";
                    worksheet.Cell(1, 8).Value = "إجمالي المستحق";

                    for (int i = 0; i < ReportData.Count; i++)
                    {
                        var item = ReportData[i];
                        worksheet.Cell(i + 2, 1).Value = item.Name;
                        worksheet.Cell(i + 2, 2).Value = item.TotalBalance;
                        worksheet.Cell(i + 2, 3).Value = item.Current;
                        worksheet.Cell(i + 2, 4).Value = item.Days1To30;
                        worksheet.Cell(i + 2, 5).Value = item.Days31To60;
                        worksheet.Cell(i + 2, 6).Value = item.Days61To90;
                        worksheet.Cell(i + 2, 7).Value = item.Days90Plus;
                        worksheet.Cell(i + 2, 8).Value = item.TotalDue;
                    }

                    var headerRange = worksheet.Range(1, 1, 1, 8);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير تقرير أعمار الديون إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير أعمار الديون إلى Excel", "AgingReportViewModel.ExportToExcel", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    private async Task ExportPdfAsync()
    {
        if (ReportData.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("الاسم", typeof(string));
            dataTable.Columns.Add("الرصيد الإجمالي", typeof(decimal));
            dataTable.Columns.Add("جاري", typeof(decimal));
            dataTable.Columns.Add("1-30 يوم", typeof(decimal));
            dataTable.Columns.Add("31-60 يوم", typeof(decimal));
            dataTable.Columns.Add("61-90 يوم", typeof(decimal));
            dataTable.Columns.Add("أكثر من 90 يوم", typeof(decimal));
            dataTable.Columns.Add("إجمالي المستحق", typeof(decimal));

            foreach (var item in ReportData)
                dataTable.Rows.Add(item.Name, item.TotalBalance,
                    item.Current, item.Days1To30, item.Days31To60,
                    item.Days61To90, item.Days90Plus, item.TotalDue);

            await PdfExportService.ExportToPdfAsync("تقرير أعمار الديون", dataTable, TotalDue,
                $"AgingReport_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير أعمار الديون إلى PDF", "AgingReportViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}

/// <summary>
/// Simple DTO for party selection in aging report.
/// </summary>
public record PartySelectionDto(int Id, string Name);
