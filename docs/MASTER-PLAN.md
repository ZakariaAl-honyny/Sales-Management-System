Implementation Plan: Dynamic UOM + Multi-Barcode + Costing Strategy + Cash Boxes
📋 Master Rules for AI Agent
This plan has 7 phases. Complete and test each phase before starting the next. Never mix phases.

🗂️ Phase 0: Database Schema — Complete Migration
Agent Rule: Run ALL scripts in order. Do not skip any table.

Task 0.1 — Remove Old Columns, Add Dynamic UOM Tables
SQL

-- =============================================
-- STEP 1: Backup first (ALWAYS)
-- =============================================
-- Run on staging environment first

-- =============================================
-- STEP 2: Create ProductUnits (Dynamic UOM)
-- =============================================
CREATE TABLE ProductUnits (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    ProductId       INT NOT NULL,
    UnitName        NVARCHAR(100) NOT NULL,      -- e.g., "حبة", "طبق", "كرتون"
    BaseConversionFactor DECIMAL(18,6) NOT NULL, -- Pieces per this unit
    IsBaseUnit      BIT NOT NULL DEFAULT 0,       -- TRUE for exactly ONE unit per product
    SalesPrice      DECIMAL(18,4) NOT NULL DEFAULT 0,
    PurchaseCost    DECIMAL(18,4) NOT NULL DEFAULT 0,
    SupplierPrice   DECIMAL(18,4) NOT NULL DEFAULT 0, -- Catalog price from supplier
    LastPurchasePrice DECIMAL(18,4) NOT NULL DEFAULT 0,-- Last actual invoice price
    SortOrder       INT NOT NULL DEFAULT 0,        -- Display order in UI
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_ProductUnits_Products
        FOREIGN KEY (ProductId) REFERENCES Products(Id)
            ON DELETE CASCADE,

    CONSTRAINT CHK_BaseUnitFactor
        CHECK (IsBaseUnit = 0 OR BaseConversionFactor = 1)
);

CREATE INDEX IX_ProductUnits_ProductId ON ProductUnits(ProductId);

-- =============================================
-- STEP 3: Unit Barcodes (Many per Unit)
-- =============================================
CREATE TABLE UnitBarcodes (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    ProductUnitId   INT NOT NULL,
    BarcodeValue    NVARCHAR(100) NOT NULL,
    IsDefault       BIT NOT NULL DEFAULT 0,
    SupplierCode    NVARCHAR(100) NULL, -- Optional: which supplier uses this code
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_UnitBarcodes_ProductUnits
        FOREIGN KEY (ProductUnitId) REFERENCES ProductUnits(Id)
            ON DELETE CASCADE,

    CONSTRAINT UQ_UnitBarcodes_Value
        UNIQUE (BarcodeValue)
);

CREATE INDEX IX_UnitBarcodes_Value ON UnitBarcodes(BarcodeValue);

