-- File: Scripts/SeedPerformanceData.sql
-- Seeds 500 customers, 200 products, 1 year of enterprise-volume transactions
-- WARNING: Takes 5-15 minutes to run. Use on test environment only.
-- Adapted for SalesSystem schema (v4.3)
-- Requires: Existing admin user (Id=1), warehouse (Id=1), cash box (Id=1)
--           Categories (Id=1..10), Units (Id=1..5)

SET NOCOUNT ON;

DECLARE @StartDate DATE = DATEADD(YEAR, -1, GETDATE());
DECLARE @EndDate DATE = GETDATE();
DECLARE @CurrentDate DATE = @StartDate;
DECLARE @InvoiceId INT;
DECLARE @CustomerId INT;
DECLARE @SupplierId INT;
DECLARE @ProductId INT;
DECLARE @DailyInvoices INT;
DECLARE @AdminUserId INT = 1;
DECLARE @WarehouseId INT = 1;
DECLARE @CashBoxId INT = 1;

-- ─── Ensure seed prerequisites exist ───────────────
IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = 1)
BEGIN
    RAISERROR('Admin user (Id=1) not found. Run application seed first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM Warehouses WHERE Id = 1)
BEGIN
    RAISERROR('Warehouse (Id=1) not found. Run application seed first.', 16, 1);
    RETURN;
END

-- Ensure 10 categories exist
IF (SELECT COUNT(*) FROM Categories) < 10
BEGIN
    PRINT 'Seeding categories...';
    DECLARE @ci INT = 1;
    WHILE @ci <= 10
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Categories WHERE Id = @ci)
            INSERT INTO Categories (Name, Description, CreatedAt, CreatedByUserId)
            VALUES (N'تصنيف ' + CAST(@ci AS NVARCHAR), N'وصف التصنيف ' + CAST(@ci AS NVARCHAR), GETUTCDATE(), @AdminUserId);
        SET @ci = @ci + 1;
    END;
END;

-- Ensure 5 units exist
IF (SELECT COUNT(*) FROM Units) < 5
BEGIN
    PRINT 'Seeding units...';
    IF NOT EXISTS (SELECT 1 FROM Units WHERE Id = 1) INSERT INTO Units (Name, Symbol, CreatedAt, CreatedByUserId) VALUES (N'قطعة', 'pcs', GETUTCDATE(), @AdminUserId);
    IF NOT EXISTS (SELECT 1 FROM Units WHERE Id = 2) INSERT INTO Units (Name, Symbol, CreatedAt, CreatedByUserId) VALUES (N'كيلو', 'kg', GETUTCDATE(), @AdminUserId);
    IF NOT EXISTS (SELECT 1 FROM Units WHERE Id = 3) INSERT INTO Units (Name, Symbol, CreatedAt, CreatedByUserId) VALUES (N'لتر', 'ltr', GETUTCDATE(), @AdminUserId);
    IF NOT EXISTS (SELECT 1 FROM Units WHERE Id = 4) INSERT INTO Units (Name, Symbol, CreatedAt, CreatedByUserId) VALUES (N'متر', 'm', GETUTCDATE(), @AdminUserId);
    IF NOT EXISTS (SELECT 1 FROM Units WHERE Id = 5) INSERT INTO Units (Name, Symbol, CreatedAt, CreatedByUserId) VALUES (N'صندوق', 'box', GETUTCDATE(), @AdminUserId);
END;

-- Ensure cash box exists
IF NOT EXISTS (SELECT 1 FROM CashBoxes WHERE Id = 1)
    INSERT INTO CashBoxes (BoxName, CurrentBalance, CurrencyCode, CreatedAt, CreatedByUserId)
    VALUES (N'الصندوق الرئيسي', 0, 'SAR', GETUTCDATE(), @AdminUserId);

