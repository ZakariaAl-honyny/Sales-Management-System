namespace SalesSystem.Contracts.Common;

public static class ErrorCodes
{
    public const string NotFound = "NOT_FOUND";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string DuplicateEntry = "DUPLICATE_ENTRY";
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string InvalidOperation = "INVALID_OPERATION";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string DuplicateBarcode = "DUPLICATE_BARCODE";
    public const string RequiresPasswordSetup = "REQUIRES_PASSWORD_SETUP";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string InvalidToken = "INVALID_RESET_TOKEN";
    public const string ReferencedByOtherEntities = "REFERENCED_BY_OTHER_ENTITIES";
}
