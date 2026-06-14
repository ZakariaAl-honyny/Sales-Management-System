using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Accounting;

/// <summary>
/// Editor ViewModel for creating or editing a fiscal year.
/// RULE-059: Save button always enabled — validates on click with warning dialog.
/// Uses INotifyDataErrorInfo for real-time validation.
/// </summary>
public class FiscalYearEditorViewModel : ViewModelBase
{
    private readonly IFiscalYearApiService _fiscalYearApi;
    private readonly IDialogService _dialogService;
    private readonly int? _editId;

    /// <summary>
    /// Raised when the entity is saved successfully.
    /// </summary>
    public event Action? OnSaved;

    /// <summary>
    /// Parameterless constructor for design-time / DI.
    /// </summary>
    public FiscalYearEditorViewModel()
        : this(
            App.GetService<IFiscalYearApiService>(),
            App.GetService<IDialogService>())
    {
    }

    /// <summary>
    /// Constructor for creating a new fiscal year.
    /// </summary>
    public FiscalYearEditorViewModel(
        IFiscalYearApiService fiscalYearApi,
        IDialogService dialogService)
        : this(fiscalYearApi, dialogService, null)
    {
    }

    /// <summary>
    /// Constructor for editing an existing fiscal year.
    /// </summary>
    public FiscalYearEditorViewModel(
        IFiscalYearApiService fiscalYearApi,
        IDialogService dialogService,
        FiscalYearDto? existing)
    {
        _fiscalYearApi = fiscalYearApi ?? throw new ArgumentNullException(nameof(fiscalYearApi));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        SetDialogService(dialogService);

        if (existing != null)
        {
            _editId = existing.Id;
            _year = existing.Year;
            _startDate = existing.StartDate;
            _endDate = existing.EndDate;
            IsEditMode = true;
        }
        else
        {
            // Default: next year
            _year = DateTime.Now.Year;
            _startDate = new DateTime(DateTime.Now.Year, 1, 1);
            _endDate = new DateTime(DateTime.Now.Year, 12, 31);
        }

        SaveCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync)));
        CancelCommand = new RelayCommand(RequestClose);
    }

    // ═══════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    // ═══════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════

    private int _year;
    public int Year
    {
        get => _year;
        set
        {
            if (SetProperty(ref _year, value))
            {
                ClearErrors(nameof(Year));
                if (value <= 0)
                    AddError(nameof(Year), "السنة يجب أن تكون أكبر من صفر");
            }
        }
    }

    private DateTime _startDate;
    public DateTime StartDate
    {
        get => _startDate;
        set
        {
            if (SetProperty(ref _startDate, value))
            {
                ClearErrors(nameof(StartDate));
                ClearErrors(nameof(EndDate));
                if (value >= EndDate)
                    AddError(nameof(StartDate), "تاريخ البداية يجب أن يكون قبل تاريخ النهاية");
            }
        }
    }

    private DateTime _endDate;
    public DateTime EndDate
    {
        get => _endDate;
        set
        {
            if (SetProperty(ref _endDate, value))
            {
                ClearErrors(nameof(EndDate));
                ClearErrors(nameof(StartDate));
                if (value <= StartDate)
                    AddError(nameof(EndDate), "تاريخ النهاية يجب أن يكون بعد تاريخ البداية");
            }
        }
    }

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string Title => IsEditMode ? "تعديل السنة المالية" : "إضافة سنة مالية جديدة";

    // ═══════════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════════

    private bool Validate()
    {
        ClearAllErrors();

        if (Year <= 0)
            AddError(nameof(Year), "السنة يجب أن تكون أكبر من صفر");
        if (StartDate == default)
            AddError(nameof(StartDate), "تاريخ البداية مطلوب");
        if (EndDate == default)
            AddError(nameof(EndDate), "تاريخ النهاية مطلوب");
        if (StartDate >= EndDate)
            AddError(nameof(StartDate), "تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

        return !HasErrors;
    }

    // ═══════════════════════════════════════════════════════════════
    // Operations
    // ═══════════════════════════════════════════════════════════════

    private async Task SaveOperationAsync()
    {
        ErrorMessage = null;

        if (!Validate())
        {
            await ValidateAllAsync();
            return;
        }

        if (IsEditMode && _editId.HasValue)
        {
            // For fiscal years, update is not directly supported via API.
            // The list ViewModel handles open/close instead.
            await _dialogService.ShowWarningAsync("ملاحظة",
                "لا يمكن تعديل السنوات المالية بعد الإنشاء. يمكنك فتح أو إغلاق السنة المالية من شاشة القائمة.");
            RequestClose();
        }
        else
        {
            var request = new CreateFiscalYearRequest(Year);
            var result = await _fiscalYearApi.CreateAsync(request);

            if (result.IsSuccess)
            {
                await _dialogService.ShowSuccessAsync("تم", $"تم إنشاء السنة المالية {Year} بنجاح");
                OnSaved?.Invoke();
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إنشاء السنة المالية", "FiscalYearEditorViewModel.Create");
            }
        }
    }
}