-- =============================================
-- STEP 4: Cash Boxes
-- =============================================
CREATE TABLE CashBoxes (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    BoxName         NVARCHAR(100) NOT NULL,
    CurrentBalance  DECIMAL(18,4) NOT NULL DEFAULT 0,
    BranchId        INT NULL,
    CurrencyCode    NVARCHAR(10) NOT NULL DEFAULT 'SAR',
    AssignedUserId  INT NULL,         -- NULL = shared box
    IsActive        BIT NOT NULL DEFAULT 1,
    Notes           NVARCHAR(500) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- =============================================
-- STEP 5: Cash Transactions Log
-- =============================================
CREATE TABLE CashTransactions (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    CashBoxId       INT NOT NULL,
    TransactionType INT NOT NULL, -- 0=SaleIn, 1=PurchaseOut, 2=TransferIn, 3=TransferOut, 4=Manual
    Amount          DECIMAL(18,4) NOT NULL,
    BalanceBefore   DECIMAL(18,4) NOT NULL,  -- Snapshot for audit
    BalanceAfter    DECIMAL(18,4) NOT NULL,  -- Snapshot for audit
    ReferenceType   NVARCHAR(50) NULL,       -- "SalesInvoice", "PurchaseInvoice"
    ReferenceId     INT NULL,
    Notes           NVARCHAR(500) NULL,
    CreatedBy       INT NOT NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_CashTransactions_CashBoxes
        FOREIGN KEY (CashBoxId) REFERENCES CashBoxes(Id)
);

CREATE INDEX IX_CashTransactions_CashBoxId ON CashTransactions(CashBoxId);
CREATE INDEX IX_CashTransactions_Reference ON CashTransactions(ReferenceType, ReferenceId);

-- =============================================
-- STEP 6: System Settings (Costing Strategy)
-- =============================================
CREATE TABLE SystemSettings (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    SettingKey      NVARCHAR(100) NOT NULL UNIQUE,
    SettingValue    NVARCHAR(500) NOT NULL,
    DataType        NVARCHAR(50) NOT NULL DEFAULT 'string', -- 'string','int','bool','decimal'
    Category        NVARCHAR(100) NOT NULL,
    DisplayName     NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(1000) NULL,
    UpdatedBy       INT NULL,
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- Seed costing strategy default
INSERT INTO SystemSettings (SettingKey, SettingValue, DataType, Category, DisplayName, Description)
VALUES (
    'CostingMethod',
    'WeightedAverage',
    'string',
    'Inventory',
    'طريقة احتساب التكلفة',
    'تحدد كيف يحتسب النظام تكلفة البضاعة في المخزن'
);

-- =============================================
-- STEP 7: Add CashBoxId to invoices
-- =============================================
ALTER TABLE SalesInvoices    ADD CashBoxId INT NULL;
ALTER TABLE PurchaseInvoices ADD CashBoxId INT NULL;

ALTER TABLE SalesInvoices    ADD CONSTRAINT FK_Sales_CashBox
    FOREIGN KEY (CashBoxId) REFERENCES CashBoxes(Id);
ALTER TABLE PurchaseInvoices ADD CONSTRAINT FK_Purchase_CashBox
    FOREIGN KEY (CashBoxId) REFERENCES CashBoxes(Id);

-- =============================================
-- STEP 8: Price History Log (Audit Trail)
-- =============================================
CREATE TABLE ProductPriceHistory (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    ProductUnitId   INT NOT NULL,
    ChangeType      NVARCHAR(50) NOT NULL, -- 'PurchaseCost','SalesPrice','SupplierPrice'
    OldValue        DECIMAL(18,4) NOT NULL,
    NewValue        DECIMAL(18,4) NOT NULL,
    CostingMethod   NVARCHAR(50) NULL,
    InvoiceId       INT NULL,
    ChangedBy       INT NOT NULL,
    ChangedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_PriceHistory_ProductUnits
        FOREIGN KEY (ProductUnitId) REFERENCES ProductUnits(Id)
);
✅ Phase 0 Checklist
 All 8 SQL blocks executed without errors
 ProductUnits has CHECK constraint on base unit factor
 UnitBarcodes has UNIQUE constraint on barcode value
 CashBoxes linked to invoices tables
 SystemSettings seeded with default costing method
 ProductPriceHistory created for audit trail
🏗️ Phase 1: Domain Layer — Entities & Enums
Task 1.1 — New Enums
csharp

// File: Domain/Enums/CostingMethod.cs
public enum CostingMethod
{
    WeightedAverage = 0,    // متوسط التكلفة المرجح
    LastPurchasePrice = 1,  // آخر سعر توريد
    SupplierPrice = 2       // سعر المورد
}

// File: Domain/Enums/CashTransactionType.cs
public enum CashTransactionType
{
    SaleIn = 0,         // مبيعات (وارد)
    PurchaseOut = 1,    // مشتريات (صادر)
    TransferIn = 2,     // تحويل وارد
    TransferOut = 3,    // تحويل صادر
    ManualIn = 4,       // إيداع يدوي
    ManualOut = 5       // سحب يدوي
}
Task 1.2 — ProductUnit Entity
csharp

// File: Domain/Entities/ProductUnit.cs

public class ProductUnit : BaseEntity
{
    // ─── Properties ───────────────────────────────
    public int ProductId { get; private set; }
    public string UnitName { get; private set; }

    /// <summary>
    /// How many BASE UNITS does this unit contain?
    /// Base unit itself = 1. Box of 12 = 12. Pallet of 360 = 360.
    /// </summary>
    public decimal BaseConversionFactor { get; private set; }

    public bool IsBaseUnit { get; private set; }
    public decimal SalesPrice { get; private set; }
    public decimal PurchaseCost { get; private set; }
    public decimal SupplierPrice { get; private set; }
    public decimal LastPurchasePrice { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation
    public Product Product { get; private set; }

    private readonly List<UnitBarcode> _barcodes = new();
    public IReadOnlyCollection<UnitBarcode> Barcodes => _barcodes.AsReadOnly();

    private ProductUnit() { } // EF Core

    // ─── Factory ──────────────────────────────────
    public static ProductUnit CreateBaseUnit(
        int productId,
        string unitName)
    {
        if (string.IsNullOrWhiteSpace(unitName))
            throw new DomainException("Unit name cannot be empty");

        return new ProductUnit
        {
            ProductId = productId,
            UnitName = unitName.Trim(),
            BaseConversionFactor = 1,
            IsBaseUnit = true,
            IsActive = true,
            SortOrder = 0
        };
    }

    public static ProductUnit CreateDerivedUnit(
        int productId,
        string unitName,
        decimal baseConversionFactor,
        int sortOrder = 1)
    {
        if (string.IsNullOrWhiteSpace(unitName))
            throw new DomainException("Unit name cannot be empty");

        if (baseConversionFactor <= 1)
            throw new DomainException(
                $"وحدة '{unitName}' يجب أن تحتوي على أكثر من وحدة صغرى واحدة. " +
                $"أدخل كم وحدة صغرى بداخلها (مثال: الكرتون يحتوي على 12 حبة، ادخل 12).");

        return new ProductUnit
        {
            ProductId = productId,
            UnitName = unitName.Trim(),
            BaseConversionFactor = baseConversionFactor,
            IsBaseUnit = false,
            IsActive = true,
            SortOrder = sortOrder
        };
    }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Converts quantity in this unit to base unit quantity.
    /// ALWAYS use this before touching stock.
    /// </summary>
    public decimal ToBaseUnitQuantity(decimal quantity)
        => quantity * BaseConversionFactor;

    /// <summary>
    /// Updates cost after purchase. Returns old cost for history logging.
    /// </summary>
    public decimal UpdatePurchaseCost(decimal newCost)
    {
        if (newCost < 0)
            throw new DomainException("التكلفة لا يمكن أن تكون سالبة");

        var oldCost = PurchaseCost;
        LastPurchasePrice = newCost;
        PurchaseCost = newCost;
        return oldCost;
    }

    public decimal UpdateSalesPrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new DomainException("سعر البيع لا يمكن أن يكون سالباً");

        var oldPrice = SalesPrice;
        SalesPrice = newPrice;
        return oldPrice;
    }

    public void UpdateSupplierPrice(decimal supplierPrice)
    {
        if (supplierPrice < 0)
            throw new DomainException("سعر المورد لا يمكن أن يكون سالباً");
        SupplierPrice = supplierPrice;
    }

    public void AddBarcode(string barcodeValue, bool isDefault = false, 
        string? supplierCode = null)
    {
        if (string.IsNullOrWhiteSpace(barcodeValue))
            throw new DomainException("قيمة الباركود لا يمكن أن تكون فارغة");

        // If this is default, unmark others
        if (isDefault)
        {
            foreach (var b in _barcodes)
                b.UnmarkDefault();
        }

        _barcodes.Add(UnitBarcode.Create(Id, barcodeValue, isDefault, supplierCode));
    }

    /// <summary>
    /// Calculates cost for this unit based on base unit cost.
    /// e.g., if base unit costs 1 SAR and this unit = 12 pieces → cost = 12 SAR
    /// </summary>
    public decimal CalculateCostFromBaseUnitCost(decimal baseUnitCost)
        => baseUnitCost * BaseConversionFactor;
}
Task 1.3 — UnitBarcode Entity
csharp

// File: Domain/Entities/UnitBarcode.cs

public class UnitBarcode : BaseEntity
{
    public int ProductUnitId { get; private set; }
    public string BarcodeValue { get; private set; }
    public bool IsDefault { get; private set; }
    public string? SupplierCode { get; private set; }

    private UnitBarcode() { }

    public static UnitBarcode Create(
        int productUnitId,
        string barcodeValue,
        bool isDefault = false,
        string? supplierCode = null)
    {
        return new UnitBarcode
        {
            ProductUnitId = productUnitId,
            BarcodeValue = barcodeValue.Trim().ToUpperInvariant(),
            IsDefault = isDefault,
            SupplierCode = supplierCode?.Trim()
        };
    }

    public void UnmarkDefault() => IsDefault = false;
}
Task 1.4 — CashBox Entity
csharp

// File: Domain/Entities/CashBox.cs

public class CashBox : BaseEntity
{
    public string BoxName { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public int? BranchId { get; private set; }
    public string CurrencyCode { get; private set; }
    public int? AssignedUserId { get; private set; }    // NULL = shared
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<CashTransaction> _transactions = new();
    public IReadOnlyCollection<CashTransaction> Transactions 
        => _transactions.AsReadOnly();

    private CashBox() { }

    public static CashBox Create(
        string boxName,
        int? branchId = null,
        int? assignedUserId = null,
        string currencyCode = "SAR",
        decimal initialBalance = 0)
    {
        if (string.IsNullOrWhiteSpace(boxName))
            throw new DomainException("اسم الصندوق مطلوب");

        return new CashBox
        {
            BoxName = boxName.Trim(),
            BranchId = branchId,
            AssignedUserId = assignedUserId,
            CurrencyCode = currencyCode,
            CurrentBalance = initialBalance,
            IsActive = true
        };
    }

    // ─── Domain Methods ───────────────────────────

    public CashTransaction Deposit(
        decimal amount,
        CashTransactionType type,
        string? referenceType = null,
        int? referenceId = null,
        int createdBy = 0,
        string? notes = null)
    {
        if (amount <= 0)
            throw new DomainException("مبلغ الإيداع يجب أن يكون أكبر من صفر");

        var balanceBefore = CurrentBalance;
        CurrentBalance += amount;

        var transaction = CashTransaction.Create(
            Id, type, amount, balanceBefore, CurrentBalance,
            referenceType, referenceId, createdBy, notes);

        _transactions.Add(transaction);
        return transaction;
    }

    public CashTransaction Withdraw(
        decimal amount,
        CashTransactionType type,
        string? referenceType = null,
        int? referenceId = null,
        int createdBy = 0,
        string? notes = null)
    {
        if (amount <= 0)
            throw new DomainException("مبلغ السحب يجب أن يكون أكبر من صفر");

        if (CurrentBalance < amount)
            throw new DomainException(
                $"رصيد الصندوق غير كافٍ. الرصيد الحالي: {CurrentBalance:N2}، " +
                $"المبلغ المطلوب: {amount:N2}");

        var balanceBefore = CurrentBalance;
        CurrentBalance -= amount;

        var transaction = CashTransaction.Create(
            Id, type, -amount, balanceBefore, CurrentBalance,
            referenceType, referenceId, createdBy, notes);

        _transactions.Add(transaction);
        return transaction;
    }

    public void CanUserAccess(int userId)
    {
        if (AssignedUserId.HasValue && AssignedUserId.Value != userId)
            throw new DomainException(
                $"ليس لديك صلاحية الوصول إلى الصندوق '{BoxName}'. " +
                $"تواصل مع المدير لتغيير الصلاحيات.");
    }
}
Task 1.5 — CashTransaction Entity
csharp

// File: Domain/Entities/CashTransaction.cs

public class CashTransaction : BaseEntity
{
    public int CashBoxId { get; private set; }
    public CashTransactionType TransactionType { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string? ReferenceType { get; private set; }
    public int? ReferenceId { get; private set; }
    public string? Notes { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CashTransaction() { }

    internal static CashTransaction Create(
        int cashBoxId,
        CashTransactionType type,
        decimal amount,
        decimal balanceBefore,
        decimal balanceAfter,
        string? referenceType,
        int? referenceId,
        int createdBy,
        string? notes)
    {
        return new CashTransaction
        {
            CashBoxId = cashBoxId,
            TransactionType = type,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedBy = createdBy,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }
}
Task 1.6 — Update Product Entity
csharp

// File: Domain/Entities/Product.cs
// MODIFY existing Product — remove old price fields, add Units collection

public class Product : BaseEntity
{
    // ─── Keep existing fields ─────────────────────
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int CategoryId { get; private set; }
    public bool IsActive { get; private set; }

    // ─── REMOVED: WholesalePrice, RetailPrice, ConversionFactor ───
    // These now live in ProductUnits

    // Navigation
    private readonly List<ProductUnit> _units = new();
    public IReadOnlyCollection<ProductUnit> Units => _units.AsReadOnly();

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Returns the base unit (Piece/Egg/etc). ALWAYS exists.
    /// Throws if not found — product data is corrupted.
    /// </summary>
    public ProductUnit GetBaseUnit()
    {
        return _units.FirstOrDefault(u => u.IsBaseUnit)
            ?? throw new DomainException(
                $"المنتج '{Name}' لا يحتوي على وحدة أساسية. " +
                $"يرجى تعريف وحدة صغرى أولاً (مثال: حبة) من شاشة إدارة المنتجات.");
    }

    public ProductUnit GetUnitById(int unitId)
    {
        return _units.FirstOrDefault(u => u.Id == unitId)
            ?? throw new DomainException(
                $"الوحدة المحددة غير موجودة في المنتج '{Name}'");
    }

    /// <summary>
    /// Validates product has exactly ONE base unit before saving.
    /// Call this in the Command Handler before persisting.
    /// </summary>
    public void ValidateUnits()
    {
        var baseUnits = _units.Where(u => u.IsBaseUnit).ToList();

        if (baseUnits.Count == 0)
            throw new DomainException(
                "⚠️ يجب تعريف وحدة صغرى واحدة على الأقل.\n" +
                "مثال: أضف وحدة باسم 'حبة' واجعل معامل التحويل = 1");

        if (baseUnits.Count > 1)
            throw new DomainException(
                "⚠️ لا يمكن تعريف أكثر من وحدة صغرى واحدة للمنتج الواحد.\n" +
                $"الوحدات المعرّفة كأساسية: {string.Join(", ", baseUnits.Select(u => u.UnitName))}");

        var invalidDerived = _units
            .Where(u => !u.IsBaseUnit && u.BaseConversionFactor <= 1)
            .ToList();

        if (invalidDerived.Any())
            throw new DomainException(
                $"⚠️ الوحدات التالية لها معامل تحويل غير صحيح:\n" +
                $"{string.Join("\n", invalidDerived.Select(u => $"- {u.UnitName}: يجب أن يكون أكبر من 1"))}\n" +
                $"أدخل كم وحدة صغرى بداخل كل وحدة أكبر.");
    }
}
✅ Phase 1 Checklist
 ProductUnit.CreateBaseUnit() sets factor to 1 automatically
 ProductUnit.CreateDerivedUnit() rejects factor <= 1 with Arabic message
 Product.ValidateUnits() catches 0 base units, >1 base units, and invalid factors
 CashBox.Withdraw() rejects insufficient balance with Arabic message
 CashBox.CanUserAccess() enforces user-box assignment
 All error messages are in Arabic and user-friendly
⚙️ Phase 2: Infrastructure — EF Core & Services
Task 2.1 — EF Configurations
csharp

// File: Infrastructure/Persistence/Configurations/ProductUnitConfiguration.cs

public class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.ToTable("ProductUnits");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UnitName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.BaseConversionFactor).HasPrecision(18, 6);
        builder.Property(x => x.SalesPrice).HasPrecision(18, 4);
        builder.Property(x => x.PurchaseCost).HasPrecision(18, 4);
        builder.Property(x => x.SupplierPrice).HasPrecision(18, 4);
        builder.Property(x => x.LastPurchasePrice).HasPrecision(18, 4);

        // Enforce: base unit must have factor = 1
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_BaseUnitFactor",
            "IsBaseUnit = 0 OR BaseConversionFactor = 1"));

        builder.HasMany(x => x.Barcodes)
            .WithOne()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Units)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: Infrastructure/Persistence/Configurations/CashBoxConfiguration.cs

public class CashBoxConfiguration : IEntityTypeConfiguration<CashBox>
{
    public void Configure(EntityTypeBuilder<CashBox> builder)
    {
        builder.ToTable("CashBoxes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BoxName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CurrentBalance).HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).HasMaxLength(10);

        builder.HasMany(x => x.Transactions)
            .WithOne()
            .HasForeignKey(x => x.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict); // Keep history if box deleted
    }
}
Task 2.2 — Barcode Lookup Service (Updated)
csharp

// File: Infrastructure/Services/BarcodeLookupService.cs

public interface IBarcodeLookupService
{
    Task<BarcodeSearchResult?> LookupAsync(string barcode, CancellationToken ct = default);
}

public record BarcodeSearchResult(
    int ProductId,
    string ProductName,
    int ProductUnitId,
    string UnitName,
    decimal BaseConversionFactor,
    bool IsBaseUnit,
    decimal SalesPrice,
    decimal PurchaseCost,
    decimal CurrentStockInBaseUnits
);

public class BarcodeLookupService : IBarcodeLookupService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BarcodeLookupService> _logger;

    public BarcodeLookupService(AppDbContext context, ILogger<BarcodeLookupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BarcodeSearchResult?> LookupAsync(
        string barcode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        var normalized = barcode.Trim().ToUpperInvariant();

        // Search in UnitBarcodes table (new multi-barcode system)
        var result = await _context.UnitBarcodes
            .Where(b => b.BarcodeValue == normalized)
            .Select(b => new BarcodeSearchResult(
                b.ProductUnit.ProductId,
                b.ProductUnit.Product.Name,
                b.ProductUnitId,
                b.ProductUnit.UnitName,
                b.ProductUnit.BaseConversionFactor,
                b.ProductUnit.IsBaseUnit,
                b.ProductUnit.SalesPrice,
                b.ProductUnit.PurchaseCost,
                b.ProductUnit.Product.Stock.CurrentQuantityInPieces
            ))
            .FirstOrDefaultAsync(ct);

        if (result == null)
            _logger.LogWarning("Barcode not found: {Barcode}", normalized);

        return result;
    }
}
Task 2.3 — Settings Repository
csharp

// File: Infrastructure/Repositories/SystemSettingsRepository.cs

public interface ISystemSettingsRepository
{
    Task<CostingMethod> GetCostingMethodAsync(CancellationToken ct = default);
    Task SetCostingMethodAsync(CostingMethod method, CancellationToken ct = default);
}

public class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly AppDbContext _context;

    public SystemSettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CostingMethod> GetCostingMethodAsync(CancellationToken ct = default)
    {
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == "CostingMethod", ct);

        if (setting == null) return CostingMethod.WeightedAverage; // Safe default

        return Enum.TryParse<CostingMethod>(setting.SettingValue, out var method)
            ? method
            : CostingMethod.WeightedAverage;
    }

    public async Task SetCostingMethodAsync(CostingMethod method, CancellationToken ct = default)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == "CostingMethod", ct);

        if (setting != null)
            setting.UpdateValue(method.ToString());

        await _context.SaveChangesAsync(ct);
    }
}
✅ Phase 2 Checklist
 EF Core configurations registered in AppDbContext.OnModelCreating()
 BarcodeLookupService searches UnitBarcodes (not old Barcode column)
 SystemSettingsRepository returns safe default if setting missing
 All repositories registered in DI container
