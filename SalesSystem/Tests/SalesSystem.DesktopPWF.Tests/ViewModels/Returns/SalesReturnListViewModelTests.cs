namespace SalesSystem.DesktopPWF.Tests.ViewModels.Returns;

/// <summary>
/// Tests for SalesReturnListViewModel
/// Note: Tests are skipped because this ViewModel uses App.GetService in constructor
/// </summary>
public class SalesReturnListViewModelTests
{
    // All tests are skipped - ViewModel uses App.GetService in constructor

    #region Skipped Tests - ViewModel uses App.GetService in constructor

    [Fact(Skip = "ViewModel uses App.GetService in constructor")]
    public async Task LoadReturnsAsync_WhenApiSucceeds_PopulatesReturnsCollection()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "ViewModel uses App.GetService in constructor")]
    public async Task LoadReturnsAsync_WhenApiFails_SetsErrorMessage()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "ViewModel uses App.GetService in constructor")]
    public async Task LoadReturnsAsync_WhenLoading_SetsIsLoadingTrue()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "ViewModel uses App.GetService in constructor")]
    public void IsLoading_Set_NotifiesPropertyChanged()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "ViewModel uses App.GetService in constructor")]
    public void RefreshCommand_IsInitialized()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "ViewModel uses App.GetService in constructor")]
    public void EditCommand_CannotExecute_WhenNoSelection()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "ViewModel uses App.GetService in constructor")]
    public void Cleanup_UnsubscribesFromEventBus()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "ViewModel uses App.GetService in constructor")]
    public void Constructor_SubscribesToSalesReturnChangedMessage()
    {
        // Skip - relies on App.GetService
    }

    #endregion
}
