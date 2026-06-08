namespace SalesSystem.DesktopPWF.Messaging.Messages;

/// <summary>
/// Published when a purchase order is created, updated, posted, or cancelled.
/// Carries only the entity ID — no data payload (RULE-034).
/// </summary>
public record PurchaseOrderChangedMessage(int OrderId);
