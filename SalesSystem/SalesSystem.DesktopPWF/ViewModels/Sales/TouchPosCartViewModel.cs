using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Sales;

/// <summary>
/// ViewModel for the Touch POS cart management panel (left side).
/// Manages cart items, quantity adjustments, and item removal.
/// Shares the ObservableCollection of InvoiceLineViewModel items with the parent editor.
/// </summary>
public class TouchPosCartViewModel : ViewModelBase
{
    private readonly Action? _recalculateTotals;
    private readonly ObservableCollection<InvoiceLineViewModel> _cartItems;

    /// <summary>
    /// Delegate to invoke when Cash checkout is requested.
    /// Parameter is the paid amount string from the numpad.
    /// </summary>
    public Action<string>? OnCashCheckout { get; set; }

    /// <summary>
    /// Delegate to invoke when Card checkout is requested.
    /// Parameter is the paid amount string from the numpad.
    /// </summary>
    public Action<string>? OnCardCheckout { get; set; }

    /// <summary>
    /// Delegate to invoke when saving as Draft is requested.
    /// </summary>
    public Action? OnDraftSave { get; set; }

    private decimal _subTotal;
    private decimal _totalAmount;
    private string _paidAmountString = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="TouchPosCartViewModel"/> class.
    /// </summary>
    /// <param name="cartItems">
    /// The shared ObservableCollection of invoice line items from the parent editor.
    /// This is the same collection used by SalesInvoiceEditorViewModel.Items.
    /// </param>
    /// <param name="recalculateTotals">
    /// Optional callback to invoke after any cart modification to recalculate invoice totals
    /// in the parent ViewModel (SalesInvoiceEditorViewModel.RecalculateTotals).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when cartItems is null.</exception>
    public TouchPosCartViewModel(
        ObservableCollection<InvoiceLineViewModel> cartItems,
        Action? recalculateTotals = null)
    {
        _cartItems = cartItems ?? throw new System.ArgumentNullException(nameof(cartItems));
        _recalculateTotals = recalculateTotals;

        IncreaseQtyCommand = new RelayCommand(IncreaseQty);
        DecreaseQtyCommand = new RelayCommand(DecreaseQty);
        RemoveItemCommand = new RelayCommand(RemoveItem);
        CashCheckoutCommand = new RelayCommand(CashCheckout);
        CardCheckoutCommand = new RelayCommand(CardCheckout);
        DraftCommand = new RelayCommand(DraftSave);

        // Subscribe to collection changes to recalculate when items are added/removed
        // from outside this ViewModel (e.g., from product selection or barcode scanning).
        _cartItems.CollectionChanged += OnCartItemsCollectionChanged;

        // Calculate initial totals from any pre-existing items.
        Recalculate();
    }

    #region Properties

    /// <summary>
    /// Gets the shared cart items collection. This is the same reference as
    /// SalesInvoiceEditorViewModel.Items — modifications here are reflected in the parent.
    /// </summary>
    public ObservableCollection<InvoiceLineViewModel> CartItems => _cartItems;

    /// <summary>
    /// Gets the subtotal computed as the sum of all line totals (Quantity × UnitPrice − Discount).
    /// Updated automatically when items or quantities change.
    /// </summary>
    public decimal SubTotal
    {
        get => _subTotal;
        private set => SetProperty(ref _subTotal, value);
    }

    /// <summary>
    /// Gets the total amount for the cart display.
    /// In the cart panel this mirrors SubTotal since tax and invoice-level
    /// discounts are handled by the parent ViewModel.
    /// </summary>
    public decimal TotalAmount
    {
        get => _totalAmount;
        private set => SetProperty(ref _totalAmount, value);
    }

    /// <summary>
    /// Gets or sets the paid amount string entered via the numpad.
    /// Used by checkout commands (Cash/Card) to pass the entered amount.
    /// </summary>
    public string PaidAmountString
    {
        get => _paidAmountString;
        set => SetProperty(ref _paidAmountString, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the command to increase the quantity of a cart item by 1.
    /// Takes an <see cref="InvoiceLineViewModel"/> as command parameter.
    /// Quantity will never exceed practical limits (enforced by the line ViewModel).
    /// </summary>
    public ICommand IncreaseQtyCommand { get; }

    /// <summary>
    /// Gets the command to decrease the quantity of a cart item by 1.
    /// Takes an <see cref="InvoiceLineViewModel"/> as command parameter.
    /// Quantity will never go below 1.
    /// </summary>
    public ICommand DecreaseQtyCommand { get; }

    /// <summary>
    /// Gets the command to remove an item from the cart entirely.
    /// Takes an <see cref="InvoiceLineViewModel"/> as command parameter.
    /// The item is removed from the shared <see cref="CartItems"/> collection.
    /// </summary>
    public ICommand RemoveItemCommand { get; }

    /// <summary>
    /// Gets the command to complete checkout as Cash payment.
    /// Uses the current <see cref="PaidAmountString"/> value.
    /// </summary>
    public ICommand CashCheckoutCommand { get; }

    /// <summary>
    /// Gets the command to complete checkout as Card payment.
    /// Uses the current <see cref="PaidAmountString"/> value.
    /// </summary>
    public ICommand CardCheckoutCommand { get; }

    /// <summary>
    /// Gets the command to save the current cart as a Draft invoice.
    /// </summary>
    public ICommand DraftCommand { get; }

    #endregion

    #region Command Handlers

    private void IncreaseQty(object? parameter)
    {
        if (parameter is InvoiceLineViewModel line)
        {
            line.Quantity += 1;
            Recalculate();
        }
    }

    private void DecreaseQty(object? parameter)
    {
        if (parameter is InvoiceLineViewModel line && line.Quantity > 1)
        {
            line.Quantity -= 1;
            Recalculate();
        }
    }

    private void RemoveItem(object? parameter)
    {
        if (parameter is InvoiceLineViewModel line && _cartItems.Contains(line))
        {
            _cartItems.Remove(line);
            // Recalculate is called by OnCartItemsCollectionChanged on removal,
            // but we call it explicitly here to keep the pattern consistent.
            Recalculate();
        }
    }

    private void CashCheckout(object? parameter)
    {
        OnCashCheckout?.Invoke(PaidAmountString);
    }

    private void CardCheckout(object? parameter)
    {
        OnCardCheckout?.Invoke(PaidAmountString);
    }

    private void DraftSave(object? parameter)
    {
        OnDraftSave?.Invoke();
    }

    #endregion

    #region Collection Change Handling

    private void OnCartItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // When items are added or removed externally, recalculate totals.
        Recalculate();
    }

    #endregion

    #region Recalculation

    /// <summary>
    /// Recalculates <see cref="SubTotal"/> and <see cref="TotalAmount"/> from the current
    /// cart items, then invokes the parent's recalculate callback.
    /// Call this after any modification to cart items (quantity change, add, remove).
    /// </summary>
    public void Recalculate()
    {
        SubTotal = _cartItems.Sum(i => i.LineTotal);
        TotalAmount = SubTotal;
        _recalculateTotals?.Invoke();
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Releases resources and unsubscribes from the cart items collection change event
    /// to prevent memory leaks. Called by ScreenWindowService when the window is closed.
    /// </summary>
    public override void Cleanup()
    {
        _cartItems.CollectionChanged -= OnCartItemsCollectionChanged;
        base.Cleanup();
    }

    #endregion
}
