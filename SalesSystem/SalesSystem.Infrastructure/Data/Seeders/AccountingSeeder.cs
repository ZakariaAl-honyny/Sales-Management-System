using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds the chart of accounts (system accounts) and global SystemAccountMappings.
/// Idempotent — skips if the Accounts table already has records.
/// </summary>
public static class AccountingSeeder
{
    public static async Task SeedAsync(SalesDbContext db, ILogger? logger = null)
    {
        if (await db.Set<Account>().AnyAsync())
        {
            logger?.LogInformation("Accounts already seeded. Skipping...");
            return;
        }

        var accounts = new List<Account>();

        // 1. Assets (AccountType.Asset = 1)
        accounts.Add(Account.Create("1000", "الأصول", "Assets", AccountType.Asset, null, true, "أصول متداولة"));
        accounts.Add(Account.Create("1100", "النقدية في الصندوق", "Cash on Hand", AccountType.Asset, null, true, "النقدية المتوفرة في الصندوق"));
        accounts.Add(Account.Create("1200", "الحسابات البنكية", "Bank Accounts", AccountType.Asset, null, true, "الأرصدة في البنوك"));
        accounts.Add(Account.Create("1300", "حسابات العملاء", "Accounts Receivable", AccountType.Asset, null, true, "المبالغ المستحقة من العملاء"));
        accounts.Add(Account.Create("1400", "المخزون", "Inventory", AccountType.Asset, null, true, "قيمة البضاعة في المخزن"));

        // 2. Liabilities (AccountType.Liability = 2)
        accounts.Add(Account.Create("2000", "الخصوم", "Liabilities", AccountType.Liability, null, true, "الالتزامات المالية"));
        accounts.Add(Account.Create("2100", "حسابات الموردين", "Accounts Payable", AccountType.Liability, null, true, "المبالغ المستحقة للموردين"));
        accounts.Add(Account.Create("2200", "ضريبة القيمة المضافة الخارجة", "VAT Output", AccountType.Liability, null, true, "ضريبة المبيعات المستحقة للحكومة"));
        accounts.Add(Account.Create("2300", "ضريبة القيمة المضافة الداخلة", "VAT Input", AccountType.Liability, null, true, "ضريبة المشتريات المستردة"));

        // 3. Equity (AccountType.Equity = 3)
        accounts.Add(Account.Create("3000", "حقوق الملكية", "Equity", AccountType.Equity, null, true, "رأس المال وحقوق الملكية"));
        accounts.Add(Account.Create("3100", "رأس المال", "Capital", AccountType.Equity, null, true, "رأس مال المنشأة"));

        // 4. Revenue (AccountType.Revenue = 4)
        accounts.Add(Account.Create("4000", "الإيرادات", "Revenue", AccountType.Revenue, null, true, "إيرادات المبيعات"));
        accounts.Add(Account.Create("4100", "إيرادات المبيعات", "Sales Revenue", AccountType.Revenue, null, true, "إيرادات بيع المنتجات"));
        accounts.Add(Account.Create("4200", "مرتجعات المبيعات", "Sales Returns", AccountType.Revenue, null, true, "مرتجع البضاعة المباعة"));

        // 5. Expenses (AccountType.Expense = 5)
        accounts.Add(Account.Create("5000", "المصروفات", "Expenses", AccountType.Expense, null, true, "المصروفات التشغيلية"));
        accounts.Add(Account.Create("5100", "تكلفة البضاعة المباعة", "Cost of Goods Sold", AccountType.Expense, null, true, "تكلفة المشتريات"));
        accounts.Add(Account.Create("5200", "المصروفات العمومية", "General Expenses", AccountType.Expense, null, true, "المصروفات الإدارية والتشغيلية"));
        accounts.Add(Account.Create("5300", "هالك المخزون", "Spoilage Loss", AccountType.Expense, null, true, "خسائر التلف والهلاك"));

        await db.Set<Account>().AddRangeAsync(accounts);
        await db.SaveChangesAsync();

        // Get IDs for system account mappings
        var cashAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "1100");
        var bankAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "1200");
        var arAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "1300");
        var inventoryAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "1400");
        var apAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "2100");
        var vatOutputAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "2200");
        var vatInputAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "2300");
        var capitalAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "3100");
        var salesRevenueAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "4100");
        var salesReturnAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "4200");
        var cogsAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "5100");
        var generalExpenseAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "5200");
        var spoilageAccount = await db.Set<Account>().FirstAsync(a => a.AccountCode == "5300");

        // Seed global SystemAccountMappings
        var mappings = SystemAccountMappings.Create(
            defaultCashAccountId: cashAccount.Id,
            defaultBankAccountId: bankAccount.Id,
            inventoryAssetAccountId: inventoryAccount.Id,
            accountsReceivableAccountId: arAccount.Id,
            accountsPayableAccountId: apAccount.Id,
            vatOutputAccountId: vatOutputAccount.Id,
            vatInputAccountId: vatInputAccount.Id,
            capitalAccountId: capitalAccount.Id,
            salesRevenueAccountId: salesRevenueAccount.Id,
            salesReturnAccountId: salesReturnAccount.Id,
            cogsAccountId: cogsAccount.Id,
            generalExpenseAccountId: generalExpenseAccount.Id,
            spoilageLossAccountId: spoilageAccount.Id
        );
        db.Set<SystemAccountMappings>().Add(mappings);

        await db.SaveChangesAsync();
        logger?.LogInformation("Accounting foundation seeded successfully: {Count} accounts created.", accounts.Count);
    }
}
