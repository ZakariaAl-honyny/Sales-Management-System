namespace SalesSystem.DesktopPWF.Messaging.Messages;

/// <summary>
/// Published when an additional fee is created, updated, or deleted.
/// Carries only the entity ID and parent invoice ID — no data payload (RULE-034).
/// </summary>
public record AdditionalFeeChangedMessage(int FeeId, int PurchaseInvoiceId);
