namespace SalesSystem.Contracts.Responses;

public record CashTransactionDto(
    int Id,
    int CashBoxId,
    byte TransactionType,
    decimal Amount,
    decimal RunningBalance,
    string? ReferenceType,
    int? ReferenceId,
    int? CurrencyId,
    string? Notes,
    int CreatedBy,
    DateTime CreatedAt
)
{
    public string TransactionTypeName => TransactionType switch
    {
        1 => "الرصيد الافتتاحي",
        2 => "مبيعات",
        3 => "مصروفات",
        4 => "تحويل صادر",
        5 => "تحويل وارد",
        6 => "مرتجع مبيعات",
        7 => "دفع مورد",
        8 => "دفع عميل",
        _ => "غير معروف"
    };
}