⚙️ Phase 3: Application Layer — Pricing Service
Task 3.1 — UpdateProductPricingService
csharp

// File: Application/Services/UpdateProductPricingService.cs

public interface IUpdateProductPricingService
{
    Task UpdateFromPurchaseAsync(
        UpdatePricingRequest request,
        CancellationToken ct = default);
}

public record UpdatePricingRequest(
    int ProductUnitId,
    decimal NewPurchaseCost,
    decimal NewQuantityPurchased,
    decimal? NewSalesPrice,          // Optional — user may override
    int InvoiceId,
    int ChangedBy
);

public class UpdateProductPricingService : IUpdateProductPricingService
{
    private readonly AppDbContext _context;
    private readonly ISystemSettingsRepository _settings;
    private readonly ILogger<UpdateProductPricingService> _logger;

    public UpdateProductPricingService(
        AppDbContext context,
        ISystemSettingsRepository settings,
        ILogger<UpdateProductPricingService> logger)
    {
        _context = context;
        _settings = settings;
        _logger = logger;
    }

    public async Task UpdateFromPurchaseAsync(
        UpdatePricingRequest request,
        CancellationToken ct = default)
    {
        // ─── 1. Load the purchased unit and ALL units for this product ───
        var purchasedUnit = await _context.ProductUnits
            .Include(u => u.Product)
                .ThenInclude(p => p.Units)
            .FirstOrDefaultAsync(u => u.Id == request.ProductUnitId, ct)
            ?? throw new NotFoundException("ProductUnit", request.ProductUnitId);

        var product = purchasedUnit.Product;
        var allUnits = product.Units.Where(u => u.IsActive).ToList();
        var baseUnit = product.GetBaseUnit();

        // ─── 2. Calculate new BASE UNIT cost ─────────────────────────────
        var costingMethod = await _settings.GetCostingMethodAsync(ct);

        var newBaseUnitCost = await CalculateNewBaseUnitCostAsync(
            costingMethod,
            baseUnit,
            purchasedUnit,
            request.NewPurchaseCost,
            request.NewQuantityPurchased,
            ct);

        _logger.LogInformation(
            "Updating costs for Product {ProductId} using {Method}. " +
            "New base unit cost: {Cost}",
            product.Id, costingMethod, newBaseUnitCost);

        // ─── 3. Cascade cost update to ALL units ─────────────────────────
        var historyEntries = new List<ProductPriceHistory>();

        foreach (var unit in allUnits)
        {
            var newUnitCost = unit.CalculateCostFromBaseUnitCost(newBaseUnitCost);
            var oldCost = unit.UpdatePurchaseCost(newUnitCost);

            historyEntries.Add(new ProductPriceHistory
            {
                ProductUnitId = unit.Id,
                ChangeType = "PurchaseCost",
                OldValue = oldCost,
                NewValue = newUnitCost,
                CostingMethod = costingMethod.ToString(),
                InvoiceId = request.InvoiceId,
                ChangedBy = request.ChangedBy,
                ChangedAt = DateTime.UtcNow
            });
        }

        // ─── 4. Update sales price if user provided one ──────────────────
        if (request.NewSalesPrice.HasValue && request.NewSalesPrice.Value > 0)
        {
            var oldSalesPrice = purchasedUnit.UpdateSalesPrice(request.NewSalesPrice.Value);

            historyEntries.Add(new ProductPriceHistory
            {
                ProductUnitId = purchasedUnit.Id,
                ChangeType = "SalesPrice",
                OldValue = oldSalesPrice,
                NewValue = request.NewSalesPrice.Value,
                InvoiceId = request.InvoiceId,
                ChangedBy = request.ChangedBy,
                ChangedAt = DateTime.UtcNow
            });
        }

        // ─── 5. Save history ──────────────────────────────────────────────
        _context.ProductPriceHistory.AddRange(historyEntries);
        await _context.SaveChangesAsync(ct);
    }

    private async Task<decimal> CalculateNewBaseUnitCostAsync(
        CostingMethod method,
        ProductUnit baseUnit,
        ProductUnit purchasedUnit,
        decimal invoiceCostForPurchasedUnit,
        decimal quantityPurchased,
        CancellationToken ct)
    {
        // Convert invoice cost to base unit cost first
        var newBaseCostFromInvoice = purchasedUnit.IsBaseUnit
            ? invoiceCostForPurchasedUnit
            : invoiceCostForPurchasedUnit / purchasedUnit.BaseConversionFactor;

        return method switch
        {
            CostingMethod.LastPurchasePrice =>
                // Simple: just use the new price
                newBaseCostFromInvoice,

            CostingMethod.SupplierPrice =>
                // Use the supplier catalog price (don't change cost from invoice)
                baseUnit.SupplierPrice > 0
                    ? baseUnit.SupplierPrice
                    : newBaseCostFromInvoice,

            CostingMethod.WeightedAverage =>
                // Weighted average: [(OldStock × OldCost) + (NewQty × NewCost)] / TotalQty
                await CalculateWeightedAverageAsync(
                    baseUnit,
                    newBaseCostFromInvoice,
                    quantityPurchased * purchasedUnit.BaseConversionFactor,
                    ct),

            _ => newBaseCostFromInvoice
        };
    }

    private async Task<decimal> CalculateWeightedAverageAsync(
        ProductUnit baseUnit,
        decimal newBaseUnitCost,
        decimal newQuantityInBaseUnits,
        CancellationToken ct)
    {
        // Get current stock in base units
        var currentStock = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.ProductId == baseUnit.ProductId)
            .Select(s => s.CurrentQuantityInPieces)
            .FirstOrDefaultAsync(ct);

        var oldCost = baseUnit.PurchaseCost;

        // If no existing stock, just use new cost
        if (currentStock <= 0) return newBaseUnitCost;

        // Weighted Average Formula
        var weightedAverage =
            ((currentStock * oldCost) + (newQuantityInBaseUnits * newBaseUnitCost))
            / (currentStock + newQuantityInBaseUnits);

        return Math.Round(weightedAverage, 4);
    }
}
Task 3.2 — Purchase Invoice Command (Updated)
csharp

// File: Application/Commands/CreatePurchaseInvoice/CreatePurchaseInvoiceCommand.cs

public record CreatePurchaseInvoiceCommand : IRequest<int>
{
    public int SupplierId { get; init; }
    public int CashBoxId { get; init; }     // NEW: Which cash box pays
    public int CashierId { get; init; }
    public string? Notes { get; init; }
    public List<PurchaseInvoiceItemRequest> Items { get; init; } = new();
}

public record PurchaseInvoiceItemRequest(
    int ProductUnitId,
    decimal Quantity,
    decimal UnitCost,
    decimal? NewSalesPrice,     // Optional: override sales price from invoice screen
    decimal Discount
);
csharp

// File: Application/Commands/CreatePurchaseInvoice/CreatePurchaseInvoiceCommandHandler.cs

public class CreatePurchaseInvoiceCommandHandler
    : IRequestHandler<CreatePurchaseInvoiceCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUpdateProductPricingService _pricingService;
    private readonly AppDbContext _context;

    public CreatePurchaseInvoiceCommandHandler(
        IUnitOfWork unitOfWork,
        IUpdateProductPricingService pricingService,
        AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _pricingService = pricingService;
        _context = context;
    }

    public async Task<int> Handle(
        CreatePurchaseInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        // ─── 1. Validate CashBox access ──────────────────────────────────
        var cashBox = await _unitOfWork.CashBoxes
            .GetByIdAsync(command.CashBoxId, cancellationToken)
            ?? throw new NotFoundException("CashBox", command.CashBoxId);

        cashBox.CanUserAccess(command.CashierId); // Throws if no permission

        // ─── 2. Create invoice ────────────────────────────────────────────
        var invoice = PurchaseInvoice.Create(
            command.SupplierId,
            command.CashBoxId,
            command.CashierId,
            command.Notes);

        decimal totalAmount = 0;

        foreach (var itemRequest in command.Items)
        {
            // Load unit with product
            var productUnit = await _context.ProductUnits
                .Include(u => u.Product)
                    .ThenInclude(p => p.Units)
                .Include(u => u.Product)
                    .ThenInclude(p => p.Stock)
                .FirstOrDefaultAsync(u => u.Id == itemRequest.ProductUnitId, cancellationToken)
                ?? throw new NotFoundException("ProductUnit", itemRequest.ProductUnitId);

            // Add to invoice
            invoice.AddItem(
                productUnit.Id,
                productUnit.Product.Name,
                productUnit.UnitName,
                itemRequest.Quantity,
                itemRequest.UnitCost,
                itemRequest.Discount);

            // Add stock — Domain converts to base units internally
            productUnit.Product.Stock.AddStock(
                itemRequest.Quantity,
                productUnit.BaseConversionFactor);

            totalAmount += (itemRequest.Quantity * itemRequest.UnitCost) - itemRequest.Discount;
        }

        // ─── 3. Deduct from cash box ──────────────────────────────────────
        cashBox.Withdraw(
            totalAmount,
            CashTransactionType.PurchaseOut,
            referenceType: "PurchaseInvoice",
            referenceId: invoice.Id,
            createdBy: command.CashierId,
            notes: $"دفع فاتورة مشتريات رقم {invoice.Id}");

        // ─── 4. Save invoice and stock ────────────────────────────────────
        _unitOfWork.PurchaseInvoices.Add(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ─── 5. Update product pricing (AFTER save so invoice.Id exists) ──
        foreach (var itemRequest in command.Items)
        {
            await _pricingService.UpdateFromPurchaseAsync(
                new UpdatePricingRequest(
                    itemRequest.ProductUnitId,
                    itemRequest.UnitCost,
                    itemRequest.Quantity,
                    itemRequest.NewSalesPrice,
                    invoice.Id,
                    command.CashierId),
                cancellationToken);
        }

        return invoice.Id;
    }
}
Task 3.3 — Cash Transfer Command
csharp

// File: Application/Commands/TransferCash/TransferCashCommand.cs

public record TransferCashCommand(
    int FromCashBoxId,
    int ToCashBoxId,
    decimal Amount,
    int TransferredBy,
    string? Notes
) : IRequest<Unit>;

