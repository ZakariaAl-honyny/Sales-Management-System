using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.DocumentSequences;

/// <summary>
/// ViewModel for managing document sequences.
/// Shows all document types with their current next number.
/// Supports resetting the next number (Admin only).
/// RULE-059: All buttons always enabled — validates/confirms on click.
/// RULE-220: Newest-first sorting.
/// </summary>
public class DocumentSequenceListViewModel : ViewModelBase, IDisposable
{
    private readonly IDocumentSequenceApiService _sequenceApi;
    private readonly IDialogService _dialogService;

    private ObservableCollection<DocumentSequenceDto> _sequences = new();
    private DocumentSequenceDto? _selectedSequence;
    private bool _isEmpty;
    private string? _errorMessage;
    private int _resetValue;

    public DocumentSequenceListViewModel()
        : this(
            App.GetService<IDocumentSequenceApiService>(),
            App.GetService<IDialogService>())
    {
    }

    public DocumentSequenceListViewModel(
        IDocumentSequenceApiService sequenceApi,
        IDialogService dialogService)
    {
        _sequenceApi = sequenceApi ?? throw new ArgumentNullException(nameof(sequenceApi));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        SetDialogService(dialogService);

        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadSequencesOperationAsync)));
        ResetCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(ResetSequenceOperationAsync)));
        CloseCommand = new RelayCommand(RequestClose);

        _ = ExecuteAsync(LoadSequencesOperationAsync);
    }

    // ═══════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════

    public ObservableCollection<DocumentSequenceDto> Sequences
    {
        get => _sequences;
        set => SetProperty(ref _sequences, value);
    }

    public DocumentSequenceDto? SelectedSequence
    {
        get => _selectedSequence;
        set
        {
            if (SetProperty(ref _selectedSequence, value))
            {
                if (value != null)
                    ResetValue = value.NextNumber;
                OnPropertyChanged(nameof(HasSelection));
            }
        }
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

    public int ResetValue
    {
        get => _resetValue;
        set => SetProperty(ref _resetValue, value);
    }

    public bool HasSelection => SelectedSequence != null;
    public bool HasNoSequences => Sequences.Count == 0;

    // ═══════════════════════════════════════════════════════════════
    // Commands
    // ═══════════════════════════════════════════════════════════════

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand ResetCommand { get; private set; } = null!;
    public ICommand CloseCommand { get; private set; } = null!;

    // ═══════════════════════════════════════════════════════════════
    // Operations
    // ═══════════════════════════════════════════════════════════════

    private async Task LoadSequencesOperationAsync()
    {
        ErrorMessage = null;
        var result = await _sequenceApi.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Sequences.Clear();
                foreach (var dto in result.Value.OrderByDescending(x => x.Id))
                {
                    Sequences.Add(dto);
                }
                IsEmpty = Sequences.Count == 0;
                OnPropertyChanged(nameof(HasNoSequences));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تسلسل المستندات", "DocumentSequenceListViewModel.Load");
            IsEmpty = Sequences.Count == 0;
            OnPropertyChanged(nameof(HasNoSequences));
        }
    }

    private async Task ResetSequenceOperationAsync()
    {
        if (SelectedSequence == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "يرجى تحديد تسلسل مستند من القائمة.");
            return;
        }

        if (ResetValue < 0)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرقم التالي يجب أن يكون 0 أو أكبر.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد إعادة تعيين التسلسل",
            $"سيتم إعادة تعيين الرقم التالي لتسلسل '{SelectedSequence.DocumentType}' من {SelectedSequence.NextNumber} إلى {ResetValue}.\n\n" +
            $"هل تريد المتابعة؟");

        if (!confirmed) return;

        var request = new UpdateDocumentSequenceRequest(SelectedSequence.Id, ResetValue);
        var result = await _sequenceApi.UpdateAsync(SelectedSequence.Id, request);

        if (result.IsSuccess)
        {
            await _dialogService.ShowSuccessAsync("تم", $"تم إعادة تعيين تسلسل '{SelectedSequence.DocumentType}' بنجاح.");
            _ = ExecuteAsync(LoadSequencesOperationAsync);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في إعادة تعيين التسلسل", "DocumentSequenceListViewModel.Reset");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Cleanup
    // ═══════════════════════════════════════════════════════════════

    public override void Cleanup()
    {
        Sequences.Clear();
        base.Cleanup();
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }
}
