using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Thread-safe service for generating hierarchical account codes.
/// Codes follow pattern: Level 1 = 1 digit, Level 2 = 2 digits,
/// Level 3 = 4 digits, Level 4 = 8 digits.
/// </summary>
public interface IAccountCodeGeneratorService
{
    /// <summary>
    /// Generates the next account code for a given parent account and level.
    /// Thread-safe via SemaphoreSlim.
    /// </summary>
    Task<Result<string>> GenerateCodeAsync(int? parentId, byte level, CancellationToken ct = default);

    /// <summary>
    /// Gets the default color code for a given account nature.
    /// Asset=#2196F3, Liability=#F44336, Equity=#4CAF50,
    /// Revenue=#4CAF50, Expense=#FF9800.
    /// </summary>
    static string GetColorCode(byte nature) => nature switch
    {
        1 => "#2196F3",  // Asset - Blue
        2 => "#F44336",  // Liability - Red
        3 => "#4CAF50",  // Equity - Green
        4 => "#4CAF50",  // Revenue - Green
        5 => "#FF9800",  // Expense - Orange
        _ => "#9E9E9E"   // Unknown - Grey
    };
}
