namespace SalesSystem.Desktop.Messaging.Messages;

public record StockChangedMessage(int ProductId, int? WarehouseId = null);

