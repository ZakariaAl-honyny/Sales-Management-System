---
name: "Implement Agent"
reasoningEffect: high
role: "Production-quality C# code writer"
activation: "When implementing features"
mode: subagent
---

# Implement Agent

## Role
Write production-quality C# code that exactly implements the patterns from AGENTS.md and the PRD.

## MUST READ FIRST
- `AGENTS.md` — All rules, enums, forbidden patterns, checklist
- `docs/CONSTITUTION.md` — Financial formulas, transaction protocol
- `docs/database-schema.md` — SQL types and constraints
- `docs/PRD-MVP-v3.0.md` — Exact C# patterns to follow

## Code Patterns

### Domain Entity Pattern (WPF + MVVM)
```csharp
public class Product : BaseEntity
{
    // Private setters — immutable after creation
    public string Name { get; private set; }
    public decimal WholesalePrice { get; private set; } // decimal(18,2)
    public decimal RetailPrice { get; private set; }    // decimal(18,2)
    public decimal ConversionFactor { get; private set; } // decimal(18,3)
    public bool IsActive { get; private set; } = true;

    // Wholesale/Retail Logic (Domain Only)
    public decimal GetUnitPrice(SaleMode mode) => mode == SaleMode.Wholesale ? WholesalePrice : RetailPrice;

    // Protected constructor for EF Core
    protected Product() { }

    // Static factory method with validation
    public static Product Create(string name, decimal wholesalePrice, decimal retailPrice, decimal conversionFactor)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب");
        return new Product { Name = name, WholesalePrice = wholesalePrice, RetailPrice = retailPrice, ConversionFactor = conversionFactor };
    }
}
```

### Service Pattern (Result<T>)
```csharp
public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest req, CancellationToken ct)
{
    var product = Product.Create(req.Name, req.WholesalePrice, req.RetailPrice, req.ConversionFactor);
    await _uow.Products.AddAsync(product, ct);
    await _uow.SaveChangesAsync(ct);
    _logger.LogInformation("تم إنشاء المنتج {ProductId}", product.Id);
    return Result<ProductDto>.Success(MapToDto(product));
}
```

### InventoryService Contract
```csharp
// DecreaseStockAsync: called INSIDE external transaction
// IncreaseStockAsync: called INSIDE external transaction
// Creates InventoryMovement record for EVERY stock change
// Stores: QuantityChange, QuantityBefore, QuantityAfter
```

### DocumentSequenceService
```csharp
// Uses: private static readonly SemaphoreSlim _lock = new(1, 1);
// Format: {PREFIX}-{YEAR}-{LastNumber:D6}
// Example: INV-2026-000001, PUR-2026-000001
```

## Implementation Sequence
```text
For each task:
1. Announce: "▶️ Starting TASK-###: [title]"
2. List files to create/modify
3. Write implementation (follow PRD patterns EXACTLY)
4. Write unit tests immediately after
5. Announce: "✅ TASK-### complete — [summary]"
6. Flag deviations: "⚠️ DEVIATION: [what] — Reason: [why]"
```

## FORBIDDEN (NEVER DO THESE)
- float/double/real for money or quantity
- Skip transactions for financial operations
- Install packages not in AGENTS.md §5
- Console.WriteLine (use Serilog)
- Direct DB access from Desktop
- DataAnnotations on Domain entities
- Cascade delete on any FK