using System.Windows;
using FluentAssertions;
using Xunit;

namespace SalesSystem.DesktopPWF.Tests.Security;

[Trait("Category", "Security")]
public class FallbackErrorDialogExistsTests
{
    [Fact]
    public void FallbackErrorDialog_ClassExists()
    {
        var dialogType = typeof(SalesSystem.DesktopPWF.Views.Dialogs.FallbackErrorDialog);
        dialogType.Should().NotBeNull("FallbackErrorDialog should exist in Views/Dialogs");
        dialogType.IsSubclassOf(typeof(Window)).Should().BeTrue("FallbackErrorDialog must inherit from Window");
    }

    [Fact]
    public void FallbackErrorDialog_ConstructorAcceptsMessage()
    {
        var dialogType = typeof(SalesSystem.DesktopPWF.Views.Dialogs.FallbackErrorDialog);
        var ctor = dialogType.GetConstructor(new[] { typeof(string) });
        ctor.Should().NotBeNull("FallbackErrorDialog should have a constructor accepting a string message");
    }
}