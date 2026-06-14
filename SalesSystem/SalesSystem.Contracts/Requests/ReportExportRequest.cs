namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request model for exporting reports to Excel or PDF format.
/// </summary>
public record ReportExportRequest(
    string ReportType,
    string Format,
    string? ReportName = null,
    Dictionary<string, string>? Filters = null
);
