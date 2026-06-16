namespace SalesSystem.Contracts.Enums;

public enum InvoiceStatus : byte { Draft = 1, Posted = 2, Cancelled = 3 }

public enum PaymentType : byte { Cash = 1, Credit = 2, Mixed = 3 }

public enum MovementType : byte
{
    PurchaseIn = 1,
    SaleOut = 2,
    SaleReturnIn = 3,
    PurchaseReturnOut = 4,
    TransferOut = 5,
    TransferIn = 6,
    Adjustment = 7
}

public enum SaleMode : byte { Retail = 1, Wholesale = 2 }

/// <summary>
/// نوع الخصم: مبلغ أو نسبة مئوية
/// </summary>
public enum DiscountType : byte { Amount = 0, Percentage = 1 }

/// <summary>
/// أنواع معاملات الخزينة النقدية (للتوافق مع التقارير المالية القديمة — سيتم استبدالها بـ ReceiptVoucher/PaymentVoucher)
/// </summary>
public enum CashTransactionType : byte
{
    OpeningBalance = 1,
    SalesIncome = 2,
    Expense = 3,
    TransferOut = 4,
    TransferIn = 5,
    RefundOut = 6,
    SupplierPayment = 7,
    CustomerPayment = 8
}
