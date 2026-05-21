namespace SalesSystem.DesktopPWF.Tests.ViewModels.Payments;

/// <summary>
/// Tests for CustomerPaymentEditorViewModel
/// Note: All tests are skipped because this ViewModel has constructor dependencies on App.GetService
/// </summary>
public class CustomerPaymentEditorViewModelTests
{
    // All tests are skipped - ViewModel has constructor dependencies on App.GetService

    #region Skipped Tests - Editor ViewModel requires DI container

    [Fact(Skip = "Editor ViewModel requires DI container (App.GetService)")]
    public void Constructor_NewPayment_SetsIsEditFalse()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "Editor ViewModel requires DI container (App.GetService)")]
    public void Constructor_WithPaymentId_SetsIsEditTrue()
    {
        // Skip - relies on App.GetService
    }

    [Fact(Skip = "Editor ViewModel requires DI container (App.GetService)")]
    public void Amount_WhenSet_NotifiesPropertyChanged()
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