public class TransferCashCommandHandler
    : IRequestHandler<TransferCashCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransferCashCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        TransferCashCommand command,
        CancellationToken cancellationToken)
    {
        if (command.FromCashBoxId == command.ToCashBoxId)
            throw new DomainException("لا يمكن التحويل من الصندوق إلى نفسه");

        var fromBox = await _unitOfWork.CashBoxes
            .GetByIdAsync(command.FromCashBoxId, cancellationToken)
            ?? throw new NotFoundException("CashBox", command.FromCashBoxId);

        var toBox = await _unitOfWork.CashBoxes
            .GetByIdAsync(command.ToCashBoxId, cancellationToken)
            ?? throw new NotFoundException("CashBox", command.ToCashBoxId);

        fromBox.CanUserAccess(command.TransferredBy);

        // These two domain calls maintain balance integrity
        fromBox.Withdraw(command.Amount, CashTransactionType.TransferOut,
            notes: $"تحويل إلى: {toBox.BoxName} | {command.Notes}",
            createdBy: command.TransferredBy);

        toBox.Deposit(command.Amount, CashTransactionType.TransferIn,
            notes: $"تحويل من: {fromBox.BoxName} | {command.Notes}",
            createdBy: command.TransferredBy);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
✅ Phase 3 Checklist
 WeightedAverage formula is correct: (OldQty×OldCost + NewQty×NewCost) / TotalQty
 LastPurchasePrice just overwrites — no formula
 Cost cascade goes to ALL units (base and derived)
 Derived unit cost = baseUnitCost × ConversionFactor
 Pricing update happens AFTER invoice saved (so ID exists for history)
 Cash transfer uses domain methods (not direct property assignment)
🖥️ Phase 4: WPF ViewModels
Task 4.1 — Product Unit Builder ViewModel
csharp

// File: WPF/ViewModels/Products/ProductUnitBuilderViewModel.cs

public class ProductUnitBuilderViewModel : BaseViewModel
{
    private bool _hasShownOnboarding = false;

    public ObservableCollection<ProductUnitRowViewModel> Units { get; } = new();

    // Validation summary shown in UI
    public string ValidationSummary { get; private set; } = string.Empty;
    public bool HasValidationError { get; private set; }

    // Commands
    public IRelayCommand AddUnitCommand { get; }
    public IRelayCommand<ProductUnitRowViewModel> RemoveUnitCommand { get; }
    public IRelayCommand ShowHelpCommand { get; }

    public ProductUnitBuilderViewModel()
    {
        AddUnitCommand = new RelayCommand(AddNewUnit);
        RemoveUnitCommand = new RelayCommand<ProductUnitRowViewModel>(RemoveUnit);
        ShowHelpCommand = new RelayCommand(ShowOnboarding);

        Units.CollectionChanged += (_, _) => Validate();
    }

    public void Initialize(List<ProductUnitRowViewModel>? existingUnits = null)
    {
        Units.Clear();

        if (existingUnits?.Any() == true)
        {
            foreach (var unit in existingUnits.OrderBy(u => u.SortOrder))
                AddUnitWithChangeTracking(unit);
        }
        else
        {
            // New product — show onboarding and pre-add base unit row
            ShowOnboarding();
            AddBaseUnitRow();
        }
    }

    private void AddBaseUnitRow()
    {
        var baseRow = new ProductUnitRowViewModel
        {
            IsBaseUnit = true,
            BaseConversionFactor = 1,
            SortOrder = 0,
            Placeholder_UnitName = "مثال: حبة، قطعة، بيضة"
        };
        baseRow.PropertyChanged += (_, _) => Validate();
        Units.Add(baseRow);
    }

    private void AddNewUnit()
    {
        var row = new ProductUnitRowViewModel
        {
            IsBaseUnit = false,
            SortOrder = Units.Count,
            Placeholder_UnitName = "مثال: طبق، كرتون"
        };
        row.PropertyChanged += (_, _) => Validate();
        Units.Add(row);
    }

    private void AddUnitWithChangeTracking(ProductUnitRowViewModel unit)
    {
        unit.PropertyChanged += (_, _) => Validate();
        Units.Add(unit);
    }

    private void RemoveUnit(ProductUnitRowViewModel unit)
    {
        if (unit.IsBaseUnit && Units.Count > 1)
        {
            ValidationSummary = "⚠️ لا يمكن حذف الوحدة الأساسية إذا كانت هناك وحدات أخرى مرتبطة بها.";
            HasValidationError = true;
            OnPropertyChanged(nameof(ValidationSummary));
            OnPropertyChanged(nameof(HasValidationError));
            return;
        }

        Units.Remove(unit);
        Validate();
    }

    public bool Validate()
    {
        var errors = new List<string>();

        var baseUnits = Units.Where(u => u.IsBaseUnit).ToList();

        if (baseUnits.Count == 0)
            errors.Add("⚠️ أضف وحدة صغرى واحدة (مثال: حبة) واجعل معامل التحويل = 1");

        if (baseUnits.Count > 1)
            errors.Add("⚠️ لا يمكن تعريف أكثر من وحدة صغرى واحدة");

        foreach (var unit in Units)
        {
            if (string.IsNullOrWhiteSpace(unit.UnitName))
                errors.Add($"⚠️ الصف {unit.SortOrder + 1}: اسم الوحدة مطلوب");

            if (!unit.IsBaseUnit && unit.BaseConversionFactor <= 1)
                errors.Add(
                    $"⚠️ '{unit.UnitName}': معامل التحويل يجب أن يكون أكبر من 1 " +
                    $"(كم وحدة صغرى بداخلها؟)");
        }

        ValidationSummary = errors.Any()
            ? string.Join("\n", errors)
            : "✅ وحدات المنتج صحيحة";

        HasValidationError = errors.Any();

        OnPropertyChanged(nameof(ValidationSummary));
        OnPropertyChanged(nameof(HasValidationError));

        return !errors.Any();
    }

    private void ShowOnboarding()
    {
        var dialog = new OnboardingDialog
        {
            Message =
                "💡 كيف تبني وحدات المنتج؟\n\n" +
                "1️⃣  ابدأ دائماً بإضافة الوحدة الصغرى\n" +
                "     التي لا يمكن تجزئتها (مثل: حبة)\n" +
                "     واجعل معامل التحويل = 1\n\n" +
                "2️⃣  ثم أضف الوحدات الأكبر\n" +
                "     (مثل: طبق، كرتون)\n" +
                "     واكتب كم (حبة) بداخلها.\n\n" +
                "     مثال: طبق البيض = 30 حبة\n" +
                "              كرتون = 12 طبق = 360 حبة\n\n" +
                "✅  النظام سيحسب كل شيء تلقائياً!"
        };
        dialog.ShowDialog();
    }
}
Task 4.2 — Purchase Invoice ViewModel (with Price Sync Indicator)
csharp

// File: WPF/ViewModels/Invoice/PurchaseInvoiceItemViewModel.cs

public class PurchaseInvoiceItemViewModel : BaseViewModel
{
    private readonly IMediator _mediator;

    private int _productUnitId;
    private decimal _quantity = 1;
    private decimal _unitCost;
    private decimal _newSalesPrice;
    private decimal _oldCostInDatabase;
    private string _unitName = string.Empty;

    // ─── Properties ───────────────────────────────

    public int ProductUnitId
    {
        get => _productUnitId;
        set => SetProperty(ref _productUnitId, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
                OnPropertyChanged(nameof(TotalCost));
        }
    }

    public decimal UnitCost
    {
        get => _unitCost;
        set
        {
            if (SetProperty(ref _unitCost, value))
            {
                OnPropertyChanged(nameof(TotalCost));
                OnPropertyChanged(nameof(CostChangedFromDatabase));
                OnPropertyChanged(nameof(PriceDifferenceIndicator));
            }
        }
    }

    public decimal NewSalesPrice
    {
        get => _newSalesPrice;
        set => SetProperty(ref _newSalesPrice, value);
    }

    public decimal TotalCost => (Quantity * UnitCost) - Discount;
    public decimal Discount { get; set; }

    // ⭐ KEY: Shows sync warning icon when cost differs from DB
    public bool CostChangedFromDatabase =>
        _oldCostInDatabase > 0 &&
        Math.Abs(UnitCost - _oldCostInDatabase) > 0.0001m;

    public string PriceDifferenceIndicator
    {
        get
        {
            if (!CostChangedFromDatabase) return string.Empty;

            var diff = UnitCost - _oldCostInDatabase;
            var direction = diff > 0 ? "↑ ارتفع" : "↓ انخفض";
            return $"🔄 {direction} عن السعر القديم ({_oldCostInDatabase:N2}) " +
                   $"| سيتم تحديث التكلفة في بطاقة الصنف عند الحفظ";
        }
    }

    // ─── Available units for ComboBox ─────────────
    public ObservableCollection<ProductUnitOption> AvailableUnits { get; } = new();

    private ProductUnitOption? _selectedUnit;
    public ProductUnitOption? SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (SetProperty(ref _selectedUnit, value) && value != null)
                _ = OnUnitChangedAsync(value);
        }
    }

    private async Task OnUnitChangedAsync(ProductUnitOption unit)
    {
        ProductUnitId = unit.UnitId;

        // Load current cost from DB for comparison
        var currentData = await _mediator.Send(
            new GetProductUnitPricingQuery(unit.UnitId));

        _oldCostInDatabase = currentData.PurchaseCost;
        UnitCost = currentData.PurchaseCost;         // Pre-fill with DB cost
        NewSalesPrice = currentData.SalesPrice;      // Pre-fill sales price

        OnPropertyChanged(nameof(CostChangedFromDatabase));
        OnPropertyChanged(nameof(PriceDifferenceIndicator));
    }

    public void SetProduct(BarcodeSearchResult result, List<ProductUnitOption> units)
    {
        AvailableUnits.Clear();
        foreach (var unit in units)
            AvailableUnits.Add(unit);

        _selectedUnit = units.First(u => u.UnitId == result.ProductUnitId);
        ProductUnitId = result.ProductUnitId;
        _oldCostInDatabase = result.PurchaseCost;
        UnitCost = result.PurchaseCost;
        NewSalesPrice = result.SalesPrice;

        OnPropertyChanged(nameof(SelectedUnit));
        OnPropertyChanged(nameof(TotalCost));
        OnPropertyChanged(nameof(CostChangedFromDatabase));
    }
}

public record ProductUnitOption(int UnitId, string UnitName, decimal ConversionFactor);
✅ Phase 4 Checklist
 ProductUnitBuilderViewModel.Validate() shows Arabic error messages
 Onboarding dialog shows automatically for new products
 CostChangedFromDatabase triggers when user edits cost field
 PriceDifferenceIndicator shows direction (↑/↓) and old value
 Unit ComboBox in purchase invoice pre-fills cost from DB
🖼️ Phase 5: WPF XAML
Task 5.1 — Unit Hierarchy Builder XAML
XML

