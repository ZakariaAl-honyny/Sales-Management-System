namespace SalesSystem.DesktopPWF.Messaging.Messages;

/// <summary>
/// Published when a product category is created, updated, or deactivated.
/// Carries only the entity ID — no data payload (RULE-034).
/// </summary>
public record ProductCategoryChangedMessage(int CategoryId);
