namespace SalesSystem.Infrastructure.Printing;

/// <summary>
/// Exception for printer-related errors.
/// Message is in Arabic for user-facing display.
/// </summary>
public class PrinterException : Exception
{
    public PrinterException(string message) : base(message) { }
}
