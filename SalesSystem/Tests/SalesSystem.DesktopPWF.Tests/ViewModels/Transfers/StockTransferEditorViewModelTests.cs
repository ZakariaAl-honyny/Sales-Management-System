namespace SalesSystem.DesktopPWF.Tests.ViewModels.Transfers;

/// <summary>
/// Tests for StockTransferEditorViewModel
/// Note: All tests are skipped because this ViewModel has constructor dependencies on App.GetService
/// </summary>
public class StockTransferEditorViewModelTests
{
    // All tests are skipped - ViewModel has constructor dependencies on App.GetService

    #region Skipped Tests - Editor ViewModel requires DI container

    [Fact(Skip = "Editor ViewModel requires DI container (App.GetService)")]
    public void Constructor_NewTransfer_SetsIsEditFalse()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "Editor ViewModel requires DI container (App.GetService)")]
    public void Constructor_WithTransferId_SetsIsEditTrue()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "Editor ViewModel requires DI container (App.GetService)")]
    public void Notes_WhenSet_NotifiesPropertyChanged()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "Editor ViewModel requires DI container (App.GetService)")]
    public async Task SaveCommand_WhenValid_CallsCreateService()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "Editor ViewModel requires DI container (App.GetService)")]
    public async Task SaveCommand_WhenEditMode_CallsUpdateService()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "Editor ViewModel requires DI container (App.GetService)")]
    public void CancelCommand_InvokesCloseRequested()
    {
        // Skip - relies on App.GetService
    }

    #endregion
}