-- ─── Seed Customers (500 customers) ───────────────
PRINT 'Seeding customers...';
DECLARE @i INT = 1;
WHILE @i <= 500
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Customers WHERE Code = 'CUST-' + RIGHT('000000' + CAST(@i AS VARCHAR), 6))
    BEGIN
        INSERT INTO Customers (Code, Name, Phone, Address, OpeningBalance, CurrentBalance, CreatedAt, CreatedByUserId)
        VALUES (
            'CUST-' + RIGHT('000000' + CAST(@i AS VARCHAR), 6),
            N'عميل تجريبي رقم ' + CAST(@i AS NVARCHAR),
            '05' + RIGHT('00000000' + CAST((@i * 7 + 10000000) AS VARCHAR), 8),
            N'الرياض - حي رقم ' + CAST((@i % 20) + 1 AS NVARCHAR),
            0, 0,
            DATEADD(DAY, -(@i % 365), GETUTCDATE()),
            @AdminUserId
        );
    END;
    SET @i = @i + 1;
END;

-- ─── Seed Suppliers (50 suppliers) ───────────────
PRINT 'Seeding suppliers...';
SET @i = 1;
WHILE @i <= 50
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Suppliers WHERE Code = 'SUPP-' + RIGHT('000000' + CAST(@i AS VARCHAR), 6))
    BEGIN
        INSERT INTO Suppliers (Code, Name, Phone, Address, OpeningBalance, CurrentBalance, CreatedAt, CreatedByUserId)
        VALUES (
            'SUPP-' + RIGHT('000000' + CAST(@i AS VARCHAR), 6),
            N'مورد تجريبي رقم ' + CAST(@i AS NVARCHAR),
            '055' + RIGHT('0000000' + CAST((@i * 13 + 5000000) AS VARCHAR), 7),
            N'جدة - منطقة رقم ' + CAST((@i % 15) + 1 AS NVARCHAR),
            0, 0,
            DATEADD(DAY, -(@i % 365), GETUTCDATE()),
            @AdminUserId
        );
    END;
    SET @i = @i + 1;
END;

-- ─── Seed Products (200 products with units) ──────
PRINT 'Seeding products...';
SET @i = 1;
WHILE @i <= 200
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Products WHERE Code = 'PROD-' + RIGHT('000000' + CAST(@i AS VARCHAR), 6))
    BEGIN
        INSERT INTO Products (Code, Name, CategoryId, UnitId, RetailUnitId, PurchasePrice, SalePrice, RetailPrice, WholesalePrice, ConversionFactor, MinStock, CreatedAt, CreatedByUserId)
        VALUES (
            'PROD-' + RIGHT('000000' + CAST(@i AS VARCHAR), 6),
            N'منتج تجريبي ' + CAST(@i AS NVARCHAR),
            (@i % 10) + 1,
            1, 1,
            (@i % 30) + 5,
            (@i % 50) + 10,
            (@i % 50) + 10,
            ((@i % 50) + 10) * 0.85,
            1, 10,
            DATEADD(DAY, -(@i % 365), GETUTCDATE()),
            @AdminUserId
        );

        SET @ProductId = SCOPE_IDENTITY();

        -- Base unit (piece)
        INSERT INTO ProductUnits (ProductId, UnitName, BaseConversionFactor, IsBaseUnit, SalesPrice, PurchaseCost, SortOrder, CreatedAt, CreatedByUserId)
        VALUES (@ProductId, N'حبة', 1, 1, (@i % 50) + 10, (@i % 30) + 5, 0, GETUTCDATE(), @AdminUserId);

        -- Box unit (12 pieces)
        INSERT INTO ProductUnits (ProductId, UnitName, BaseConversionFactor, IsBaseUnit, SalesPrice, PurchaseCost, SortOrder, CreatedAt, CreatedByUserId)
        VALUES (@ProductId, N'كرتون', 12, 0, ((@i % 50) + 10) * 12 * 0.95, ((@i % 30) + 5) * 12, 1, GETUTCDATE(), @AdminUserId);

        -- Initial stock
        INSERT INTO WarehouseStocks (WarehouseId, ProductId, Quantity, ReorderLevel, CreatedAt, CreatedByUserId)
        VALUES (@WarehouseId, @ProductId, (@i * 37) % 1000 + 100, 10, GETUTCDATE(), @AdminUserId);
    END;

    SET @i = @i + 1;
