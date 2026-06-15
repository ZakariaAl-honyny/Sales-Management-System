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

/// <summary>
/// ViewModel for the Account Statement Report — displays debits, credits, and running balance
/// for a customer or supplier over a date range.
/// </summary>
public class AccountStatementViewModel : ViewModelBase
{
    private IFinancialReportApiService? _reportApiService;
    private ICustomerApiService? _customerApiService;
    private ISupplierApiService? _supplierApiService;

    private IFinancialReportApiService ReportApiService => _reportApiService ??= App.GetService<IFinancialReportApiService>();
    private ICustomerApiService CustomerApiService => _customerApiService ??= App.GetService<ICustomerApiService>();
    private ISupplierApiService SupplierApiService => _supplierApiService ??= App.GetService<ISupplierApiService>();

    // Non-null helper for DialogService (set via SetDialogService in constructor)
    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private DateTime _dateFrom;
    private DateTime _dateTo;
    private string? _errorMessage;
    private bool _isCustomerMode = true;
    private CustomerDto? _selectedCustomer;
    private SupplierDto? _selectedSupplier;
    private decimal _runningBalance;

    public AccountStatementViewModel()
    {
        _dateTo = DateTime.Today;
        _dateFrom = DateTime.Today.AddDays(-30);

        Entries = new ObservableCollection<AccountStatementDto>();
        Customers = new ObservableCollection<CustomerDto>();
        Suppliers = new ObservableCollection<SupplierDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateCustomerStatementCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadCustomerStatementAsync)));

        GenerateSupplierStatementCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadSupplierStatementAsync)));

        LoadCustomersCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadCustomersAsync)));

        LoadSuppliersCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadSuppliersAsync)));

        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

        ExportExcelCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportExcelAsync()));

        // Load initial data
        _ = LoadCustomersAsync();
        _ = LoadSuppliersAsync();
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

    /// <summary>
    /// True when viewing a customer statement, false for supplier
    /// </summary>
    public bool IsCustomerMode
    {
        get => _isCustomerMode;
        set
        {
            if (SetProperty(ref _isCustomerMode, value))
            {
                OnPropertyChanged(nameof(IsSupplierMode));
                OnPropertyChanged(nameof(ModeLabel));
            }
        }
    }

    /// <summary>
    /// True when viewing a supplier statement
    /// </summary>
    public bool IsSupplierMode => !IsCustomerMode;

    /// <summary>
    /// Label indicating the current mode
    /// </summary>
    public string ModeLabel => IsCustomerMode ? "كشف حساب عميل" : "كشف حساب مورد";

    public ObservableCollection<CustomerDto> Customers { get; }
    public ObservableCollection<SupplierDto> Suppliers { get; }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public SupplierDto? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetProperty(ref _selectedSupplier, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<AccountStatementDto> Entries { get; }

    public decimal RunningBalance
    {
        get => _runningBalance;
        private set
        {
            if (SetProperty(ref _runningBalance, value))
                OnPropertyChanged(nameof(FormattedRunningBalance));
        }
    }

    public string FormattedRunningBalance => RunningBalance.ToString("N2");

    /// <summary>
    /// True when there are entries
    /// </summary>
    public bool HasData => Entries.Count > 0;

    /// <summary>
    /// True when no entries — show empty state
    /// </summary>
    public bool IsEmpty => Entries.Count == 0;

    #endregion

    #region Commands

    public AsyncRelayCommand GenerateCustomerStatementCommand { get; }
    public AsyncRelayCommand GenerateSupplierStatementCommand { get; }
    public AsyncRelayCommand LoadCustomersCommand { get; }
    public AsyncRelayCommand LoadSuppliersCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }
    public AsyncRelayCommand ExportExcelCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadCustomersAsync()
    {
        try
        {
            var result = await CustomerApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Customers.Clear();
                    foreach (var c in result.Value.Where(c => c.IsActive))
                        Customers.Add(c);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة العملاء", "AccountStatementViewModel.LoadCustomersAsync", ex);
        }
    }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            var result = await SupplierApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Suppliers.Clear();
                    foreach (var s in result.Value.Where(s => s.IsActive))
                        Suppliers.Add(s);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة الموردين", "AccountStatementViewModel.LoadSuppliersAsync", ex);
        }
    }

    private async Task LoadCustomerStatementAsync()
    {
        ErrorMessage = null;

        if (SelectedCustomer == null)
        {
            await D.ShowWarningAsync("تنبيه", "يرجى اختيار عميل لعرض كشف الحساب");
            return;
        }

        Log.Information("Loading customer account statement: Customer={CustomerId}, from {DateFrom} to {DateTo}",
            SelectedCustomer.Id, DateFrom, DateTo);

        var result = await ReportApiService.GetCustomerAccountStatementAsync(
            SelectedCustomer.Id, DateFrom, DateTo);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var entry in result.Value.OrderByDescending(x => x.Date))
                {
                    Entries.Add(entry);
                }

                RunningBalance = result.Value.LastOrDefault()?.Balance ?? 0;

                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Customer account statement loaded: {Count} entries, Balance: {RunningBalance}",
                Entries.Count, RunningBalance);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل كشف حساب العميل", "AccountStatementViewModel.LoadCustomerStatementAsync");
            Log.Warning("Failed to load customer account statement: {Error}", result.Error);
        }
    }

    private async Task LoadSupplierStatementAsync()
    {
        ErrorMessage = null;

        if (SelectedSupplier == null)
        {
            await D.ShowWarningAsync("تنبيه", "يرجى اختيار مورد لعرض كشف الحساب");
            return;
        }

        Log.Information("Loading supplier account statement: Supplier={SupplierId}, from {DateFrom} to {DateTo}",
            SelectedSupplier.Id, DateFrom, DateTo);

        var result = await ReportApiService.GetSupplierAccountStatementAsync(
            SelectedSupplier.Id, DateFrom, DateTo);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var entry in result.Value.OrderByDescending(x => x.Date))
                {
                    Entries.Add(entry);
                }

                RunningBalance = result.Value.LastOrDefault()?.Balance ?? 0;

                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Supplier account statement loaded: {Count} entries, Balance: {RunningBalance}",
                Entries.Count, RunningBalance);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل كشف حساب المورد", "AccountStatementViewModel.LoadSupplierStatementAsync");
            Log.Warning("Failed to load supplier account statement: {Error}", result.Error);
        }
    }

    #endregion

    #region Export

    private async Task ExportPdfAsync()
    {
        if (!HasData)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("التاريخ", typeof(string));
            dataTable.Columns.Add("البيان", typeof(string));
            dataTable.Columns.Add("رقم المرجع", typeof(string));
            dataTable.Columns.Add("مدين", typeof(decimal));
            dataTable.Columns.Add("دائن", typeof(decimal));
            dataTable.Columns.Add("الرصيد", typeof(decimal));

            foreach (var item in Entries)
                dataTable.Rows.Add(item.Date.ToString("yyyy/MM/dd"),
                    item.Description, item.ReferenceNumber,
                    item.Debit, item.Credit, item.Balance);

            var title = IsCustomerMode ? "كشف حساب عميل" : "كشف حساب مورد";
            await PdfExportService.ExportToPdfAsync(title, dataTable, RunningBalance,
                $"AccountStatement_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير كشف الحساب إلى PDF", "AccountStatementViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    private async Task ExportExcelAsync()
    {
        if (!HasData)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"AccountStatement_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var title = IsCustomerMode ? "كشف حساب عميل" : "كشف حساب مورد";
                    var worksheet = workbook.Worksheets.Add(title);

                    // Header row
                    worksheet.Cell(1, 1).Value = "التاريخ";
                    worksheet.Cell(1, 2).Value = "البيان";
                    worksheet.Cell(1, 3).Value = "رقم المرجع";
                    worksheet.Cell(1, 4).Value = "مدين";
                    worksheet.Cell(1, 5).Value = "دائن";
                    worksheet.Cell(1, 6).Value = "الرصيد";

                    // Data rows
                    for (int i = 0; i < Entries.Count; i++)
                    {
                        var item = Entries[i];
                        worksheet.Cell(i + 2, 1).Value = item.Date.ToString("yyyy/MM/dd");
                        worksheet.Cell(i + 2, 2).Value = item.Description;
                        worksheet.Cell(i + 2, 3).Value = item.ReferenceNumber;
                        worksheet.Cell(i + 2, 4).Value = item.Debit;
                        worksheet.Cell(i + 2, 5).Value = item.Credit;
                        worksheet.Cell(i + 2, 6).Value = item.Balance;
                    }

                    // Total/balance row
                    var totalRow = Entries.Count + 2;
                    worksheet.Cell(totalRow, 1).Value = "الرصيد النهائي";
                    worksheet.Cell(totalRow, 6).Value = RunningBalance;
                    worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(totalRow, 6).Style.Font.Bold = true;

                    // Format header
                    var headerRange = worksheet.Range(1, 1, 1, 6);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير كشف الحساب إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير كشف الحساب إلى Excel", "AccountStatementViewModel.ExportToExcel", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
