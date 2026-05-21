namespace SalesSystem.E2ETests;

/// <summary>
/// Test categories for organizing E2E tests.
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// End-to-end tests category.
    /// </summary>
    public const string E2E = "E2E";

    /// <summary>
    /// Login flow tests category.
    /// </summary>
    public const string Login = "Login";

    /// <summary>
    /// Navigation tests category.
    /// </summary>
    public const string Navigation = "Navigation";

    /// <summary>
    /// Smoke tests category (quick sanity checks).
    /// </summary>
    public const string Smoke = "Smoke";

    /// <summary>
    /// Critical tests category (must pass for release).
    /// </summary>
    public const string Critical = "Critical";
}