<!-- File: Views/Products/UnitHierarchyBuilderControl.xaml -->
<UserControl x:Class="YourApp.Views.Products.UnitHierarchyBuilderControl"
             FlowDirection="RightToLeft">
    <StackPanel>

        <!-- Help Box (always visible) -->
        <Border Background="#E8F5E9" BorderBrush="#4CAF50"
                BorderThickness="1" CornerRadius="6"
                Padding="12" Margin="0,0,0,8">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBlock TextWrapping="Wrap" FontSize="12" Foreground="#2E7D32">
                    <Run FontWeight="Bold">💡 كيف تبني وحدات المنتج؟  </Run>
                    <LineBreak/>
                    <Run>1. ابدأ بالوحدة الصغرى (مثال: حبة) — معامل التحويل = 1</Run>
                    <LineBreak/>
                    <Run>2. أضف الوحدات الأكبر واكتب كم وحدة صغرى بداخلها</Run>
                </TextBlock>
                <Button Grid.Column="1"
                        Content="تفاصيل أكثر ؟"
                        Command="{Binding ShowHelpCommand}"
                        Background="Transparent"
                        Foreground="#4CAF50"
                        BorderThickness="0"
                        FontSize="11"/>
            </Grid>
        </Border>

        <!-- Validation Summary -->
        <Border Background="#FFEBEE"
                BorderBrush="#F44336"
                BorderThickness="1"
                CornerRadius="4"
                Padding="10"
                Margin="0,0,0,8"
                Visibility="{Binding HasValidationError,
                             Converter={StaticResource BoolToVisibility}}">
            <TextBlock Text="{Binding ValidationSummary}"
                       Foreground="#C62828"
                       TextWrapping="Wrap"/>
        </Border>

        <!-- Units DataGrid -->
        <DataGrid ItemsSource="{Binding Units}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  HeadersVisibility="Column"
                  GridLinesVisibility="Horizontal">
            <DataGrid.Columns>

                <!-- Unit Name -->
                <DataGridTemplateColumn Header="اسم الوحدة" Width="140">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBox Text="{Binding UnitName, UpdateSourceTrigger=PropertyChanged}"
                                     PlaceholderText="{Binding Placeholder_UnitName}"
                                     BorderThickness="0" Padding="4"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>

                <!-- Conversion Factor -->
                <DataGridTemplateColumn Header="يساوي كم وحدة صغرى؟" Width="160">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Grid>
                                <!-- Show "1 (أساسية)" for base unit -->
                                <TextBlock
                                    Text="1  ✅ (وحدة أساسية)"
                                    Foreground="#4CAF50"
                                    VerticalAlignment="Center"
                                    Padding="4"
                                    Visibility="{Binding IsBaseUnit,
                                                 Converter={StaticResource BoolToVisibility}}"/>

                                <!-- Editable for derived units -->
                                <TextBox
                                    Text="{Binding BaseConversionFactor,
                                                   UpdateSourceTrigger=PropertyChanged}"
                                    Visibility="{Binding IsBaseUnit,
                                                 Converter={StaticResource InverseBoolToVisibility}}"
                                    BorderThickness="0"
                                    Padding="4"/>
                            </Grid>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>

                <!-- Sales Price -->
                <DataGridTextColumn Header="سعر البيع"
                    Binding="{Binding SalesPrice, UpdateSourceTrigger=PropertyChanged}"
                    Width="90"/>

                <!-- Purchase Cost -->
                <DataGridTextColumn Header="تكلفة الشراء"
                    Binding="{Binding PurchaseCost, UpdateSourceTrigger=PropertyChanged}"
                    Width="90"/>

                <!-- Supplier Price -->
                <DataGridTextColumn Header="سعر المورد"
                    Binding="{Binding SupplierPrice, UpdateSourceTrigger=PropertyChanged}"
                    Width="90"/>

                <!-- Last Purchase Price (Read Only) -->
                <DataGridTextColumn Header="آخر سعر توريد"
                    Binding="{Binding LastPurchasePrice, StringFormat=N2}"
                    Width="110"
                    IsReadOnly="True">
                    <DataGridTextColumn.ElementStyle>
                        <Style TargetType="TextBlock">
                            <Setter Property="Foreground" Value="#1565C0"/>
                            <Setter Property="FontWeight" Value="Bold"/>
                        </Style>
                    </DataGridTextColumn.ElementStyle>
                </DataGridTextColumn>

                <!-- Barcode Count -->
                <DataGridTextColumn Header="عدد الباركودات"
                    Binding="{Binding BarcodesCount}"
                    Width="110"
                    IsReadOnly="True"/>

                <!-- Delete Button -->
                <DataGridTemplateColumn Header="" Width="40">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Button Content="🗑"
                                    Command="{Binding DataContext.RemoveUnitCommand,
                                              RelativeSource={RelativeSource AncestorType=UserControl}}"
                                    CommandParameter="{Binding}"
                                    Background="Transparent"
                                    BorderThickness="0"
                                    Foreground="#EF5350"
                                    FontSize="14"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- Add Unit Button -->
        <Button Content="+ إضافة وحدة جديدة"
                Command="{Binding AddUnitCommand}"
                HorizontalAlignment="Left"
                Margin="0,8,0,0"
                Padding="12,6"
                Background="#E3F2FD"
                Foreground="#1565C0"
                BorderThickness="1"
                BorderBrush="#90CAF9"/>
    </StackPanel>
</UserControl>
Task 5.2 — Purchase Invoice Item Row (Price Sync Indicator)
XML

<!-- Inside Purchase Invoice DataGrid -->
<!-- Add these two columns to existing DataGrid -->

<!-- Unit Cost with sync indicator -->
<DataGridTemplateColumn Header="تكلفة الوحدة" Width="130">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <StackPanel>
                <TextBox Text="{Binding UnitCost, UpdateSourceTrigger=PropertyChanged}"
                         BorderThickness="0"/>
                <!-- Sync warning — only shows when cost differs from DB -->
                <TextBlock Text="{Binding PriceDifferenceIndicator}"
                           FontSize="10"
                           Foreground="#E65100"
                           TextWrapping="Wrap"
                           Visibility="{Binding CostChangedFromDatabase,
                                        Converter={StaticResource BoolToVisibility}}"/>
            </StackPanel>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>

<!-- New Sales Price (optional override) -->
<DataGridTemplateColumn Header="سعر البيع الجديد" Width="120">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <TextBox Text="{Binding NewSalesPrice, UpdateSourceTrigger=PropertyChanged}"
                     PlaceholderText="اختياري"
                     BorderThickness="0"
                     ToolTip="إذا أدخلت سعراً جديداً، سيتم تحديث سعر بيع الصنف فور حفظ الفاتورة"/>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
Task 5.3 — Settings Screen (Costing Method Selector)
XML

<!-- File: Views/Settings/CostingMethodSettingView.xaml -->
<StackPanel Margin="16" FlowDirection="RightToLeft">

    <TextBlock Text="طريقة احتساب تكلفة المخزون"
               FontSize="16" FontWeight="Bold" Margin="0,0,0,12"/>

    <!-- Option 1: Weighted Average -->
    <Border BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="8"
            Padding="16" Margin="0,4">
        <StackPanel>
            <RadioButton Content="متوسط التكلفة المرجح  (Weighted Average)"
                         IsChecked="{Binding IsWeightedAverageSelected}"
                         FontSize="14" FontWeight="Bold"/>
            <TextBlock Margin="24,6,0,0" TextWrapping="Wrap" Foreground="#555"
                       Text="يجمع بين سعر البضاعة القديمة في المخزن والجديدة ليعطيك تكلفة موحدة ومتوازنة. ✅ الأنسب للتقارير الضريبية الدقيقة وللمحاسبة القياسية."/>
        </StackPanel>
    </Border>

    <!-- Option 2: Last Purchase Price -->
    <Border BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="8"
            Padding="16" Margin="0,4">
        <StackPanel>
            <RadioButton Content="آخر سعر توريد  (Last Purchase Price)"
                         IsChecked="{Binding IsLastPriceSelected}"
                         FontSize="14" FontWeight="Bold"/>
            <TextBlock Margin="24,6,0,0" TextWrapping="Wrap" Foreground="#555"
                       Text="يستبدل تكلفة المنتج بسعر آخر فاتورة شراء مباشرةً. ✅ مناسب للأسواق المتقلبة حيث تريد دائماً أن يعكس السعر الواقع الحالي."/>
        </StackPanel>
    </Border>

    <!-- Option 3: Supplier Price -->
    <Border BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="8"
            Padding="16" Margin="0,4">
        <StackPanel>
            <RadioButton Content="سعر المورد  (Supplier Catalog Price)"
                         IsChecked="{Binding IsSupplierPriceSelected}"
                         FontSize="14" FontWeight="Bold"/>
            <TextBlock Margin="24,6,0,0" TextWrapping="Wrap" Foreground="#555"
                       Text="يعتمد على السعر المدخل في بطاقة الصنف من قائمة المورد ولا يتغير تلقائياً عند الشراء. ✅ مناسب عندما تتفاوض على سعر ثابت مع المورد لفترة طويلة."/>
        </StackPanel>
    </Border>

    <Button Content="💾  حفظ الإعداد"
            Command="{Binding SaveCostingMethodCommand}"
            HorizontalAlignment="Left"
            Margin="0,16,0,0"
            Padding="20,10"
            Background="#1976D2" Foreground="White" BorderThickness="0"/>
</StackPanel>
✅ Phase 5 Checklist
 Help box always visible (not just on first open)
 Validation error box only shows when HasValidationError = true
 Base unit row shows "✅ وحدة أساسية" and factor is read-only
 Sync warning shows correct direction (↑/↓) with old price
 Each costing method has Arabic explanation text
 All interactive elements minimum 36px height (touch-friendly)
🧪 Phase 6: Unit Tests
csharp

// File: Tests/Domain/ProductUnitTests.cs

public class ProductUnitTests
{
    [Fact]
    public void CreateBaseUnit_AlwaysHasFactorOne()
    {
        var unit = ProductUnit.CreateBaseUnit(productId: 1, unitName: "حبة");
        Assert.Equal(1, unit.BaseConversionFactor);
        Assert.True(unit.IsBaseUnit);
    }

    [Fact]
    public void CreateDerivedUnit_WithFactorOne_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            ProductUnit.CreateDerivedUnit(1, "كرتون", baseConversionFactor: 1));

        Assert.Contains("أكبر من وحدة صغرى واحدة", ex.Message);
    }

    [Fact]
    public void ToBaseUnitQuantity_MultipliesCorrectly()
    {
        var box = ProductUnit.CreateDerivedUnit(1, "كرتون", 12);
        var baseQty = box.ToBaseUnitQuantity(3); // 3 boxes × 12 = 36 pieces
        Assert.Equal(36, baseQty);
    }

    [Fact]
    public void CalculateCostFromBaseUnitCost_ScalesCorrectly()
    {
        var box = ProductUnit.CreateDerivedUnit(1, "كرتون", 12);
        var boxCost = box.CalculateCostFromBaseUnitCost(baseUnitCost: 2m);
        Assert.Equal(24m, boxCost); // 2 SAR/piece × 12 pieces = 24 SAR/box
    }
}

// File: Tests/Application/WeightedAverageCostingTests.cs

public class WeightedAverageCostingTests
{
    [Fact]
    public async Task WeightedAverage_CalculatesCorrectly()
    {
        // Old stock: 10 pieces at 100 SAR each
        // New stock: 10 pieces at 150 SAR each
        // Expected: (10×100 + 10×150) / 20 = 125 SAR

        var mockContext = CreateMockContextWithStock(currentPieces: 10, currentCost: 100);
        var service = CreateService(mockContext, CostingMethod.WeightedAverage);

        var result = await service.CalculateNewCostAsync(
            currentCost: 100,
            currentStock: 10,
            newCost: 150,
            newQuantity: 10);

        Assert.Equal(125m, result);
    }

