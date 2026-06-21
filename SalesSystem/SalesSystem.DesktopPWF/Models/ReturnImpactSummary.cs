using System;

namespace SalesSystem.DesktopPWF.Models;

public class ReturnImpactSummary
{
    public decimal TotalReturnAmount { get; set; }
    public decimal StockQuantityImpact { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal BalanceImpact { get; set; }
    public string CounterpartyName { get; set; } = string.Empty;
    public string CounterpartyType { get; set; } = "العميل"; // "العميل" or "المورد"
    public decimal TaxImpact { get; set; }
    
    /// <summary>إجمالي الخصم المتناسب من الفاتورة الأصلية</summary>
    public decimal ReturnedDiscountAmount { get; set; }
    
    /// <summary>إجمالي الضريبة المتناسبة من الفاتورة الأصلية</summary>
    public decimal ReturnedTaxAmount { get; set; }
    
    /// <summary>إجمالي الرسوم المتناسبة من الفاتورة الأصلية</summary>
    public decimal ReturnedChargeAmount { get; set; }
    
    public bool HasImpact => TotalReturnAmount > 0 || StockQuantityImpact > 0;
}
