namespace SalesSystem.DesktopPWF.Messaging.Messages;

/// <summary>
/// Published when a Bill of Material (assembly component) is created, updated, or deleted.
/// Carries the BOM ID only — NO data payload (RULE-034).
/// </summary>
public record BillOfMaterialChangedMessage(int BillOfMaterialId);