    [Fact]
    public async Task WeightedAverage_ZeroOldStock_ReturnsNewCost()
    {
        var result = await CalculateWeightedAverage(
            currentStock: 0, currentCost: 100,
            newCost: 150, newQuantity: 10);

        Assert.Equal(150m, result); // No old stock to average with
    }
}

// File: Tests/Domain/CashBoxTests.cs

public class CashBoxTests
{
    [Fact]
    public void Withdraw_InsufficientBalance_ThrowsDomainException()
    {
        var box = CashBox.Create("صندوق الكاشير");
        box.Deposit(100, CashTransactionType.ManualIn, createdBy: 1);

        var ex = Assert.Throws<DomainException>(() =>
            box.Withdraw(200, CashTransactionType.PurchaseOut, createdBy: 1));

        Assert.Contains("رصيد الصندوق غير كافٍ", ex.Message);
    }

    [Fact]
    public void Deposit_UpdatesBalanceAndCreatesTransaction()
    {
        var box = CashBox.Create("صندوق الكاشير");
        box.Deposit(500, CashTransactionType.SaleIn, createdBy: 1);

        Assert.Equal(500, box.CurrentBalance);
        Assert.Single(box.Transactions);
        Assert.Equal(0, box.Transactions.First().BalanceBefore);
        Assert.Equal(500, box.Transactions.First().BalanceAfter);
    }

    [Fact]
    public void CanUserAccess_WrongUser_ThrowsDomainException()
    {
        var box = CashBox.Create("صندوق كاشير 1", assignedUserId: 5);

        Assert.Throws<DomainException>(() => box.CanUserAccess(userId: 99));
    }

    [Fact]
    public void CanUserAccess_SharedBox_AllowsAnyUser()
    {
        var sharedBox = CashBox.Create("الصندوق الرئيسي"); // No assigned user

        // Should NOT throw for any user
        var exception = Record.Exception(() => sharedBox.CanUserAccess(userId: 99));
        Assert.Null(exception);
    }
}
📦 Final Summary
text

┌──────────────────────────────────────────────────────────────────┐
│         DYNAMIC UOM + COSTING + CASH BOXES — IMPLEMENTATION      │
├──────┬──────────────────────────────────────────┬────────────────┤
│ Step │ Deliverable                              │ Key Rule       │
├──────┼──────────────────────────────────────────┼────────────────┤
│  0   │ 8 SQL migrations                         │ Run in order   │
│  1   │ 6 Domain entities + 2 enums              │ No DB in Domain│
│  2   │ EF configs + Barcode + Settings repos    │ Register in DI │
│  3   │ Pricing service + Commands               │ Save then price│
│  4   │ ViewModels (Builder + Invoice)           │ Arabic errors  │
│  5   │ XAML (Builder + Settings + Invoice)      │ 36px min touch │
│  6   │ 7 Unit tests                             │ Never skip     │
└──────┴──────────────────────────────────────────┴────────────────┘

CRITICAL RULES — NEVER VIOLATE:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Stock ALWAYS stored in base units (pieces) — never in boxes/trays
✅ Cost cascade: base unit cost × ConversionFactor = derived unit cost
✅ Pricing update runs AFTER invoice.Id is persisted
✅ WeightedAverage: zero old stock → return new cost (no division by zero)
✅ CashBox.Withdraw() uses domain method — never subtract balance directly
✅ Product.ValidateUnits() called in command handler before ANY save
✅ All user-facing errors in Arabic with actionable guidance
✅ Price history logged for EVERY cost change (audit trail)

# MASTER-PLAN — Sales Management System (v4.6.2 — Validation ErrorTemplate & INotifyDataErrorInfo)

## 📋 Core Philosophy

**One source of truth. AGENTS.md is LAW.** Every rule lives in exactly ONE place. Agents cannot break what they cannot bypass.

- **Clean Architecture (Layered)** — NOT Vertical Slices, NOT Feature Folders
- **Domain is king** — ZERO dependencies, rich entities, business rules enforced at the entity level
- **Desktop → API → SQL Server** — Desktop NEVER connects to the database
- **Result<T> over exceptions** — Services return results, controllers translate to HTTP
- **Bilingual UI** — Arabic labels, English code. All text columns use `nvarchar`
- **AGENTS.md > everything** — If code conflicts with AGENTS.md, the code is wrong

---

## 🏗️ Actual Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        SOLUTION STRUCTURE (11 Projects)                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  SalesSystem.slnx                                                       │
│  ├── 📦 SalesSystem.Domain/          ← Entities + Enums + Exceptions    │
│  │      (net10.0, ZERO NuGet deps)                                      │
│  │                                                                       │
│  ├── 📦 SalesSystem.Contracts/       ← DTOs + Requests + Result<T>      │
│  │      (net10.0, ZERO NuGet deps)                                      │
│  │                                                                       │
│  ├── 📦 SalesSystem.Application/     ← Service interfaces + impls       │
│  │      (net10.0)                                                        │
│  │                                                                       │
│  ├── 📦 SalesSystem.Infrastructure/  ← EF Core + DbContext + Repos      │
│  │      (net10.0-windows)           + Printing + Backup                 │
│  │                                                                       │
│  ├── 📦 SalesSystem.Api/             ← Controllers + FluentValidation   │
│  │      (net10.0-windows)           + JWT + Serilog + Swagger           │
│  │                                                                       │
│  ├── 📦 SalesSystem.DesktopPWF/      ← WPF UI + MVVM + EventBus         │
│  │      (net10.0-windows)           + Navigation + Dialogs              │
│  │                                                                       │
│  └── 🧪 Tests/ (5 projects)          ← Unit + Integration tests         │
│                                                                         │
│  Legacy/ (NOT in solution)                                              │
│  └── 🗑️ SalesSystem.Desktop/         ← Abandoned WinForms (safe delete) │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

Data Flow (NEVER break this chain):

  Desktop (WPF)
      ↓ HttpClient
  SalesSystem.Api (Controllers + FluentValidation + JWT)
      ↓ delegates to
  SalesSystem.Application (Service interfaces + implementations)
      ↓ delegates to
  SalesSystem.Infrastructure (EF Core + DbContext + Repositories)
      ↓ connects to
  SQL Server
      ↑
  SalesSystem.Domain (ZERO dependencies — referenced by ALL layers)
```

### Architecture Pattern: Clean Architecture (Layered)

| Layer | Responsibility | Dependencies |
|-------|---------------|--------------|
| **Domain** | Entities, Enums, Exceptions, Business Rules | NONE |
| **Contracts** | DTOs, Request/Response models, `Result<T>` | Domain |
| **Application** | Service interfaces + implementations, Use Cases | Domain, Contracts |
| **Infrastructure** | EF Core DbContext, Repositories, UoW, Printing, Backup | Application, Contracts, Domain |
| **Api** | Controllers, FluentValidation, JWT Auth, Serilog, Swagger | Application, Infrastructure |
| **DesktopPWF** | WPF Views, ViewModels (MVVM), EventBus, Navigation | Contracts (via HTTP) |

**Key decisions:**
- **Service Layer** pattern (NOT CQRS/MediatR)
- **IUnitOfWork** for multi-table operations
- **Rich Domain Model** — entities have `private set` + factory methods + guard clauses
- **4-layer validation** — Domain → Application → API (FluentValidation) → Database (CHECK constraints)

---

## ✅ Implemented Features (Phases 1-7)

| Phase | Status | Key Deliverables |
|-------|--------|-----------------|
| **Phase 1: Foundation** | ✅ Complete | Domain entities (Product, Customer, Supplier, Invoice, etc.), Enums, DomainException, Guard Clauses, Contracts (DTOs, Requests, Result<T>) |
| **Phase 2: Infrastructure** | ✅ Complete | EF Core DbContext, Repositories, IUnitOfWork, Migrations, Fluent API config, CHECK constraints, Seed data |
| **Phase 3: Application** | ✅ Complete | Service interfaces + implementations for all modules (Products, Customers, Suppliers, Sales, Purchases, Returns, Stock, Reports, Settings, Users, CashBoxes, Inventory) |
| **Phase 4: API** | ✅ Complete | REST Controllers for all modules, FluentValidation validators, JWT authentication, Policy-based authorization, Swagger/OpenAPI, Serilog logging, Error middleware |
| **Phase 5: Desktop Shell** | ✅ Complete | WPF application, Navigation system, MVVM infrastructure, ViewModelBase (292 lines), EventBus, Login screen, Session management, Role-based UI |
| **Phase 6: Desktop Modules** | ✅ Complete | All CRUD screens (Products, Customers, Suppliers, Categories, Units, Warehouses), Sales/Purchase invoices, Returns, Stock transfers, Payments, Reports (Excel export), Barcode input |
| **Phase 7: Production** | ✅ Complete | Auto-Update system, DPAPI encryption, Backup/Restore (raw SQL), Windows Service, Admin screens, Inno Setup installer, Styled dialogs (6 types), Toast notifications, Print engine (A4 + Thermal) |

---

## 🔧 Actual Code Patterns

### ViewModel Pattern

```csharp
// ViewModelBase.cs (292 lines)
// Located: SalesSystem.DesktopPWF/Services/App/ViewModelBase.cs

public abstract class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
{
    // INotifyPropertyChanged
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null);
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null);

    // Commands
    public class RelayCommand : ICommand { ... }
    public class AsyncRelayCommand : ICommand { ... }

    // INotifyDataErrorInfo
    public void AddError(string propertyName, string errorMessage);
    public void ClearErrors(string propertyName);
    public void ClearAllErrors();
    public bool HasErrors { get; }

    // Error handling
    protected void HandleException(Exception ex, string context);
    protected void HandleFailure(string error, string context);

    // State
    public bool IsBusy { get; protected set; }
    public string StatusMessage { get; protected set; }
}
```

**Key features:**
- `INotifyDataErrorInfo` for real-time validation with red border styles
- `RelayCommand` and `AsyncRelayCommand` with `CanExecute`
- `HandleException()` and `HandleFailure()` for centralized error handling
- Save buttons disabled via `CanExecute` when `HasErrors` is true

### Service Pattern

```csharp
// Interface
public interface IProductService
{
    Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<List<ProductDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct);
    Task<Result> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}

// Implementation
public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductService> _logger;

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        // 1. Validate pre-conditions
        // 2. Open transaction
        // 3. Save entity
        // 4. Commit
        // 5. Return Result<T>
    }
}
```

**Key rules:**
- ALL services return `Result<T>` or `Result` — NEVER throw exceptions
- Multi-table operations use `IUnitOfWork.BeginTransactionAsync()`
- Stock validated BEFORE opening transaction
- `InventoryMovement` recorded for EVERY stock change

### Controller Pattern

```csharp
[Authorize(Policy = "ManagerAndAbove")]
[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}
```

**Key rules:**
- Controllers have ZERO business logic — delegate to services
- `[Authorize]` on ALL endpoints (except `/api/auth/login`)
- Policy-based authorization (`AdminOnly`, `ManagerAndAbove`, `AllStaff`)
- Translate `Result<T>` to HTTP status codes

### Domain Pattern

```csharp
public class Product : EntityBase
{
    public string Name { get; private set; }
    public decimal AvgCost { get; private set; }
    public ICollection<ProductUnit> Units { get; private set; }

