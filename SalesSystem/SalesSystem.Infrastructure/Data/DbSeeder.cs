using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data;

/// <summary>
/// Seeds initial reference data required for the system to function:
/// admin user, default warehouse, default customer, default supplier,
/// default cash box, base units, and document sequences.
/// Idempotent — skips if any users already exist.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Seeds initial data into the database. Safe to call multiple times —
    /// checks for existing users first and skips if already seeded.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="logger">Optional logger for recording seed progress.</param>
    public static async Task SeedAsync(SalesDbContext db, ILogger? logger = null)
    {
        // Skip if already seeded — presence of any user indicates data exists
        if (await db.Users.AnyAsync())
        {
            logger?.LogInformation("Database already seeded. Skipping...");
            return;
        }

        // 1. Admin user — password: Admin@123, BCrypt work factor 12
        var adminUser = User.Create(
            userName: "admin",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
            fullName: "Administrator",
            role: UserRole.Admin,
            createdByUserId: null
        );
        db.Users.Add(adminUser);

        // 2. Default warehouse
        var warehouse = Warehouse.Create(
            name: "المخزن الرئيسي",
            location: null,
            isDefault: true,
            createdByUserId: null
        );
        db.Warehouses.Add(warehouse);

        // 3. Default customer — used for cash sales where no named customer is needed
        var defaultCustomer = Customer.Create(
            name: "العميل الافتراضي في النظام",
            openingBalance: 0m,
            createdByUserId: null
        );
        db.Customers.Add(defaultCustomer);

        // 4. Default supplier — for cash purchases where no named supplier is needed
        var defaultSupplier = Supplier.Create(
            name: "المورد الافتراضي في النظام",
            openingBalance: 0m,
            createdByUserId: null
        );
        db.Suppliers.Add(defaultSupplier);

        // 5. Default cash box — main cash drawer for daily operations
        var defaultCashBox = CashBox.Create(
            boxName: "الصندوق الرئيسي",
            initialBalance: 0m
        );
        db.CashBoxes.Add(defaultCashBox);

        // 6. Base units of measure
        db.Units.Add(Unit.Create("قطعة", "pcs", null));
        db.Units.Add(Unit.Create("كيلو", "kg", null));
        db.Units.Add(Unit.Create("لتر", "ltr", null));
        db.Units.Add(Unit.Create("متر", "m", null));
        db.Units.Add(Unit.Create("صندوق", "box", null));

        // 7. Document sequences for all document types (year 2026)
        db.DocumentSequences.Add(DocumentSequence.Create("INV", "INV", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("PUR", "PUR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("SR", "SR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("PR", "PR", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("TRF", "TRF", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("CP", "CP", 2026));
        db.DocumentSequences.Add(DocumentSequence.Create("SP", "SP", 2026));

        await db.SaveChangesAsync();
        logger?.LogInformation("Seed data completed successfully.");
    }
}
