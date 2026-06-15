using System.Windows.Input;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.ViewModels.JournalEntries;

/// <summary>
/// ViewModel for a single journal entry line (debit/credit pair for an account).
/// Supports INotifyDataErrorInfo via ViewModelBase and notifies parent of totals changes.
/// </summary>
public class JournalEntryLineViewModel : ViewModelBase
{
    private readonly JournalEntryEditorViewModel _parent;
    private int _accountId;
    private string _accountCode = string.Empty;
    private string _accountNameAr = string.Empty;
    private decimal _debit;
    private decimal _credit;
    private string? _description;
    private bool _isSelected;

    public JournalEntryLineViewModel(JournalEntryEditorViewModel parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        RemoveCommand = new RelayCommand(() => _parent.RemoveLine(this));
    }

    /// <summary>
    /// Creates a line ViewModel from an existing JournalEntryLineDetailDto (for edit mode).
    /// </summary>
    public static JournalEntryLineViewModel FromDto(JournalEntryLineDetailDto dto, JournalEntryEditorViewModel parent)
    {
        return new JournalEntryLineViewModel(parent)
        {
            _accountId = dto.AccountId,
            _accountCode = dto.AccountCode,
            _accountNameAr = dto.AccountNameAr,
            _debit = dto.Debit,
            _credit = dto.Credit,
            _description = dto.Description
        };
    }

    #region Properties

    public int AccountId
    {
        get => _accountId;
        set
        {
            if (SetProperty(ref _accountId, value))
                OnPropertyChanged(nameof(AccountDisplay));
        }
    }

    public string AccountCode
    {
        get => _accountCode;
        set
        {
            if (SetProperty(ref _accountCode, value))
                OnPropertyChanged(nameof(AccountDisplay));
        }
    }

    public string AccountNameAr
    {
        get => _accountNameAr;
        set
        {
            if (SetProperty(ref _accountNameAr, value))
                OnPropertyChanged(nameof(AccountDisplay));
        }
    }

    /// <summary>
    /// Combined display for account in DataGrid columns.
    /// </summary>
    public string AccountDisplay => string.IsNullOrWhiteSpace(AccountCode)
        ? AccountNameAr
        : $"{AccountCode} - {AccountNameAr}";

    public decimal Debit
    {
        get => _debit;
        set
        {
            if (SetProperty(ref _debit, value))
            {
                ClearErrors(nameof(Debit));
                if (value < 0)
                    AddError(nameof(Debit), "المبلغ لا يمكن أن يكون سالباً");
                if (value > 0 && _credit > 0)
                    AddError(nameof(Debit), "لا يمكن إدخال مدين ودائن معاً في نفس البند");
                _parent.NotifyTotalsChanged();
            }
        }
    }

    public decimal Credit
    {
        get => _credit;
        set
        {
            if (SetProperty(ref _credit, value))
            {
                ClearErrors(nameof(Credit));
                if (value < 0)
                    AddError(nameof(Credit), "المبلغ لا يمكن أن يكون سالباً");
                if (value > 0 && _debit > 0)
                    AddError(nameof(Credit), "لا يمكن إدخال مدين ودائن معاً في نفس البند");
                _parent.NotifyTotalsChanged();
            }
        }
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Formatted debit for display.
    /// </summary>
    public string DebitFormatted => Debit.ToString("N2");

    /// <summary>
    /// Formatted credit for display.
    /// </summary>
    public string CreditFormatted => Credit.ToString("N2");

    #endregion

    #region Commands

    public ICommand RemoveCommand { get; private set; }

    #endregion
}
