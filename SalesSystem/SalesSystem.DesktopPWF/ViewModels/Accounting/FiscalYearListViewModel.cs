using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Accounting;

/// <summary>
/// ViewModel for managing fiscal years.
/// Supports Create, Open, Close operations with confirmation dialogs.
/// RULE-059: All buttons always enabled — validates/confirms on click.
/// RULE-220: Newest-first sorting.
/// </summary>
public class FiscalYearListViewModel : ViewModelBase, IDisposable
{
    private readonly IFiscalYearApiService _fiscalYearService;
    private readonly IDialogService _dialogService;

    private ObservableCollection<FiscalYearItemViewModel> _years = new();
    private FiscalYearItemViewModel? _selectedYear;
    private bool _isEmpty;
    private string? _errorMessage;

    public FiscalYearListViewModel()
        : this(
            App.GetService<IFiscalYearApiService>(),
            App.GetService<IDialogService>())
    {
    }

    public FiscalYearListViewModel(
        IFiscalYearApiService fiscalYearService,
        IDialogService dialogService)
    {
        _fiscalYearService = fiscalYearService ?? throw new ArgumentNullException(nameof(fiscalYearService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        SetDialogService(dialogService);

        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadYearsOperationAsync)));
        CreateYearCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(CreateYearOperationAsync)));
        OpenYearCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(OpenYearOperationAsync)));
        CloseYearCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(CloseYearOperationAsync)));
        CloseCommand = new RelayCommand(RequestClose);

        _ = ExecuteAsync(LoadYearsOperationAsync);
    }

    #region Properties

    public ObservableCollection<FiscalYearItemViewModel> Years
    {
        get => _years;
        set => SetProperty(ref _years, value);
    }

    public FiscalYearItemViewModel? SelectedYear
    {
        get => _selectedYear;
        set => SetProperty(ref _selectedYear, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasNoYears => Years.Count == 0;

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand CreateYearCommand { get; private set; } = null!;
    public ICommand OpenYearCommand { get; private set; } = null!;
    public ICommand CloseYearCommand { get; private set; } = null!;
    public ICommand CloseCommand { get; private set; } = null!;

    #endregion

    #region Operations

    private async Task LoadYearsOperationAsync()
    {
        ErrorMessage = null;
        var result = await _fiscalYearService.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Years.Clear();
                // RULE-220: Newest first
                foreach (var dto in result.Value.OrderByDescending(x => x.Year))
                {
                    Years.Add(new FiscalYearItemViewModel(dto));
                }

                IsEmpty = Years.Count == 0;
                OnPropertyChanged(nameof(HasNoYears));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل السنوات المالية", "LoadFiscalYears");
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
            IsEmpty = Years.Count == 0;
            OnPropertyChanged(nameof(HasNoYears));
        }
    }

    private async Task CreateYearOperationAsync()
    {
        // Determine the next fiscal year
        var nextYear = Years.Count > 0
            ? Years.Max(y => y.Year) + 1
            : DateTime.Now.Year;

        // Confirm with user
        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد إنشاء سنة مالية",
            $"سيتم إنشاء السنة المالية {nextYear}.\n" +
            $"تاريخ البداية: {new DateTime(nextYear, 1, 1):yyyy/MM/dd}\n" +
            $"تاريخ النهاية: {new DateTime(nextYear, 12, 31):yyyy/MM/dd}\n\n" +
            $"هل تريد المتابعة؟");

        if (!confirmed) return;

        var request = new CreateFiscalYearRequest(nextYear);
        var result = await _fiscalYearService.CreateAsync(request);

        if (result.IsSuccess)
        {
            await _dialogService.ShowSuccessAsync("تم الإنشاء",
                $"تم إنشاء السنة المالية {nextYear} بنجاح.");
            _ = ExecuteAsync(LoadYearsOperationAsync);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في إنشاء السنة المالية", "CreateFiscalYear");
            await _dialogService.ShowErrorAsync("خطأ في إنشاء السنة المالية", ErrorMessage!);
        }
    }

    private async Task OpenYearOperationAsync()
    {
        if (SelectedYear == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "يرجى تحديد سنة مالية من القائمة.");
            return;
        }

        if (SelectedYear.IsOpen)
        {
            await _dialogService.ShowWarningAsync("تنبيه", $"السنة المالية {SelectedYear.Year} مفتوحة بالفعل.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد فتح السنة",
            $"سيتم فتح السنة المالية {SelectedYear.Year}.\nهل تريد المتابعة؟");

        if (!confirmed) return;

        var result = await _fiscalYearService.OpenAsync(SelectedYear.Id);

        if (result.IsSuccess)
        {
            await _dialogService.ShowSuccessAsync("تم الفتح",
                $"تم فتح السنة المالية {SelectedYear.Year} بنجاح.");
            _ = ExecuteAsync(LoadYearsOperationAsync);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في فتح السنة المالية", "OpenFiscalYear");
            await _dialogService.ShowErrorAsync("خطأ في فتح السنة المالية", ErrorMessage!);
        }
    }

    private async Task CloseYearOperationAsync()
    {
        if (SelectedYear == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "يرجى تحديد سنة مالية من القائمة.");
            return;
        }

        if (!SelectedYear.IsOpen)
        {
            await _dialogService.ShowWarningAsync("تنبيه", $"السنة المالية {SelectedYear.Year} مغلقة بالفعل.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد إغلاق السنة",
            $"سيتم إغلاق السنة المالية {SelectedYear.Year}.\n\n" +
            "ملاحظة: لن تتمكن من إضافة قيود جديدة بعد الإغلاق.\n" +
            "يمكن إعادة فتح السنة لاحقاً.\n\n" +
            "هل تريد المتابعة؟");

        if (!confirmed) return;

        var result = await _fiscalYearService.CloseAsync(SelectedYear.Id);

        if (result.IsSuccess)
        {
            await _dialogService.ShowSuccessAsync("تم الإغلاق",
                $"تم إغلاق السنة المالية {SelectedYear.Year} بنجاح.");
            _ = ExecuteAsync(LoadYearsOperationAsync);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في إغلاق السنة المالية", "CloseFiscalYear");
            await _dialogService.ShowErrorAsync("خطأ في إغلاق السنة المالية", ErrorMessage!);
        }
    }

    #endregion

    #region Cleanup

    public override void Cleanup()
    {
        Years.Clear();
        base.Cleanup();
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Wrapper ViewModel for a single fiscal year row in the list.
/// </summary>
public class FiscalYearItemViewModel : ViewModelBase
{
    private readonly FiscalYearDto _dto;

    public FiscalYearItemViewModel(FiscalYearDto dto)
    {
        _dto = dto;
    }

    public int Id => _dto.Id;
    public int Year => _dto.Year;
    public DateTime StartDate => _dto.StartDate;
    public DateTime EndDate => _dto.EndDate;
    public bool IsOpen => _dto.IsOpen;
    public DateTime? OpenedAt => _dto.OpenedAt;
    public int? OpenedByUserId => _dto.OpenedByUserId;
    public DateTime? ClosedAt => _dto.ClosedAt;
    public int? ClosedByUserId => _dto.ClosedByUserId;

    public string YearDisplay => $"السنة المالية {Year}";

    public string DateRangeDisplay => $"{StartDate:yyyy/MM/dd} — {EndDate:yyyy/MM/dd}";

    public string StatusDisplay => IsOpen ? "مفتوحة" : "مغلقة";

    public string StatusToolTip => IsOpen
        ? "السنة المالية مفتوحة ويمكن إضافة قيود جديدة"
        : "السنة المالية مغلقة ولا يمكن إضافة قيود جديدة";
}
