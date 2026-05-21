using System.Text;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Infrastructure.Printing.Thermal;

namespace SalesSystem.Infrastructure.Tests.Printing;

public class EscPosCommandBuilderTests
{
    static EscPosCommandBuilderTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    // ─────────────────────────────────────────────
    // Initialize
    // ─────────────────────────────────────────────

    [Fact]
    public void Initialize_ShouldReturnCorrectBytes()
    {
        var result = EscPos.Initialize();
        result.Should().BeEquivalentTo(new byte[] { 0x1B, 0x40 });
    }

    [Fact]
    public void Initialize_ShouldHaveLength2()
    {
        var result = EscPos.Initialize();
        result.Length.Should().Be(2);
    }

    // ─────────────────────────────────────────────
    // CutPaper
    // ─────────────────────────────────────────────

    [Fact]
    public void CutPaper_ShouldReturnCorrectBytes()
    {
        var result = EscPos.CutPaper();
        result.Should().BeEquivalentTo(new byte[] { 0x1D, 0x56, 0x42, 0x00 });
    }

    [Fact]
    public void CutPaper_ShouldHaveLength4()
    {
        var result = EscPos.CutPaper();
        result.Length.Should().Be(4);
    }

    // ─────────────────────────────────────────────
    // SetBold
    // ─────────────────────────────────────────────

    [Fact]
    public void SetBold_True_ShouldReturnCorrectBytes()
    {
        var result = EscPos.SetBold(true);
        result.Should().BeEquivalentTo(new byte[] { 0x1B, 0x45, 0x01 });
    }

    [Fact]
    public void SetBold_False_ShouldReturnCorrectBytes()
    {
        var result = EscPos.SetBold(false);
        result.Should().BeEquivalentTo(new byte[] { 0x1B, 0x45, 0x00 });
    }

    [Fact]
    public void SetBold_ShouldHaveLength3()
    {
        EscPos.SetBold(true).Length.Should().Be(3);
        EscPos.SetBold(false).Length.Should().Be(3);
    }

    // ─────────────────────────────────────────────
    // SetAlignment
    // ─────────────────────────────────────────────

    [Fact]
    public void SetAlignment_Left_ShouldReturnCorrectBytes()
    {
        var result = EscPos.SetAlignment(Alignment.Left);
        result.Should().BeEquivalentTo(new byte[] { 0x1B, 0x61, 0x00 });
    }

    [Fact]
    public void SetAlignment_Center_ShouldReturnCorrectBytes()
    {
        var result = EscPos.SetAlignment(Alignment.Center);
        result.Should().BeEquivalentTo(new byte[] { 0x1B, 0x61, 0x01 });
    }

    [Fact]
    public void SetAlignment_Right_ShouldReturnCorrectBytes()
    {
        var result = EscPos.SetAlignment(Alignment.Right);
        result.Should().BeEquivalentTo(new byte[] { 0x1B, 0x61, 0x02 });
    }

    [Fact]
    public void SetAlignment_ShouldHaveLength3()
    {
        EscPos.SetAlignment(Alignment.Left).Length.Should().Be(3);
        EscPos.SetAlignment(Alignment.Center).Length.Should().Be(3);
        EscPos.SetAlignment(Alignment.Right).Length.Should().Be(3);
    }

    // ─────────────────────────────────────────────
    // SetFontSize
    // ─────────────────────────────────────────────

    [Fact]
    public void SetFontSize_Normal_ShouldReturnCorrectBytes()
    {
        var result = EscPos.SetFontSize(1);
        result.Should().BeEquivalentTo(new byte[] { 0x1D, 0x21, 0x00 });
    }

    [Fact]
    public void SetFontSize_Double_ShouldReturnCorrectBytes()
    {
        var result = EscPos.SetFontSize(2);
        result.Should().BeEquivalentTo(new byte[] { 0x1D, 0x21, 0x11 });
    }

    [Fact]
    public void SetFontSize_BelowOne_ShouldTreatAsNormal()
    {
        var result = EscPos.SetFontSize(0);
        result.Should().BeEquivalentTo(new byte[] { 0x1D, 0x21, 0x00 });
    }

    [Fact]
    public void SetFontSize_ShouldHaveLength3()
    {
        EscPos.SetFontSize(1).Length.Should().Be(3);
        EscPos.SetFontSize(2).Length.Should().Be(3);
    }

    // ─────────────────────────────────────────────
    // PrintLine
    // ─────────────────────────────────────────────

    [Fact]
    public void PrintLine_ShouldEndWithNewline()
    {
        var result = EscPos.PrintLine("Hello");
        result[^1].Should().Be(0x0A); // LF
    }

    [Fact]
    public void PrintLine_EmptyString_ShouldReturnJustNewline()
    {
        var result = EscPos.PrintLine(string.Empty);
        result.Should().BeEquivalentTo(new byte[] { 0x0A });
    }

    [Fact]
    public void PrintLine_ShouldEncodeInWindows1256()
    {
        var result = EscPos.PrintLine("مرحباً");
        var text = Encoding.GetEncoding(1256).GetString(result);
        text.TrimEnd('\n').Should().Be("مرحباً");
    }

    [Fact]
    public void PrintLine_ShouldReturnTextBytesPlusNewline()
    {
        var result = EscPos.PrintLine("ABC");
        result.Length.Should().Be(4); // 3 text bytes + 1 newline
        result[0].Should().Be((byte)'A');
        result[1].Should().Be((byte)'B');
        result[2].Should().Be((byte)'C');
        result[3].Should().Be(0x0A);
    }

    [Fact]
    public void PrintLine_WithNewlineInText_ShouldPreserveIt()
    {
        var result = EscPos.PrintLine("Line1\nLine2");
        // Should contain 0x0A in the middle as well as at the end
        var count = result.Count(b => b == 0x0A);
        count.Should().Be(2); // one from \n in text, one appended
    }

    // ─────────────────────────────────────────────
    // Full receipt structure validation
    // ─────────────────────────────────────────────

    [Fact]
    public void FullReceipt_ShouldStartWithInitAndEndWithCut()
    {
        var generator = new ThermalReceiptGenerator();
        var invoice = new InvoicePrintDto
        {
            InvoiceId = 1,
            InvoiceNumber = "TEST-001",
            InvoiceDate = DateTime.Now,
            StoreName = "Test Store",
            CustomerOrSupplierName = "Test Customer",
            Items = new List<InvoiceItemPrintDto>
            {
                new("Item", "pc", 1m, 100m, 0m, 100m),
            },
            SubTotal = 100m,
            GrandTotal = 100m,
            AmountPaid = 100m,
            PaymentMethod = "Cash",
            InvoiceType = InvoiceTypePrint.Test,
        };

        var bytes = generator.GenerateEscPosCommands(invoice);

        // Starts with Initialize (ESC @)
        bytes[0].Should().Be(0x1B);
        bytes[1].Should().Be(0x40);

        // Ends with Cut Paper (GS V B NUL)
        bytes[^4].Should().Be(0x1D);
        bytes[^3].Should().Be(0x56);
        bytes[^2].Should().Be(0x42);
        bytes[^1].Should().Be(0x00);
    }
}
