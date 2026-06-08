using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SalesSystem.Application.Accounting.Services;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace SalesSystem.Application.Tests.Services;

/// <summary>
/// Integration tests for AccountingIntegrationService.
/// Verifies that journal entries are created correctly for all business operations
/// (sales, purchases, payments, opening balances, and reversals).
/// The service delegates actual journal entry creation to IJournalEntryService (mocked).
/// </summary>
public class AccountingIntegrationServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ISystemAccountService> _mockSystemAccountService;
    private readonly Mock<IJournalEntryService> _mockJournalEntryService;
    private readonly Mock<ILogger<AccountingIntegrationService>> _mockLogger;

    private readonly AccountingIntegrationService _sut;

    // Reusable system account mappings DTO with valid account IDs
    private static readonly SystemAccountMappingsDto DefaultMappings = new(
        Id: 1,
        DefaultCashAccountId: 10,
        DefaultCashAccountName: "الصندوق",
        DefaultCashAccountCode: "110101",
        DefaultBankAccountId: 11,
        DefaultBankAccountName: "البنك",
        DefaultBankAccountCode: "110201",
        InventoryAssetAccountId: 20,
        InventoryAssetAccountName: "المخزون",
        InventoryAssetAccountCode: "1201",
        AccountsReceivableAccountId: 30,
        AccountsReceivableAccountName: "الذمم المدينة",
        AccountsReceivableAccountCode: "1301",
        AccountsPayableAccountId: 40,
        AccountsPayableAccountName: "الذمم الدائنة",
        AccountsPayableAccountCode: "2101",
        VatOutputAccountId: 50,
        VatOutputAccountName: "ضريبة المخرجات",
        VatOutputAccountCode: "2201",
        VatInputAccountId: 60,
        VatInputAccountName: "ضريبة المدخلات",
        VatInputAccountCode: "2202",
        CapitalAccountId: 70,
        CapitalAccountName: "رأس المال",
        CapitalAccountCode: "3101",
        SalesRevenueAccountId: 80,
        SalesRevenueAccountName: "إيرادات المبيعات",
        SalesRevenueAccountCode: "4101",
        SalesReturnAccountId: 81,
        SalesReturnAccountName: "مردودات المبيعات",
        SalesReturnAccountCode: "4102",
        CogsAccountId: 90,
        CogsAccountName: "تكلفة البضاعة المباعة",
        CogsAccountCode: "5101",
        GeneralExpenseAccountId: 100,
        GeneralExpenseAccountName: "مصروفات عمومية",
        GeneralExpenseAccountCode: "5201",
        SpoilageLossAccountId: 110,
        SpoilageLossAccountName: "التوالف",
        SpoilageLossAccountCode: "5301",
        OpeningBalanceEquityAccountId: 120,
        OpeningBalanceEquityAccountName: "أرباح مبقاة",
        OpeningBalanceEquityAccountCode: "3201",
        BranchId: null);

    public AccountingIntegrationServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("[TEST] AccountingIntegrationServiceTests initialized");

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);

        _mockUow = new Mock<IUnitOfWork>();
        _mockSystemAccountService = new Mock<ISystemAccountService>();
        _mockJournalEntryService = new Mock<IJournalEntryService>();
        _mockLogger = new Mock<ILogger<AccountingIntegrationService>>();

        // Setup UoW repositories for entities the SUT queries directly
        _mockUow.Setup(u => u.JournalEntries).Returns(new InMemoryEfCoreRepository<JournalEntry>(_dbContext));
        _mockUow.Setup(u => u.JournalEntryLines).Returns(new InMemoryEfCoreRepository<JournalEntryLine>(_dbContext));

        // Setup SaveChangesAsync to propagate to the in-memory DB
        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _dbContext.SaveChangesAsync();
                return 1;
            });

        // Default: system account service returns valid mappings
        _mockSystemAccountService.Setup(s => s.GetMappingsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SystemAccountMappingsDto>.Success(DefaultMappings));

        // Default: journal entry service returns success with ID = 1
        _mockJournalEntryService.Setup(s => s.CreateJournalEntryAsync(
                It.IsAny<CreateJournalEntryRequest>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(1));

        _sut = new AccountingIntegrationService(
            _mockJournalEntryService.Object,
            _mockSystemAccountService.Object,
            _mockUow.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    // ────────────────────────────────────────────────────────────────
    //  A. Customer Opening Entry
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCustomerOpeningEntry_WithPositiveBalance_CreatesEntry()
    {
        _output.WriteLine("[TEST] CreateCustomerOpeningEntry_WithPositiveBalance_CreatesEntry");

        var result = await _sut.CreateCustomerOpeningEntryAsync(
            customerId: 1,
            customerName: "عميل تجربة",
            openingBalance: 500m,
            createdByUserId: 1,
            transactionDate: new DateTime(2026, 1, 15),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify journal entry service was called with correct parameters
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.EntryType == JournalEntryType.OpeningBalance &&
                r.ReferenceType == "Customer" &&
                r.ReferenceId == 1 &&
                r.Lines.Count == 2 &&
                r.Lines[0].AccountId == DefaultMappings.AccountsReceivableAccountId &&
                r.Lines[0].Debit == 500m &&
                r.Lines[0].Credit == 0m &&
                r.Lines[1].AccountId == DefaultMappings.OpeningBalanceEquityAccountId &&
                r.Lines[1].Debit == 0m &&
                r.Lines[1].Credit == 500m),
            It.Is<int>(u => u == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Customer opening entry created with Dr AR / Cr OpeningBalanceEquity");
    }

    [Fact]
    public async Task CreateCustomerOpeningEntry_WithZeroBalance_ReturnsSuccessNoEntry()
    {
        _output.WriteLine("[TEST] CreateCustomerOpeningEntry_WithZeroBalance_ReturnsSuccessNoEntry");

        var result = await _sut.CreateCustomerOpeningEntryAsync(
            customerId: 2,
            customerName: "عميل بدون رصيد",
            openingBalance: 0m,
            createdByUserId: 1,
            transactionDate: new DateTime(2026, 1, 15),
            CancellationToken.None);

        // Code returns Success(0) for zero/negative opening balance
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);

        // Journal entry service should NOT have been called
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.IsAny<CreateJournalEntryRequest>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        _output.WriteLine("[PASS] No journal entry created for zero opening balance");
    }

    // ────────────────────────────────────────────────────────────────
    //  B. Supplier Opening Entry
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSupplierOpeningEntry_WithPositiveBalance_CreatesEntry()
    {
        _output.WriteLine("[TEST] CreateSupplierOpeningEntry_WithPositiveBalance_CreatesEntry");

        var result = await _sut.CreateSupplierOpeningEntryAsync(
            supplierId: 1,
            supplierName: "مورد تجربة",
            openingBalance: 1000m,
            createdByUserId: 1,
            transactionDate: new DateTime(2026, 1, 15),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify: Dr OpeningBalanceEquity / Cr AccountsPayable
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.EntryType == JournalEntryType.OpeningBalance &&
                r.ReferenceType == "Supplier" &&
                r.ReferenceId == 1 &&
                r.Lines.Count == 2 &&
                r.Lines[0].AccountId == DefaultMappings.OpeningBalanceEquityAccountId &&
                r.Lines[0].Debit == 1000m &&
                r.Lines[0].Credit == 0m &&
                r.Lines[1].AccountId == DefaultMappings.AccountsPayableAccountId &&
                r.Lines[1].Debit == 0m &&
                r.Lines[1].Credit == 1000m),
            It.Is<int>(u => u == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Supplier opening entry created with Dr OpeningBalanceEquity / Cr AP");
    }

    // ────────────────────────────────────────────────────────────────
    //  C. Sales Invoice Post Entry
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSalesPostEntry_WithCashSale_CreatesRevenueAndCogs()
    {
        _output.WriteLine("[TEST] CreateSalesPostEntry_WithCashSale_CreatesRevenueAndCogs");

        // Arrange: cash sale with total = 1150 (1000 + 150 tax), cost = 700
        var invoice = SalesInvoice.Create(
            warehouseId: 1,
            invoiceNo: 1001,
            customerId: 1,
            paymentType: PaymentType.Cash);
        invoice.AddItem(SalesInvoiceItem.Create(
            productId: 1,
            quantity: 10m,
            unitPrice: 100m)); // LineTotal = 1000
        invoice.RecalculateTotals();
        invoice.SetTaxAmount(150m); // TotalAmount = 1000 - 0 + 150 = 1150
        invoice.SetPaidAmount(1150m);

        // Save to DB so invoice gets an Id for ReferenceId
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();
        _output.WriteLine($"[SETUP] Invoice saved with Id={invoice.Id}");

        var result = await _sut.CreateSalesPostEntryAsync(
            invoice,
            createdByUserId: 1,
            totalCost: 700m,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify: Dr Cash (1150), Cr SalesRevenue (1000), Cr VatOutput (150),
        //         Dr COGS (700), Cr Inventory (700) — 5 lines total
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.EntryType == JournalEntryType.Sales &&
                r.ReferenceType == "SalesInvoice" &&
                r.ReferenceId == invoice.Id &&
                r.Lines.Count == 5 &&
                // Cash line (Dr)
                r.Lines[0].AccountId == DefaultMappings.DefaultCashAccountId &&
                r.Lines[0].Debit == 1150m &&
                r.Lines[0].Credit == 0m &&
                // Revenue line (Cr)
                r.Lines[1].AccountId == DefaultMappings.SalesRevenueAccountId &&
                r.Lines[1].Debit == 0m &&
                r.Lines[1].Credit == 1000m &&
                // VAT line (Cr)
                r.Lines[2].AccountId == DefaultMappings.VatOutputAccountId &&
                r.Lines[2].Debit == 0m &&
                r.Lines[2].Credit == 150m &&
                // COGS line (Dr)
                r.Lines[3].AccountId == DefaultMappings.CogsAccountId &&
                r.Lines[3].Debit == 700m &&
                r.Lines[3].Credit == 0m &&
                // Inventory line (Cr)
                r.Lines[4].AccountId == DefaultMappings.InventoryAssetAccountId &&
                r.Lines[4].Debit == 0m &&
                r.Lines[4].Credit == 700m),
            It.Is<int>(u => u == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Cash sale entry created with Dr Cash / Cr Revenue + Vat, Dr COGS / Cr Inventory");
    }

    [Fact]
    public async Task CreateSalesPostEntry_WithCreditSale_CreatesRevenueAndCogs()
    {
        _output.WriteLine("[TEST] CreateSalesPostEntry_WithCreditSale_CreatesRevenueAndCogs");

        // Arrange: credit sale (no paid amount)
        var invoice = SalesInvoice.Create(
            warehouseId: 1,
            invoiceNo: 1002,
            customerId: 1,
            paymentType: PaymentType.Credit);
        invoice.AddItem(SalesInvoiceItem.Create(
            productId: 1,
            quantity: 5m,
            unitPrice: 200m)); // LineTotal = 1000
        invoice.RecalculateTotals();
        invoice.SetPaidAmount(0m);
        // TotalAmount = 1000

        // Save to DB so invoice gets an Id for ReferenceId
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();
        _output.WriteLine($"[SETUP] Invoice saved with Id={invoice.Id}");

        var result = await _sut.CreateSalesPostEntryAsync(
            invoice,
            createdByUserId: 1,
            totalCost: 400m,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify: Dr AR (1000), Cr Revenue (1000), Dr COGS (400), Cr Inventory (400) — 4 lines total
        // No VAT since TaxAmount = 0
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.EntryType == JournalEntryType.Sales &&
                r.Lines.Count == 4 &&
                // AR line (Dr, not Cash since it's credit sale)
                r.Lines[0].AccountId == DefaultMappings.AccountsReceivableAccountId &&
                r.Lines[0].Debit == 1000m &&
                r.Lines[0].Credit == 0m &&
                // Revenue line (Cr)
                r.Lines[1].AccountId == DefaultMappings.SalesRevenueAccountId &&
                r.Lines[1].Debit == 0m &&
                r.Lines[1].Credit == 1000m &&
                // COGS line (Dr)
                r.Lines[2].AccountId == DefaultMappings.CogsAccountId &&
                r.Lines[2].Debit == 400m &&
                r.Lines[2].Credit == 0m &&
                // Inventory line (Cr)
                r.Lines[3].AccountId == DefaultMappings.InventoryAssetAccountId &&
                r.Lines[3].Debit == 0m &&
                r.Lines[3].Credit == 400m),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Credit sale entry created with Dr AR / Cr Revenue, Dr COGS / Cr Inventory");
    }

    [Fact]
    public async Task CreateSalesPostEntry_WhenMappingsMissing_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateSalesPostEntry_WhenMappingsMissing_ReturnsFailure");

        // Arrange: system account service returns failure
        _mockSystemAccountService.Setup(s => s.GetMappingsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SystemAccountMappingsDto>.Failure("لم يتم إعداد الحسابات النظامية"));

        var invoice = SalesInvoice.Create(
            warehouseId: 1,
            invoiceNo: 1003,
            customerId: 1,
            paymentType: PaymentType.Cash);
        invoice.AddItem(SalesInvoiceItem.Create(productId: 1, quantity: 1m, unitPrice: 10m));
        invoice.RecalculateTotals();
        invoice.SetPaidAmount(10m);

        var result = await _sut.CreateSalesPostEntryAsync(
            invoice,
            createdByUserId: 1,
            totalCost: 5m,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الحسابات النظامية");

        _output.WriteLine("[PASS] Missing mappings returns failure");
    }

    // ────────────────────────────────────────────────────────────────
    //  D. Reverse Sales Post Entry (cancellation)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReverseSalesPostEntry_CreatesReversal()
    {
        _output.WriteLine("[TEST] ReverseSalesPostEntry_CreatesReversal");

        // Arrange: create an invoice with items.
        // The service will try to find the original journal entry in the DB.
        // Since no original entry exists in the InMemory DB, it falls through to the
        // fallback COGS computation path (item.Product == null → computedCost = 0).
        // The revenue-side reversal is still created regardless.
        var invoice = SalesInvoice.Create(
            warehouseId: 1,
            invoiceNo: 1001,
            customerId: 1,
            paymentType: PaymentType.Cash);
        invoice.AddItem(SalesInvoiceItem.Create(
            productId: 1,
            quantity: 10m,
            unitPrice: 100m)); // LineTotal = 1000
        invoice.RecalculateTotals();
        invoice.SetTaxAmount(150m);
        invoice.SetPaidAmount(1150m);
        // Save to DB so it gets an Id for the ReferenceId
        _dbContext.SalesInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        _output.WriteLine($"[SETUP] Invoice created with Id={invoice.Id}, InvoiceNo={invoice.InvoiceNo}");

        var result = await _sut.ReverseSalesPostEntryAsync(
            invoice,
            reversedByUserId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify reversal entry was created with revenue-side Dr/Cr swap:
        // Original: Cr SalesRevenue, Cr VatOutput, Dr Cash
        // Reverse:  Dr SalesRevenue, Dr VatOutput, Cr Cash
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.EntryType == JournalEntryType.SalesReturn &&
                r.ReferenceType == "SalesInvoice" &&
                r.ReferenceId == invoice.Id &&
                r.Description.Contains("عكس") &&
                r.Lines.Any(l => l.AccountId == DefaultMappings.SalesRevenueAccountId
                              && l.Debit == 1000m && l.Credit == 0m) &&
                r.Lines.Any(l => l.AccountId == DefaultMappings.VatOutputAccountId
                              && l.Debit == 150m && l.Credit == 0m) &&
                r.Lines.Any(l => l.AccountId == DefaultMappings.DefaultCashAccountId
                              && l.Debit == 0m && l.Credit == 1150m)),
            It.Is<int>(u => u == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Sales reversal entry created with swapped Dr/Cr");
    }

    // ────────────────────────────────────────────────────────────────
    //  E. Purchase Invoice Post Entry
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePurchasePostEntry_CreatesInventoryAndVat()
    {
        _output.WriteLine("[TEST] CreatePurchasePostEntry_CreatesInventoryAndVat");

        // Arrange: purchase invoice with VAT
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 2001,
            paymentType: PaymentType.Cash);
        invoice.AddItem(PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 20m,
            unitCost: 50m)); // LineTotal = 1000
        invoice.RecalculateTotals();
        invoice.SetTaxAmount(150m); // VAT Input
        invoice.SetPaidAmount(1150m);
        // TotalAmount = 1000 + 150 = 1150

        var result = await _sut.CreatePurchasePostEntryAsync(
            invoice,
            createdByUserId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify: Dr Inventory (1000), Dr VatInput (150), Cr Cash (1150)
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.EntryType == JournalEntryType.Purchase &&
                r.ReferenceType == "PurchaseInvoice" &&
                r.Lines.Count == 3 &&
                // Inventory line
                r.Lines[0].AccountId == DefaultMappings.InventoryAssetAccountId &&
                r.Lines[0].Debit == 1000m &&
                r.Lines[0].Credit == 0m &&
                // VAT Input line
                r.Lines[1].AccountId == DefaultMappings.VatInputAccountId &&
                r.Lines[1].Debit == 150m &&
                r.Lines[1].Credit == 0m &&
                // Cash line (Cr)
                r.Lines[2].AccountId == DefaultMappings.DefaultCashAccountId &&
                r.Lines[2].Debit == 0m &&
                r.Lines[2].Credit == 1150m),
            It.Is<int>(u => u == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Purchase entry created with Dr Inventory + VatInput / Cr Cash");
    }

    // ────────────────────────────────────────────────────────────────
    //  F. Reverse Purchase Post Entry (cancellation)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReversePurchasePostEntry_CreatesReversal()
    {
        _output.WriteLine("[TEST] ReversePurchasePostEntry_CreatesReversal");

        // Arrange
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 2002,
            paymentType: PaymentType.Cash);
        invoice.AddItem(PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 10m,
            unitCost: 100m)); // LineTotal = 1000
        invoice.RecalculateTotals();
        invoice.SetPaidAmount(1000m);

        _dbContext.PurchaseInvoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.ReversePurchasePostEntryAsync(
            invoice,
            reversedByUserId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify: Cr Inventory (1000), Dr Cash (1000)
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.EntryType == JournalEntryType.PurchaseReturn &&
                r.ReferenceType == "PurchaseInvoice" &&
                r.ReferenceId == invoice.Id &&
                r.Lines.Count == 2 &&
                // Inventory line (Cr)
                r.Lines[0].AccountId == DefaultMappings.InventoryAssetAccountId &&
                r.Lines[0].Debit == 0m &&
                r.Lines[0].Credit == 1000m &&
                // Cash line (Dr)
                r.Lines[1].AccountId == DefaultMappings.DefaultCashAccountId &&
                r.Lines[1].Debit == 1000m &&
                r.Lines[1].Credit == 0m),
            It.Is<int>(u => u == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Purchase reversal entry created with Cr Inventory / Dr Cash");
    }

    // ────────────────────────────────────────────────────────────────
    //  G. Customer Payment Entry
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCustomerPaymentEntry_CreatesCashAndAr()
    {
        _output.WriteLine("[TEST] CreateCustomerPaymentEntry_CreatesCashAndAr");

        // Arrange
        var payment = CustomerPayment.Create(
            paymentNo: "CP-20260601-0001",
            customerId: 1,
            amount: 500m,
            paymentMethod: 1); // Cash

        var result = await _sut.CreateCustomerPaymentEntryAsync(
            payment,
            customerName: "عميل تجربة",
            createdByUserId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify: Dr Cash (500), Cr AR (500)
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.EntryType == JournalEntryType.CustomerReceipt &&
                r.ReferenceType == "CustomerPayment" &&
                r.ReferenceId == payment.Id &&
                r.Lines.Count == 2 &&
                r.Lines[0].AccountId == DefaultMappings.DefaultCashAccountId &&
                r.Lines[0].Debit == 500m &&
                r.Lines[0].Credit == 0m &&
                r.Lines[1].AccountId == DefaultMappings.AccountsReceivableAccountId &&
                r.Lines[1].Debit == 0m &&
                r.Lines[1].Credit == 500m),
            It.Is<int>(u => u == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Customer payment entry created with Dr Cash / Cr AR");
    }

    // ────────────────────────────────────────────────────────────────
    //  H. Supplier Payment Entry
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSupplierPaymentEntry_CreatesApAndCash()
    {
        _output.WriteLine("[TEST] CreateSupplierPaymentEntry_CreatesApAndCash");

        // Arrange
        var payment = SupplierPayment.Create(
            paymentNo: "SP-20260601-0001",
            supplierId: 1,
            amount: 800m,
            paymentMethod: 1); // Cash

        var result = await _sut.CreateSupplierPaymentEntryAsync(
            payment,
            supplierName: "مورد تجربة",
            createdByUserId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify: Dr AP (800), Cr Cash (800)
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.EntryType == JournalEntryType.SupplierPayment &&
                r.ReferenceType == "SupplierPayment" &&
                r.ReferenceId == payment.Id &&
                r.Lines.Count == 2 &&
                r.Lines[0].AccountId == DefaultMappings.AccountsPayableAccountId &&
                r.Lines[0].Debit == 800m &&
                r.Lines[0].Credit == 0m &&
                r.Lines[1].AccountId == DefaultMappings.DefaultCashAccountId &&
                r.Lines[1].Debit == 0m &&
                r.Lines[1].Credit == 800m),
            It.Is<int>(u => u == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Supplier payment entry created with Dr AP / Cr Cash");
    }

    // ────────────────────────────────────────────────────────────────
    //  I. Reverse Customer Payment Entry
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReverseCustomerPaymentEntry_CreatesReversal()
    {
        _output.WriteLine("[TEST] ReverseCustomerPaymentEntry_CreatesReversal");

        var result = await _sut.ReverseCustomerPaymentEntryAsync(
            paymentId: 1,
            amount: 500m,
            customerName: "عميل تجربة",
            reversedByUserId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify reversal: Dr AR (500), Cr Cash (500) — opposite of original
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.EntryType == JournalEntryType.Manual &&
                r.ReferenceType == "CustomerPayment" &&
                r.ReferenceId == 1 &&
                r.Lines.Count == 2 &&
                r.Lines[0].AccountId == DefaultMappings.AccountsReceivableAccountId &&
                r.Lines[0].Debit == 500m &&
                r.Lines[0].Credit == 0m &&
                r.Lines[1].AccountId == DefaultMappings.DefaultCashAccountId &&
                r.Lines[1].Debit == 0m &&
                r.Lines[1].Credit == 500m),
            It.Is<int>(u => u == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Customer payment reversal entry created with Dr AR / Cr Cash");
    }

    // ────────────────────────────────────────────────────────────────
    //  J. Reverse Supplier Payment Entry
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReverseSupplierPaymentEntry_CreatesReversal()
    {
        _output.WriteLine("[TEST] ReverseSupplierPaymentEntry_CreatesReversal");

        var result = await _sut.ReverseSupplierPaymentEntryAsync(
            paymentId: 1,
            amount: 800m,
            supplierName: "مورد تجربة",
            reversedByUserId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        // Verify reversal: Dr Cash (800), Cr AP (800) — opposite of original
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.EntryType == JournalEntryType.Manual &&
                r.ReferenceType == "SupplierPayment" &&
                r.ReferenceId == 1 &&
                r.Lines.Count == 2 &&
                r.Lines[0].AccountId == DefaultMappings.DefaultCashAccountId &&
                r.Lines[0].Debit == 800m &&
                r.Lines[0].Credit == 0m &&
                r.Lines[1].AccountId == DefaultMappings.AccountsPayableAccountId &&
                r.Lines[1].Debit == 0m &&
                r.Lines[1].Credit == 800m),
            It.Is<int>(u => u == 1),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Supplier payment reversal entry created with Dr Cash / Cr AP");
    }

    // ────────────────────────────────────────────────────────────────
    //  Additional: Edge Cases & Error Handling
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSalesPostEntry_WithDiscount_CalculatesNetRevenue()
    {
        _output.WriteLine("[TEST] CreateSalesPostEntry_WithDiscount_CalculatesNetRevenue");

        // Arrange: invoice with header discount
        var invoice = SalesInvoice.Create(
            warehouseId: 1,
            invoiceNo: 1004,
            customerId: 1,
            paymentType: PaymentType.Cash,
            discountAmount: 100m);
        invoice.AddItem(SalesInvoiceItem.Create(
            productId: 1,
            quantity: 10m,
            unitPrice: 100m)); // LineTotal = 1000
        invoice.RecalculateTotals();
        // SubTotal = 1000, Discount = 100, Tax = 0, Total = 900
        invoice.SetPaidAmount(900m);

        var result = await _sut.CreateSalesPostEntryAsync(
            invoice,
            createdByUserId: 1,
            totalCost: 500m,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Net revenue = SubTotal - Discount = 1000 - 100 = 900
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.Lines.Any(l => l.AccountId == DefaultMappings.SalesRevenueAccountId
                              && l.Credit == 900m)),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Net revenue correctly calculated after discount");
    }

    [Fact]
    public async Task CreatePurchasePostEntry_WithDiscount_CalculatesNetInventoryCost()
    {
        _output.WriteLine("[TEST] CreatePurchasePostEntry_WithDiscount_CalculatesNetInventoryCost");

        // Arrange: purchase with header discount
        var invoice = PurchaseInvoice.Create(
            supplierId: 1,
            warehouseId: 1,
            invoiceNo: 2003,
            paymentType: PaymentType.Cash,
            discountAmount: 200m);
        invoice.AddItem(PurchaseInvoiceItem.Create(
            productId: 1,
            quantity: 50m,
            unitCost: 20m)); // LineTotal = 1000
        invoice.RecalculateTotals();
        // SubTotal = 1000, Discount = 200, Net = 800, Tax = 0, Total = 800
        invoice.SetPaidAmount(800m);

        var result = await _sut.CreatePurchasePostEntryAsync(
            invoice,
            createdByUserId: 1,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Net inventory cost = SubTotal - Discount = 1000 - 200 = 800
        _mockJournalEntryService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<CreateJournalEntryRequest>(r =>
                r.Lines.Any(l => l.AccountId == DefaultMappings.InventoryAssetAccountId
                              && l.Debit == 800m)),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _output.WriteLine("[PASS] Net inventory cost correctly calculated after discount");
    }

    [Fact]
    public async Task CreateCustomerOpeningEntry_WhenMappingsMissing_ReturnsFailure()
    {
        _output.WriteLine("[TEST] CreateCustomerOpeningEntry_WhenMappingsMissing_ReturnsFailure");

        _mockSystemAccountService.Setup(s => s.GetMappingsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<SystemAccountMappingsDto>.Failure("لم يتم إعداد الحسابات النظامية"));

        var result = await _sut.CreateCustomerOpeningEntryAsync(
            customerId: 1,
            customerName: "عميل",
            openingBalance: 500m,
            createdByUserId: 1,
            transactionDate: new DateTime(2026, 1, 15),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("الحسابات النظامية");

        _output.WriteLine("[PASS] Missing customer opening entry mappings returns failure");
    }

    // ────────────────────────────────────────────────────────────────
    //  Helper Classes
    // ────────────────────────────────────────────────────────────────

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
        public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
        public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
        public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
        public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<SystemAccountMappings> SystemAccountMappings => Set<SystemAccountMappings>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
    }

    private class InMemoryEfCoreRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly DbContext _context;

        public InMemoryEfCoreRepository(DbContext context)
        {
            _context = context;
        }

        public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _context.Set<T>().FindAsync(new object[] { id }, ct);

        public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<T>>(_context.Set<T>().ToList());

        public async Task<T> AddAsync(T entity, CancellationToken ct = default)
        {
            await _context.Set<T>().AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);
            return entity;
        }

        public Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            _context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.Set<T>().FindAsync(new object[] { id }, ct);
            if (entity != null)
            {
                entity.MarkAsDeleted();
                _context.Set<T>().Update(entity);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _context.Set<T>().FindAsync(new object[] { id }, ct);
            if (entity != null)
            {
                _context.Set<T>().Remove(entity);
                await _context.SaveChangesAsync(ct);
            }
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
        }

        public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().FirstOrDefault(predicate));

        public Task<T?> FirstOrDefaultIgnoreFiltersAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().IgnoreQueryFilters().FirstOrDefault(predicate));

        public Task<List<T>> ToListAsync(CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().ToList());

        public Task<List<T>> ToListAsync(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? queryConfig = null, CancellationToken ct = default, bool ignoreQueryFilters = false, params string[] includePaths)
        {
            IQueryable<T> query = _context.Set<T>();
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            if (predicate != null) query = query.Where(predicate);
            if (queryConfig != null) query = queryConfig(query);
            return Task.FromResult(query.ToList());
        }

        public Task<(List<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IQueryable<T>>? orderConfig, int page, int pageSize, CancellationToken ct = default, bool ignoreQueryFilters = false, params string[] includePaths)
        {
            IQueryable<T> query = _context.Set<T>();
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            if (predicate != null) query = query.Where(predicate);
            var totalCount = query.Count();
            if (orderConfig != null) query = orderConfig(query);
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, totalCount));
        }

        public Task<List<T>> ToListIgnoreFiltersAsync(CancellationToken ct = default, params string[] includePaths)
            => Task.FromResult(_context.Set<T>().IgnoreQueryFilters().ToList());

        public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => Task.FromResult(predicate == null ? _context.Set<T>().Count() : _context.Set<T>().Count(predicate));

        public Task<int> CountIgnoreFiltersAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
            => Task.FromResult(predicate == null ? _context.Set<T>().IgnoreQueryFilters().Count() : _context.Set<T>().IgnoreQueryFilters().Count(predicate));

        public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => Task.FromResult(_context.Set<T>().Any(predicate));

        public Task<bool> AnyIgnoreFiltersAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => Task.FromResult(_context.Set<T>().IgnoreQueryFilters().Any(predicate));

        public IQueryable<T> Query() => _context.Set<T>().AsQueryable();
    }
}
