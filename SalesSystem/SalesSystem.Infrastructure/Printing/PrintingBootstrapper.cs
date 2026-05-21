using QuestPDF.Infrastructure;

namespace SalesSystem.Infrastructure.Printing;

/// <summary>
/// Initializes QuestPDF license at application startup.
/// Call ONCE before any PDF generation.
/// </summary>
public static class PrintingBootstrapper
{
    public static void Initialize()
    {
        // QuestPDF Community license (free for revenue under $1M USD)
        QuestPDF.Settings.License = LicenseType.Community;
    }
}
