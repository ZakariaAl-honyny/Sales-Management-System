using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SalesSystem.DesktopPWF.Models;

/// <summary>
/// Represents an unpaid invoice that can receive partial payment allocation.
/// Used in Customer Receipt and Supplier Payment editors for multi-invoice settlement.
/// </summary>
public class UnpaidInvoiceAllocationItem : INotifyPropertyChanged
{
    private decimal _allocatedAmount;

    public int InvoiceId { get; set; }
    public int InvoiceNo { get; set; }
    public string? InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    /// <summary>Remaining amount = TotalAmount - PaidAmount (before current allocation)</summary>
    public decimal RemainingAmount => TotalAmount - PaidAmount;

    /// <summary>Amount to allocate from this receipt/payment</summary>
    public decimal AllocatedAmount
    {
        get => _allocatedAmount;
        set
        {
            if (_allocatedAmount != value)
            {
                _allocatedAmount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFullyAllocated));
                OnPropertyChanged(nameof(RemainingAfterAllocation));
            }
        }
    }

    /// <summary>Amount remaining on invoice after this allocation</summary>
    public decimal RemainingAfterAllocation => RemainingAmount - AllocatedAmount;

    /// <summary>True when AllocatedAmount >= RemainingAmount</summary>
    public bool IsFullyAllocated => AllocatedAmount >= RemainingAmount;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Represents an unpaid purchase invoice for supplier payment allocation.
/// </summary>
public class UnpaidPurchaseInvoiceLine : UnpaidInvoiceAllocationItem
{
}
