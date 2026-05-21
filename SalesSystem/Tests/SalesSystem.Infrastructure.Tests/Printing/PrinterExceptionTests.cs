using SalesSystem.Infrastructure.Printing;

namespace SalesSystem.Infrastructure.Tests.Printing;

public class PrinterExceptionTests
{
    [Fact]
    public void Constructor_ShouldStoreMessage()
    {
        var message = "الطابعة غير متصلة";
        var ex = new PrinterException(message);
        ex.Message.Should().Be(message);
    }

    [Fact]
    public void ShouldInheritFromException()
    {
        var ex = new PrinterException("test");
        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void InnerException_ShouldBeNull_WhenNotProvided()
    {
        var ex = new PrinterException("test");
        ex.InnerException.Should().BeNull();
    }
}