END;

-- ─── Seed 1 Year of Sales Invoices ─────────────────
PRINT 'Seeding sales invoices (this may take several minutes)...';

WHILE @CurrentDate <= @EndDate
BEGIN
    -- 5-20 invoices per day (more on weekends)
    SET @DailyInvoices = CASE DATEPART(WEEKDAY, @CurrentDate)
        WHEN 6 THEN 20  -- Thursday (busy)
        WHEN 7 THEN 25  -- Friday (busiest)
        ELSE 10
    END;

    DECLARE @j INT = 1;
    WHILE @j <= @DailyInvoices
    BEGIN
        SET @CustomerId = (ABS(CHECKSUM(NEWID())) % 500) + 1;

        INSERT INTO SalesInvoices
            (InvoiceNo, CustomerId, WarehouseId, InvoiceDate, PaymentType,
             SubTotal, DiscountAmount, TaxAmount, TotalAmount, PaidAmount, DueAmount,
             Notes, Status, CreatedAt, CreatedByUserId)
        VALUES (
            'INV-' + FORMAT(@CurrentDate, 'yyyyMMdd') + '-' + RIGHT('0000' + CAST(@j AS VARCHAR), 4),
            @CustomerId, @WarehouseId,
            DATEADD(MINUTE, @j * 30, CAST(@CurrentDate AS DATETIME)),
            CASE ABS(CHECKSUM(NEWID())) % 2 WHEN 0 THEN CAST(1 AS TINYINT) ELSE CAST(2 AS TINYINT) END,
            0, 0, 0, 0, 0, 0,
            NULL, 2,   -- Status = Posted (2)
            DATEADD(MINUTE, @j * 30, CAST(@CurrentDate AS DATETIME)),
            @AdminUserId
        );

        SET @InvoiceId = SCOPE_IDENTITY();

        -- Add 1-5 items per invoice
        DECLARE @ItemCount INT = (ABS(CHECKSUM(NEWID())) % 5) + 1;
        DECLARE @k INT = 1;
        DECLARE @InvoiceTotal DECIMAL(18, 2) = 0;

        WHILE @k <= @ItemCount
        BEGIN
            SET @ProductId = (ABS(CHECKSUM(NEWID())) % 200) + 1;
            DECLARE @Qty DECIMAL(18, 3) = (ABS(CHECKSUM(NEWID())) % 10) + 1;
            DECLARE @Price DECIMAL(18, 2) = (ABS(CHECKSUM(NEWID())) % 100) + 10;
            DECLARE @ItemTotal DECIMAL(18, 2) = @Qty * @Price;

            INSERT INTO SalesInvoiceItems
                (SalesInvoiceId, ProductId, Quantity, UnitPrice, DiscountAmount, LineTotal, Mode, Notes, CreatedAt, CreatedByUserId)
            VALUES (
                @InvoiceId, @ProductId, @Qty, @Price, 0, @ItemTotal, 1, NULL,
                DATEADD(MINUTE, @j * 30, CAST(@CurrentDate AS DATETIME)),
                @AdminUserId
            );

            SET @InvoiceTotal = @InvoiceTotal + @ItemTotal;
            SET @k = @k + 1;
        END;

        -- Update invoice totals
        UPDATE SalesInvoices SET
            SubTotal = @InvoiceTotal,
            TaxAmount = @InvoiceTotal * 0.15,
            TotalAmount = @InvoiceTotal * 1.15
        WHERE Id = @InvoiceId;

        SET @j = @j + 1;
    END;

    SET @CurrentDate = DATEADD(DAY, 1, @CurrentDate);
END;

-- ─── Seed 6 Months of Purchase Invoices ─────────────
PRINT 'Seeding purchase invoices...';
SET @CurrentDate = DATEADD(MONTH, -6, GETDATE());
DECLARE @EndPurchaseDate DATE = GETDATE();

