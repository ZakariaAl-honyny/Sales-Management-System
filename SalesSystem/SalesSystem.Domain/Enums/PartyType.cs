namespace SalesSystem.Domain.Enums;

/// <summary>
/// Indicates whether a Party is a Customer or a Supplier.
/// Used by the unified Parties table introduced in v4.10.
/// </summary>
public enum PartyType : byte
{
    Customer = 1,
    Supplier = 2
}
