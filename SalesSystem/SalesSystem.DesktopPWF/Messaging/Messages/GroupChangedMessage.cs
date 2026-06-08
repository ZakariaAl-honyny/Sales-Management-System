namespace SalesSystem.DesktopPWF.Messaging.Messages;

/// <summary>
/// Published when a customer group is created, updated, or deleted.
/// Carries only the entity ID — no data payload (RULE-034).
/// </summary>
public record GroupChangedMessage(int GroupId);
