using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.JournalEntries;

/// <summary>
/// ViewModel for the Journal Entries list screen.
/// Displays all journal entries with status indicators and supports viewing details.
/// </summary>
public class JournalEntriesListViewModel : ViewModelBase, IDisposable
{
    private readonly IJournalEntryApiService _journalEntryService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    public JournalEntriesListViewModel()
    {
        _journalEntryService = App.GetService<IJournalEntryApiService>();
        _dialogService = App.GetService<IDialogService>();
        _eventBus = App.GetService<IEventBus>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        // Subscribe to EventBus (RULE-012: unsubscribe in Cleanup)
        _eventBus.Subscribe<JournalEntryChangedMessage>(OnJournalEntryChanged);

        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadEntriesOperationAsync)));
        ViewDetailsCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(ViewDetailsOperationAsync)));
        CloseCommand = new RelayCommand(RequestClose);
    }

    // ── Observable Properties ──

    private ObservableCollection<JournalEntryItemViewModel> _entries = new();
    public ObservableCollection<JournalEntryItemViewModel> Entries
    {
        get => _entries;
        set => SetProperty(ref _entries, value);
    }

    private JournalEntryItemViewModel? _selectedEntry;
    public JournalEntryItemViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value);
    }

    private bool _isEmpty;
    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Computed property indicating no entries are loaded.
    /// </summary>
    public bool HasNoEntries => Entries.Count == 0;

    // ── Commands ──
    // RULE-059: NO CanExecute predicates — buttons always enabled

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand ViewDetailsCommand { get; private set; } = null!;
    public ICommand CloseCommand { get; private set; } = null!;

    // ── Operations ──

    private async Task LoadEntriesOperationAsync()
    {
        ErrorMessage = null;

        var result = await _journalEntryService.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Entries.Clear();

                // RULE-220: Newest-first sorting
                foreach (var dto in result.Value.OrderByDescending(x => x.Id))
                {
                    Entries.Add(new JournalEntryItemViewModel(dto));
                }

                IsEmpty = Entries.Count == 0;
                OnPropertyChanged(nameof(HasNoEntries));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل القيود اليومية", "LoadJournalEntries");
            IsEmpty = Entries.Count == 0;
            OnPropertyChanged(nameof(HasNoEntries));
        }
    }

    private async Task ViewDetailsOperationAsync()
    {
        if (SelectedEntry == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "يرجى تحديد قيد لعرض التفاصيل.");
            return;
        }

        var result = await _journalEntryService.GetByIdAsync(SelectedEntry.Id);

        if (result.IsSuccess && result.Value != null)
        {
            var detail = result.Value;
            var lines = detail.Lines ?? new List<JournalEntryLineDetailDto>();

            var detailsMessage = string.Join("\n",
                lines.Select(l =>
                    $"    {l.AccountCode} - {l.AccountNameAr}\n" +
                    $"    مدين: {l.Debit:N2}    دائن: {l.Credit:N2}" +
                    (string.IsNullOrEmpty(l.Description) ? "" : $"    ({l.Description})")
                ));

            var message = $"رقم القيد: {detail.EntryNumber}\n" +
                          $"التاريخ: {detail.TransactionDate:yyyy/MM/dd HH:mm}\n" +
                          $"البيان: {detail.Description}\n" +
                          $"النوع: {GetEntryTypeDisplay(detail.EntryType)}\n" +
                          $"الحالة: {(detail.IsPosted ? "✅ مرحلة" : "📝 مسودة")}" +
                          (detail.IsReversed ? " (مقيدة بعكس)" : "") +
                          $"\n\nالتفاصيل:\n{detailsMessage}\n\n" +
                          $"المجموع مدين: {lines.Sum(l => l.Debit):N2}\n" +
                          $"المجموع دائن: {lines.Sum(l => l.Credit):N2}";

            await _dialogService.ShowInfoAsync("تفاصيل القيد", message);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تفاصيل القيد", "ViewJournalEntryDetails");
        }
    }

    // ── EventBus Handler ──

    private void OnJournalEntryChanged(JournalEntryChangedMessage msg)
    {
        // RULE-013: Marshal to UI thread
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = ExecuteAsync(LoadEntriesOperationAsync));
    }

    // ── Helpers ──

    /// <summary>
    /// Converts the EntryType string from the API to an Arabic display string.
    /// </summary>
    public static string GetEntryTypeDisplay(string entryType)
    {
        return entryType switch
        {
            "Manual" => "يدوي",
            "Sales" => "مبيعات",
            "Purchase" => "مشتريات",
            "SalesReturn" => "مرتجع مبيعات",
            "PurchaseReturn" => "مرتجع مشتريات",
            "Payment" => "دفعة",
            "Receipt" => "قبض",
            "OpeningBalance" => "رصيد افتتاحي",
            "Adjustment" => "تسوية",
            "Transfer" => "تحويل",
            "ExchangeRate" => "فرق عملة",
            "Closing" => "إقفال",
            "Reversal" => "عكس",
            _ => entryType // fallback to English if unknown
        };
    }

    // ── Cleanup ──

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<JournalEntryChangedMessage>(OnJournalEntryChanged);
        base.Cleanup();
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Wrapper ViewModel for a single journal entry row in the list.
/// Provides computed properties for display formatting.
/// </summary>
public class JournalEntryItemViewModel : ViewModelBase
{
    private readonly JournalEntryListDto _dto;

    public JournalEntryItemViewModel(JournalEntryListDto dto)
    {
        _dto = dto;
    }

    public int Id => _dto.Id;
    public string EntryNumber => _dto.EntryNumber;
    public DateTime TransactionDate => _dto.TransactionDate;
    public string Description => _dto.Description;
    public string EntryType => _dto.EntryType;
    public string EntryTypeDisplay => JournalEntriesListViewModel.GetEntryTypeDisplay(_dto.EntryType);
    public string? ReferenceType => _dto.ReferenceType;
    public int? ReferenceId => _dto.ReferenceId;
    public string? ReferenceNumber => _dto.ReferenceNumber;
    public decimal TotalDebit => _dto.TotalDebit;
    public decimal TotalCredit => _dto.TotalCredit;
    public bool IsPosted => _dto.IsPosted;
    public bool IsReversed => _dto.IsReversed;
    public DateTime CreatedAt => _dto.CreatedAt;
    public int? CreatedByUserId => _dto.CreatedByUserId;

    /// <summary>
    /// Arabic status text combining posted and reversed indicators.
    /// </summary>
    public string StatusDisplay
    {
        get
        {
            if (IsReversed) return "ملغي (عكس)";
            if (IsPosted) return "مرحل";
            return "مسودة";
        }
    }

    /// <summary>
    /// Debit formatted as N2.
    /// </summary>
    public string TotalDebitFormatted => $"{TotalDebit:N2}";

    /// <summary>
    /// Credit formatted as N2.
    /// </summary>
    public string TotalCreditFormatted => $"{TotalCredit:N2}";

    /// <summary>
    /// Short date format for display.
    /// </summary>
    public string TransactionDateFormatted => TransactionDate.ToString("yyyy/MM/dd");
}
