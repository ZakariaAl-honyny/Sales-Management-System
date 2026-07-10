using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

/// <summary>
/// Bitmask-only permission checking service.
/// PERMISSIONS_MASK rules:
///   - (Mask &amp; Required) == Required → permission granted
///   - Mask == -1 → Super Admin (all permissions bypassed)
///   - Mask == 0 → no permissions
///   - Assigning role to user: User.PermissionsMask = Role.PermissionsMask
/// No RolePermission join table queries are used — all checks are bitwise.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(IUnitOfWork uow, ILogger<PermissionService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    // ═════════════════════════════════════════════════════════════════
    //  Permission code → bit value mapping
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps each permission code string to its bitmask value (a power of two).
    /// Must stay in sync with the codes seeded in DbSeeder.
    /// Up to 60 permissions can be mapped (bits 0-59 fit in a non-negative long);
    /// bit 63 (-1) is reserved for Super Admin.
    /// </summary>
    private static readonly Dictionary<string, long> PermissionCodeToBit = new()
    {
        // ── Sales (bits 0-6) ──────────────────────────────────
        ["Sales.View"]      = 1L << 0,   // 1
        ["Sales.Create"]    = 1L << 1,   // 2
        ["Sales.Edit"]      = 1L << 2,   // 4
        ["Sales.Delete"]    = 1L << 3,   // 8
        ["Sales.Cancel"]    = 1L << 4,   // 16
        ["Sales.Return"]    = 1L << 5,   // 32
        ["Sales.Print"]     = 1L << 6,   // 64

        // ── Purchases (bits 7-12) ────────────────────────────
        ["Purchases.View"]  = 1L << 7,   // 128
        ["Purchases.Create"]= 1L << 8,   // 256
        ["Purchases.Edit"]  = 1L << 9,   // 512
        ["Purchases.Cancel"]= 1L << 10,  // 1024
        ["Purchases.Print"] = 1L << 11,  // 2048
        ["Purchases.Return"]= 1L << 12,  // 4096

        // ── Inventory (bits 13-17) ───────────────────────────
        ["Inventory.View"]      = 1L << 13,  // 8192
        ["Inventory.Transfer"]  = 1L << 14,  // 16384
        ["Inventory.Adjust"]    = 1L << 15,  // 32768
        ["Inventory.Count"]     = 1L << 16,  // 65536
        ["Warehouse.Manage"]    = 1L << 17,  // 131072

        // ── Customers (bits 18-21) ──────────────────────────
        ["Customers.View"]   = 1L << 18,  // 262144
        ["Customers.Create"] = 1L << 19,  // 524288
        ["Customers.Edit"]   = 1L << 20,  // 1048576
        ["Customers.Delete"] = 1L << 21,  // 2097152

        // ── Suppliers (bits 22-25) ──────────────────────────
        ["Suppliers.View"]   = 1L << 22,  // 4194304
        ["Suppliers.Create"] = 1L << 23,  // 8388608
        ["Suppliers.Edit"]   = 1L << 24,  // 16777216
        ["Suppliers.Delete"] = 1L << 25,  // 33554432

        // ── Products (bits 26-29) ───────────────────────────
        ["Products.View"]   = 1L << 26,  // 67108864
        ["Products.Create"] = 1L << 27,  // 134217728
        ["Products.Edit"]   = 1L << 28,  // 268435456
        ["Products.Delete"] = 1L << 29,  // 536870912

        // ── Reports / Accounting (bits 30-32) ───────────────
        ["Reports.View"]      = 1L << 30,  // 1073741824
        ["Accounting.View"]   = 1L << 31,  // 2147483648
        ["Accounting.Manage"] = 1L << 32,  // 4294967296

        // ── System (bits 33-35) ─────────────────────────────
        ["System.Settings"] = 1L << 33,  // 8589934592
        ["System.Users"]    = 1L << 34,  // 17179869184
        ["Roles.Manage"]    = 1L << 35,  // 34359738368

        // ── Operations (bits 36-38) ─────────────────────────
        ["Operations.Cashbox"]   = 1L << 36,  // 68719476736
        ["Operations.Banking"]   = 1L << 37,  // 137438953472
        ["Operations.Expenses"]  = 1L << 38,  // 274877906944

        // ── Audit (bit 39) ──────────────────────────────────
        ["Audit.Log"] = 1L << 39,  // 549755813888

        // ── Backup / Fiscal Year (bits 40-41) ───────────────
        ["Backup.Manage"]   = 1L << 40,  // 1099511627776
        ["FiscalYear.Manage"] = 1L << 41, // 2199023255552

        // ── Currencies (bits 42-43) ─────────────────────────
        ["Currencies.View"]   = 1L << 44,  // 17592186044416
        ["Currencies.Manage"] = 1L << 45,  // 35184372088832
    };

    /// <summary>
    /// Reverse mapping: bit value → permission code.
    /// Built once from <see cref="PermissionCodeToBit"/>.
    /// </summary>
    private static readonly Dictionary<long, string> BitToPermissionCode;

    static PermissionService()
    {
        BitToPermissionCode = PermissionCodeToBit
            .GroupBy(kvp => kvp.Value)
            .ToDictionary(g => g.Key, g => g.First().Key);
    }

    // ═════════════════════════════════════════════════════════════════
    //  Core methods
    // ═════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(int userId, long requiredPermission, CancellationToken ct = default)
    {
        try
        {
            var user = await _uow.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
                return false;

            // Super Admin bypass
            if (user.PermissionsMask == -1)
                return true;

            return (user.PermissionsMask & requiredPermission) == requiredPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check permission mask {Mask} for user {UserId}", requiredPermission, userId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<long> GetPermissionsMaskAsync(int userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _uow.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            return user?.PermissionsMask ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions mask for user {UserId}", userId);
            return 0;
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  Role mask management
    // ═════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<Result> SetRolePermissionsMaskAsync(short roleId, long mask, CancellationToken ct = default)
    {
        try
        {
            var role = await _uow.Roles.FirstOrDefaultAsync(r => r.Id == roleId, ct);
            if (role == null)
                return Result.Failure("الدور غير موجود", ErrorCodes.NotFound);

            role.SetPermissionsMask(mask);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Set permissions mask for role {RoleId}: {Mask}", roleId, mask);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set permissions mask for role {RoleId}", roleId);
            return Result.Failure("حدث خطأ أثناء تعيين صلاحيات الدور.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<Dictionary<short, long>>> GetAllRoleMasksAsync(CancellationToken ct = default)
    {
        try
        {
            var roles = await _uow.Roles.ToListAsync(ct: ct);
            var result = roles.ToDictionary(r => r.Id, r => r.PermissionsMask);
            return Result<Dictionary<short, long>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all role masks");
            return Result<Dictionary<short, long>>.Failure("حدث خطأ أثناء جلب صلاحيات الأدوار.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> AssignRoleToUserAsync(int userId, short roleId, CancellationToken ct = default)
    {
        try
        {
            var user = await _uow.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
                return Result.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

            var role = await _uow.Roles.FirstOrDefaultAsync(r => r.Id == roleId, ct);
            if (role == null)
                return Result.Failure("الدور غير موجود", ErrorCodes.NotFound);

            // Copy role's mask to user
            user.SetPermissionsMask(role.PermissionsMask);

            // Also create/update UserRole join record for tracking
            var existingUserRole = await _uow.UserRoles.FirstOrDefaultAsync(
                ur => ur.UserId == userId && ur.RoleId == roleId, ct);

            if (existingUserRole == null)
            {
                var userRole = UserRole.Create(userId, roleId);
                await _uow.UserRoles.AddAsync(userRole, ct);
            }

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Assigned role {RoleId} (mask={Mask}) to user {UserId}",
                roleId, role.PermissionsMask, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign role {RoleId} to user {UserId}", roleId, userId);
            return Result.Failure("حدث خطأ أثناء تعيين الدور للمستخدم.");
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  Permission code ↔ mask translation
    // ═════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<Result<List<string>>> GetUserPermissionsAsync(int userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _uow.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
                return Result<List<string>>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

            var mask = user.PermissionsMask;

            // Super Admin: return ALL known permission codes
            if (mask == -1)
                return Result<List<string>>.Success(PermissionCodeToBit.Keys.OrderBy(x => x).ToList());

            // Walk through each known permission bit and check if set in mask
            var granted = new List<string>();
            foreach (var (code, bitValue) in PermissionCodeToBit)
            {
                if ((mask & bitValue) == bitValue)
                    granted.Add(code);
            }

            return Result<List<string>>.Success(granted.OrderBy(x => x).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions for user {UserId}", userId);
            return Result<List<string>>.Failure("حدث خطأ أثناء جلب صلاحيات المستخدم.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> UserHasPermissionAsync(int userId, string permissionName, CancellationToken ct = default)
    {
        try
        {
            // Look up the bit value for this permission code
            if (!PermissionCodeToBit.TryGetValue(permissionName, out var requiredBit))
            {
                _logger.LogWarning("Unknown permission code: {Permission}", permissionName);
                return false;
            }

            var user = await _uow.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
                return false;

            // Super Admin bypass
            if (user.PermissionsMask == -1)
                return true;

            return (user.PermissionsMask & requiredBit) == requiredBit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check permission {Permission} for user {UserId}", permissionName, userId);
            return false;
        }
    }

    // ═════════════════════════════════════════════════════════════════
    //  Public helpers
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the bitmask value for a permission code string.
    /// Returns 0 if the code is unknown.
    /// </summary>
    public static long GetPermissionBit(string permissionCode)
    {
        return PermissionCodeToBit.TryGetValue(permissionCode, out var bit) ? bit : 0;
    }

    /// <summary>
    /// Gets the permission code string for a given bitmask value.
    /// Returns null if the value does not correspond to a single known permission.
    /// </summary>
    public static string? GetPermissionCode(long bitValue)
    {
        return BitToPermissionCode.TryGetValue(bitValue, out var code) ? code : null;
    }

    /// <summary>
    /// Returns all known permission code strings.
    /// </summary>
    public static IReadOnlyCollection<string> AllPermissionCodes => PermissionCodeToBit.Keys;
}