WHILE @CurrentDate <= @EndPurchaseDate
BEGIN
    SET @DailyInvoices = 5;  -- Fewer purchase invoices per day

    SET @j = 1;
    WHILE @j <= @DailyInvoices
    BEGIN
        SET @SupplierId = (ABS(CHECKSUM(NEWID())) % 50) + 1;

        INSERT INTO PurchaseInvoices
            (InvoiceNo, SupplierId, WarehouseId, InvoiceDate, PaymentType,
             SubTotal, DiscountAmount, TaxAmount, TotalAmount, PaidAmount, DueAmount,
             Notes, Status, CreatedAt, CreatedByUserId)
        VALUES (
            'PUR-' + FORMAT(@CurrentDate, 'yyyyMMdd') + '-' + RIGHT('0000' + CAST(@j AS VARCHAR), 4),
            @SupplierId, @WarehouseId,
            DATEADD(MINUTE, @j * 45, CAST(@CurrentDate AS DATETIME)),
            CAST(1 AS TINYINT),
            0, 0, 0, 0, 0, 0,
            NULL, 2,
            DATEADD(MINUTE, @j * 45, CAST(@CurrentDate AS DATETIME)),
            @AdminUserId
        );

        SET @InvoiceId = SCOPE_IDENTITY();

        DECLARE @PItemCount INT = (ABS(CHECKSUM(NEWID())) % 8) + 3;
        SET @k = 1;
        DECLARE @PInvoiceTotal DECIMAL(18, 2) = 0;

        WHILE @k <= @PItemCount
        BEGIN
            SET @ProductId = (ABS(CHECKSUM(NEWID())) % 200) + 1;
            DECLARE @PQty DECIMAL(18, 3) = (ABS(CHECKSUM(NEWID())) % 50) + 10;
            DECLARE @PCost DECIMAL(18, 2) = (ABS(CHECKSUM(NEWID())) % 30) + 5;
            DECLARE @PItemTotal DECIMAL(18, 2) = @PQty * @PCost;

            INSERT INTO PurchaseInvoiceItems
                (PurchaseInvoiceId, ProductId, Quantity, UnitCost, DiscountAmount, LineTotal, Notes, CreatedAt, CreatedByUserId)
            VALUES (
                @InvoiceId, @ProductId, @PQty, @PCost, 0, @PItemTotal, NULL,
                DATEADD(MINUTE, @j * 45, CAST(@CurrentDate AS DATETIME)),
                @AdminUserId
            );

            SET @PInvoiceTotal = @PInvoiceTotal + @PItemTotal;
            SET @k = @k + 1;
        END;

        UPDATE PurchaseInvoices SET
            SubTotal = @PInvoiceTotal,
            TaxAmount = @PInvoiceTotal * 0.15,
            TotalAmount = @PInvoiceTotal * 1.15
        WHERE Id = @InvoiceId;

        SET @j = @j + 1;
    END;

    SET @CurrentDate = DATEADD(DAY, 1, @CurrentDate);
END;

-- ─── Verify counts ─────────────────────────────────
PRINT '';
PRINT '=== Seeding Complete ===';
SELECT
    (SELECT COUNT(*) FROM Customers) AS Customers,
    (SELECT COUNT(*) FROM Products) AS Products,
    (SELECT COUNT(*) FROM ProductUnits) AS ProductUnits,
    (SELECT COUNT(*) FROM Suppliers) AS Suppliers,
    (SELECT COUNT(*) FROM WarehouseStocks) AS WarehouseStocks,
    (SELECT COUNT(*) FROM SalesInvoices) AS SalesInvoices,
    (SELECT COUNT(*) FROM SalesInvoiceItems) AS SalesInvoiceItems,
    (SELECT COUNT(*) FROM PurchaseInvoices) AS PurchaseInvoices,
    (SELECT COUNT(*) FROM PurchaseInvoiceItems) AS PurchaseInvoiceItems;