    // Factory method with guard clauses
    public static Product Create(string name, int categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب");
        if (categoryId <= 0)
            throw new DomainException("الفئة مطلوبة");

        return new Product { Name = name, CategoryId = categoryId };
    }

    // State change via method
    public void UpdatePrice(decimal retailPrice, decimal wholesalePrice)
    {
        if (retailPrice < 0)
            throw new DomainException("سعر التجزئة لا يمكن أن يكون سالباً");
        if (wholesalePrice < 0)
            throw new DomainException("سعر الجملة لا يمكن أن يكون سالباً");
        // ... update logic
    }
}
```

**Key rules:**
- `private set` on ALL critical properties
- State changes via methods ONLY — never direct property modification
- Guard clauses in constructors and factory methods
- `DomainException` with Arabic messages

### Validation (4 Layers)

| Layer | Where | Example |
|-------|-------|---------|
| **Domain** | Entity methods | `if (price < 0) throw DomainException("السعر لا يمكن أن يكون سالباً")` |
| **Application** | Service methods | Stock availability check before transaction |
| **API** | FluentValidation | `RuleFor(x => x.Name).NotEmpty().WithMessage("الاسم مطلوب")` |
| **Database** | CHECK constraints | `CHECK (Quantity >= 0)`, `CHECK (PaidAmount <= TotalAmount)` |

---

## 📦 Project Dependencies

```
SalesSystem.Domain
  └── (ZERO dependencies — pure C#)

SalesSystem.Contracts
  └── SalesSystem.Domain

SalesSystem.Application
  ├── SalesSystem.Domain
  └── SalesSystem.Contracts
  └── Microsoft.Extensions.Logging.Abstractions
  └── MediatR (installed, minimally used)

SalesSystem.Infrastructure
  ├── SalesSystem.Application
  ├── SalesSystem.Contracts
  ├── SalesSystem.Domain
  └── Microsoft.EntityFrameworkCore.SqlServer 10.x
  └── BCrypt.Net-Next 4.x
  └── QuestPDF 2024.3.x
  └── SixLabors.ImageSharp 3.1.x
  └── System.Drawing.Common 10.x
  └── Microsoft.Extensions.Hosting.WindowsServices 10.x
  └── Microsoft.AspNetCore.DataProtection 10.x

SalesSystem.Api
  ├── SalesSystem.Application
  ├── SalesSystem.Contracts
  ├── SalesSystem.Infrastructure
  ├── SalesSystem.Domain
  └── FluentValidation.AspNetCore 11.x
  └── Serilog.AspNetCore 8.x
  └── Microsoft.AspNetCore.Authentication.JwtBearer 10.x
  └── Swashbuckle.AspNetCore 6.x
  └── Serilog.Sinks.EventLog 8.x

SalesSystem.DesktopPWF
  ├── SalesSystem.Contracts
  ├── SalesSystem.Domain
  └── Microsoft.Extensions.Http 10.x
  └── System.Text.Json 10.x
  └── ClosedXML 0.102.x
```

---

## 🎨 Design System (Actual)

**Location:** `SalesSystem.DesktopPWF/Resources/Styles.xaml` (782 lines)

**NOT** `DesignTokens.cs` — that file was NEVER created. All styles are centralized in a single XAML ResourceDictionary.

### What's in Styles.xaml:

- **Color Brushes** — Primary, Success, Warning, Error, Info, Neutral palette
- **Typography** — TextBlock styles for Display, Header, SubHeader, Body, Caption
- **Button Styles** — Primary, Secondary, Danger, Success, Ghost, Icon
- **Card Styles** — Card (with shadow), CardFlat (no shadow)
- **Input Styles** — TextBox, ComboBox, PasswordBox
- **DataGrid Styles** — Standard grid with alternating rows, styled headers
- **Status Badges** — Success, Warning, Error badges
- **Validation Styles** — Red border for validation errors
- **Dialog Styles** — Styled dialogs (Error, Success, Warning, Info, Confirmation, DeleteConfirmation)
- **Navigation Styles** — Sidebar, menu items, active state
- **Toast Styles** — Notification toasts with auto-dismiss

### Usage Pattern:

```xml
<!-- In any XAML view -->
<Button Style="{StaticResource ButtonPrimary}" Content="حفظ"/>
<TextBlock Style="{StaticResource TextHeader}" Text="المنتجات"/>
<TextBox Style="{StaticResource TextBoxStandard}" Text="{Binding Name}"/>
<DataGrid Style="{StaticResource DataGridStandard}" .../>
<Border Style="{StaticResource BadgeSuccess}" .../>
```

**Rule:** NEVER hardcode colors or sizes in XAML views — always use `{StaticResource ...}`.

---

## 📡 Barcode Service (Actual)

**Interface:** `IBarcodeInputService` (NOT `IBarcodeScanner`)
**Implementation:** `BarcodeInputService`
**Location:** `SalesSystem.DesktopPWF/Services/App/Barcode/`

### How it works:

USB barcode scanners act as keyboard emulators — they type the barcode characters then send Enter. The service intercepts at the application level using a keyboard buffer with timing detection.

```csharp
public interface IBarcodeInputService
{
    event Action<string> BarcodeScanned;
    void StartListening();
    void StopListening();
}
```

### Key characteristics:
- **Keyboard buffer** — accumulates characters typed by scanner
- **100ms timeout** — distinguishes scanner (fast) from human typing (slow)
- **Application-level** — works across all screens, no per-screen setup
- **USB/HID only** — NO camera-based scanning (MAUI was never built)
- **Event-driven** — fires `BarcodeScanned` event with barcode string

### Usage in ViewModel:

```csharp
public class SalesInvoiceCreateViewModel : ViewModelBase
{
    public SalesInvoiceCreateViewModel(IBarcodeInputService barcodeService)
    {
        barcodeService.BarcodeScanned += OnBarcodeScanned;
    }

    private void OnBarcodeScanned(string barcode)
    {
        // Lookup product by barcode and add to invoice
        _ = AddProductByBarcodeAsync(barcode);
    }
}
```

---

## 🔐 Security (Actual)

### DPAPI Connection String Encryption

- Connection strings encrypted via `IDataProtector` with `"DPAPI:"` prefix
- `FirstRunSetupService` encrypts plaintext connection string on first run (idempotent)
- `SecureDbContextFactory` decrypts before creating DbContext
- DataProtection keys stored in `%ProgramData%\SalesSystem\DataProtectionKeys`
- `appsettings.json` writes use atomic pattern: `.tmp` → `File.Replace()` → `.bak`

### JWT Authentication

- JWT secret from environment variable — throws `InvalidOperationException` in production if missing
- `BCrypt` passwords with work factor = 12
- Policy-based authorization: `AdminOnly`, `ManagerAndAbove`, `AllStaff`
- ALL endpoints require `[Authorize]` (except `/api/auth/login`)

### Security Audit

- `SecurityAudit.cs` runs in DEBUG only — checks for unencrypted strings, hardcoded passwords
- NEVER log: passwords, connection strings
- Serilog for all logging — NEVER `Console.WriteLine`

---

## 🖨️ Print Engine (Actual)

**NOT WPF FixedDocument/PrintDialog** — uses QuestPDF + Win32 raw printing.

### A4 Invoices (QuestPDF)

- **Library:** QuestPDF Community (free for < $1M revenue)
- **Document:** `A4InvoiceDocument.cs` — RTL layout, logo, tax breakdown
- **Output:** PDF files
- **Preview:** WPF `PdfPreviewWindow` (WebBrowser control)

### Thermal Receipts (Win32 Raw Printing)

- **API:** Win32 `OpenPrinter` / `WritePrinter` via `DllImport`
- **Builder:** Custom `EscPos` static class — NOT external NuGet packages
- **Format:** 42-char monospaced columns, Windows-1256 encoding for Arabic
- **Output:** Direct to thermal printer (80mm)

### Architecture:

```
Desktop → IPrintApiService (HTTP) → PrintController (API) → IPrintService → Printer
```

**Desktop NEVER prints directly** — always goes through the API.

### API Endpoints:

```
GET    /api/v1/print/sales/{id}/preview
POST   /api/v1/print/sales/{id}/a4
POST   /api/v1/print/sales/{id}/thermal
POST   /api/v1/print/sales/{id}/save
GET    /api/v1/print/purchases/{id}/preview
POST   /api/v1/print/purchases/{id}/a4
POST   /api/v1/print/purchases/{id}/thermal
POST   /api/v1/print/purchases/{id}/save
POST   /api/v1/print/test
```

### PrintResult Pattern:

```csharp
public class PrintResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public string? FilePath { get; }
}
```

**NEVER throw from printing code** — always return `PrintResult`.

### Project Structure:

```
SalesSystem.Application/Printing/
├── Contracts/
│   ├── InvoicePrintDto.cs
│   ├── InvoiceItemPrintDto.cs
│   ├── InvoiceTypePrint.cs
│   └── PrintResult.cs
├── InvoicePrintDtoBuilder.cs
└── IPrintService.cs

