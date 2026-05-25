# Data Model: Touch-Optimized Quick POS Interface

**Feature**: `015-touch-pos-interface`

## Entities

*Note: Phase 15 is entirely a frontend UI/UX feature. It relies 100% on the existing backend domain entities and EF Core database schema. No changes to the database or C# Domain Entities are required.*

### Reused Entities (No Changes)
- `SalesSystem.Domain.Entities.SalesInvoice`
- `SalesSystem.Domain.Entities.SalesInvoiceItem`
- `SalesSystem.Domain.Entities.Product`
- `SalesSystem.Domain.Entities.Category`

The Touch POS will interact with these entities strictly via the `SalesSystem.Contracts` DTOs and existing API endpoints.