SalesSystem.Infrastructure/Printing/
├── A4InvoiceDocument.cs
├── ThermalReceiptGenerator.cs
├── EscPos.cs
├── PrintService.cs
├── PrinterException.cs
└── PrintingBootstrapper.cs
```

---

## 🔄 Auto-Update (Actual)

**Location:** `SalesSystem.DesktopPWF/Services/Update/`

### Key rules:
- **NEVER blocks startup** — fire-and-forget with silent failure
- **8-second timeout** — user never waits for update check
- **SHA256 checksum** verification before launching installer
- **Skipped version** persisted to `%AppData%\SalesSystem\settings.json`
- **Desktop calls API** for updates — NEVER implements its own HTTP download
- **`Environment.Exit(0)` is FORBIDDEN** — return `Result<bool>` instead

### IUpdaterService Interface:

```csharp
public interface IUpdaterService
{
    Task<Result<UpdateCheckResult>> CheckForUpdatesAsync(CancellationToken ct = default);
    Task<Result<string>> DownloadUpdateAsync(string downloadUrl, string expectedChecksum,
        IProgress<DownloadProgress> progress, CancellationToken ct = default);
    Task<Result<bool>> LaunchInstallerAndExitAsync(string installerPath);
    Result<string> GetCurrentVersion();
    Result SkipVersion(string version);
    Result<string> GetSkippedVersion();
}
```

### Version Comparison:

Uses `System.Version` — NEVER string comparison.

---

## 💾 Backup System (Actual)

**Location:** `SalesSystem.Infrastructure/Backup/`

### Key rules:
- **Raw SQL `BACKUP DATABASE`** — NEVER SMO dependency
- **Restore uses `SINGLE_USER WITH ROLLBACK AFTER 30`** — gives active transactions 30s
- **Scheduled backup** runs daily at 2:00 AM as `BackgroundService`
- **Retention** = configurable days (default 30) — old backups auto-deleted
- **Restore failure** triggers `TrySetMultiUserAsync` recovery
- **Config parsing** uses `int.TryParse` — NEVER `int.Parse`

### ScheduledBackupWorker:

```csharp
public class ScheduledBackupWorker : BackgroundService
{
    // Uses IServiceScopeFactory for scoped service resolution
    // Respects CancellationToken for graceful shutdown
    // Runs backup at 2:00 AM daily → then cleanup old backups
}
```

---

## 🖥️ Windows Service (Actual)

**Location:** `SalesSystem.Api/Program.cs`

### Configuration:
- **Service name:** `SalesSystemService` (Arabic display name)
- **Auto-recovery:** 3 restarts on failure (1min, 5min, 15min delays)
- **Serilog EventLog sink** for Windows Service logging
- **SQL retry on startup:** 3 attempts × 5 second delay
- **Database migration** runs on service startup (auto-migrate)

### Program.cs Integration:

```csharp
builder.Host.UseWindowsService(options => options.ServiceName = "SalesSystemService");
// + Serilog EventLog sink + SQL retry + FirstRunSetupService
```

---

## 📊 Test Coverage

| Test Project | Target | Status |
|-------------|--------|--------|
| **SalesSystem.Domain.Tests** | Domain entities, guard clauses, business rules | ✅ Active |
| **SalesSystem.Application.Tests** | Service logic, Result<T> patterns | ✅ Active |
| **SalesSystem.Infrastructure.Tests** | EF Core mappings, repositories, migrations | ✅ Active |
| **SalesSystem.Api.Tests** | Controller endpoints, validation, auth | ✅ Active |
| **SalesSystem.Integration.Tests** | End-to-end flows, API + DB integration | ✅ Active |

---

## ⚠️ Partially Implemented

### MediatR

- **Package:** MediatR v12.4.1 installed in `SalesSystem.Application`
- **Usage:** Only 1 file uses it (`ProductPriceQuery`)
- **No Commands/Queries directories** exist
- **No MediatR pipeline behaviors** registered
- **Status:** Installed but NOT adopted

### CQRS

- **Mentioned in AGENTS.md** RULE-043: "Strictly separate Read operations (Queries) from Write operations (Commands)"
- **NOT implemented** — the codebase uses Service Layer pattern
- **Services handle both reads and writes** in the same class
- **Status:** Documented but not built

### Why the gap?

The project started with Service Layer pattern and it proved sufficient for the use cases. MediatR was installed as an experiment but never adopted project-wide. AGENTS.md RULE-043 reflects an aspirational goal, not current reality.

---

## 📋 Future Plans (NOT Implemented)

These are documented in AGENTS.md or discussed but **have zero code in the codebase**:

| Feature | Status | Notes |
|---------|--------|-------|
| **MAUI Mobile App** | ❌ Not started | `Presentation.MAUI` directory never created |
| **SharedKernel project** | ❌ Not started | Architecture uses layered, not shared kernel |
| **DesignTokens.cs** | ❌ Not created | Styles live in `Resources/Styles.xaml` |
| **Roslyn Analyzer** | ❌ Not created | No `HardcodedColorAnalyzer` or similar |
| **ExecuteAsync() wrapper** | ❌ Not in ViewModelBase | Error handling uses `HandleException()` / `HandleFailure()` |
| **Vertical Slices** | ❌ Not adopted | Layered architecture is the standard |
| **Camera-based barcode** | ❌ Not started | Only USB/HID keyboard scanner implemented |
| **BarcodeScanViewModel** | ❌ Not created | Barcode handled via `IBarcodeInputService` event |
| **BaseViewModel in SharedKernel** | ❌ Not created | ViewModelBase lives in DesktopPWF |

---

## 🗑️ Legacy Code

### `Legacy/SalesSystem.Desktop/`

- **What it is:** Abandoned WinForms desktop application
- **Status:** NOT in solution file, NOT compiled, NOT referenced
- **Safe to delete:** Yes — all functionality has been rebuilt in `DesktopPWF` (WPF)
- **Why abandoned:** WinForms couldn't support the modern MVVM + EventBus + styled dialog architecture
- **Recommendation:** Delete when convenient — it's dead weight

---

## 📐 Architecture Decisions

### Why Service Layer over CQRS/MediatR?

- The application has ~20 aggregate roots, not 200+ — Service Layer is simpler and sufficient
- CQRS adds ceremony (Command/Query classes, handlers, validators) without proportional benefit at this scale
- Service Layer is easier for junior developers to understand and maintain
- Can migrate to CQRS later if complexity demands it

### Why DesktopPWF (WPF) over WinForms?

- WPF supports MVVM pattern with data binding
- XAML enables centralized styling (`Styles.xaml`)
- Better support for modern UI (animations, templates, resources)
- EventBus integration works naturally with WPF's dispatcher
- WinForms required code-behind logic — violated separation of concerns

### Why Layered over Vertical Slices?

- Small team (2-3 developers) — layered is easier to navigate
- Clear separation of concerns: Domain → Application → Infrastructure → API → Desktop
- Each layer has a single responsibility and single dependency direction
- Vertical slices work better for large teams with many independent features

### Why NOT MAUI?

- Target users are desktop-only (retail shops with POS terminals)
- Mobile would require entirely different UX (touch-optimized, offline-first)
- API already provides mobile-ready endpoints — MAUI can be added later
- Focus on perfecting desktop first

### Why Result<T> over Exceptions?

- Exceptions are for exceptional conditions — validation failures are expected
- Result<T> makes error handling explicit and type-safe
- Controllers can cleanly map Result to HTTP status codes
- Avoids try/catch boilerplate in every service method

### Why Rich Domain Model?

- Entities own their business rules — can't be bypassed from outside
- `private set` prevents accidental state corruption
- Factory methods enforce invariants at creation time
- Guard clauses catch invalid states early with clear Arabic messages

---

## 🔗 Cross-Reference Guide

| Topic | File |
|-------|------|
| **All rules (LAW)** | `AGENTS.md` |
| **Financial formulas** | `docs/CONSTITUTION.md` |
| **Full requirements** | `docs/PRD-MVP.md` |
| **Database schema** | `docs/database-schema.md` |
| **UI/UX flows** | `docs/ui-screens.md` |
| **Security details** | `.opencode/agent/security-auditor.md` |
| **Print specs** | `specs/006-printing/plan.md` |
| **Code patterns** | `.opencode/agent/implement-agent.md` |
| **Implementation plan** | `docs/MASTER-PLAN.md` (this file) |
| **Costing & UOM specs** | `docs/CONSTITUTION.md` sections 2.24–2.27 |

---

## ✅ Phase 18: WPF Validation ErrorTemplate & INotifyDataErrorInfo (v4.6.2)

**Goal**: Standardize validation UI with red border + ❗ icon ErrorTemplate, replace `HasXxxError` boolean pattern with `INotifyDataErrorInfo`, and add `ValidateAllAsync()` to ViewModelBase.

### Key Changes
- **New ErrorTemplate**: Red border (#EF4444, 1.5px) + ❗ icon badge with ToolTip — applies to TextBox, PasswordBox, ComboBox when `Validation.HasError = true`
- **ViewModelBase.cs**: Added `SetDialogService(IDialogService)`, `ValidateAllAsync()`, `ValidateField()`
- **14 Editor VMs**: All call `SetDialogService()` in constructors
- **ProductEditorViewModel**: Removed 7 `HasXxxError` booleans — real-time `AddError`/`ClearErrors`
- **CustomerEditorViewModel**: Removed 3 `HasXxxError` booleans — real-time `AddError`/`ClearErrors`

### New Rules (AGENTS.md)
| Rule | Description |
|------|-------------|
| RULE-227 | `SetDialogService()` in every Editor VM constructor |
| RULE-228 | Use `INotifyDataErrorInfo` (`AddError/ClearErrors`) — no `HasXxxError` booleans |
| RULE-229 | Pre-save validation: `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()` |
| RULE-230 | ErrorTemplate: red border + ❗ icon with ToolTip bound to `[0].ErrorContent` |

### File Impact
| Layer | Files |
|-------|-------|
| UI (XAML) | `Resources/Styles.xaml` — new ErrorTemplate + HasError triggers |
| ViewModels | `ViewModelBase.cs` — SetDialogService, ValidateAllAsync, ValidateField |
| Editor VMs | 14 files — SetDialogService() in constructors |
| Refactored VMs | ProductEditor, CustomerEditor — removed HasXxxError pattern |
| Documentation | AGENTS.md, README.md, 5 subagent files, CHANGELOG.md, MASTER-PLAN.md, CONSTITUTION.md |

---

## ✅ Phase 19: Architecture Alignment & Code Quality Remediation (v4.6.3)

**Goal**: Align Costing settings with Clean Architecture boundaries (moving to `ISettingsApiService` via HTTP Client), resolve ViewModel compiler shadowing (CS0108 warnings), wrap async void operations in ViewModels with safe try-catches, and correct garbled Arabic text.

### Key Changes
- **Costing Settings Refactor**: Migrated `CostingMethodSettingsViewModel` from repository calling to HTTP setting API client calls. Registered the VM inside `App.xaml.cs`.
- **WPF VM Quality Standard**: Avoided CS0108 by calling base class property setting helpers and calling `SetDialogService()`. Safe try-catch wrappers for async void initialization workflows.
- **RTL Arabic Corrections**: Rectified Mojibake in transfers/payments ViewModels.

---

## 📝 Version History

| Version | Date | Description |
|---------|------|-------------|
| v4.6.3 | 2026-05-23 | Architecture Alignment & Code Quality — Costing settings HTTP refactoring, VM DI registration, CS0108 member hiding resolutions, async void try-catch safety, RTL Arabic corrections |
| v4.6.2 | 2026-05-23 | WPF Validation ErrorTemplate — Red border + ❗ icon ErrorTemplate, INotifyDataErrorInfo standardization, ValidateAllAsync() base method, 14 Editor VMs updated |
| v4.6.1 | 2026-05-23 | UI Sorting & Dialog Safety — Newest-first sorting, DatabaseErrorDialog self-owner fix, comprehensive audit |
| v4.6 | 2026-05-22 | Audit & Polish — LogSystemError centralized, Dialog overlay, ValidationErrorsDialog, auto-focus, hard-delete safety, login/settings fixes |
| v4.5.3 | 2026-05-22 | Identifier Strategy Complete — Code removal (Product, Customer, Supplier, Warehouse) — all entities use auto-increment Id |
| v4.5.2 | 2026-05-22 | Identifier Strategy — Code removal (Product/Customer/Supplier) |
| v4.5.1 | 2026-05-22 | Error & Shutdown improvements — Error message overhaul, Arabic-friendly errors, MessageBox elimination |
| v4.5 | 2026-05-21 | Multi-Window Screen Management — Non-modal editors, ScreenWindowService |
| v4.4 | 2026-05-21 | Production release — Auto-Update, DPAPI, Backup, Windows Service, Installer |
| v4.3 | 2026-05-15 | Print engine (QuestPDF + Win32), Dynamic UOM, Costing strategy, Cash boxes, Price history |
| v4.2 | 2026-05-10 | Delete strategy, Defensive programming, WPF dialogs, Toast notifications, Real-time validation |
| v4.1 | 2026-05-05 | Wholesale/Retail pricing, Unit conversion in Domain |
| v4.0 | 2026-05-01 | Clean Architecture rewrite — 6 projects, Service Layer, Result<T> |
| v3.0 | 2026-04-15 | Initial architecture — PRD-MVP |
