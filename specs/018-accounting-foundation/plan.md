# Implementation Plan: Accounting Foundation

**Branch**: `018-accounting-foundation` | **Date**: 2026-05-25 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/018-accounting-foundation/spec.md`

## Summary

The Accounting Foundation phase builds the central financial backbone of the Sales Management System. It implements the Chart of Accounts, Journal Entries, Journal Entry Lines, and System Account Mappings across the Domain, Application, and Infrastructure layers using Clean Architecture. The primary focus is enforcing strict double-entry bookkeeping validation (`Total Debit == Total Credit`) and maintaining data integrity via EF Core configurations and decimal precision `(18,2)`.

## Technical Context

**Language/Version**: C# 14 / .NET 10 LTS
**Primary Dependencies**: EF Core, MediatR, FluentValidation, xUnit
**Storage**: SQL Server
**Testing**: xUnit with Moq (Application & Domain layers)
**Target Platform**: ASP.NET Core Web API / WPF Desktop
**Project Type**: Clean Architecture Enterprise System
**Performance Goals**: Fast financial queries using `AsNoTracking`
**Constraints**: Decimal precision MUST be `(18,2)`. Zero tolerance for unbalanced entries. `IsSystemAccount` must be protected.
**Scale/Scope**: 6 Database Tables, 4 Domain Entities, MediatR Commands/Queries, 9 Unit Tests.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Decimal Types**: `decimal(18,2)` for financial values (Adhered - RULE-211 enforced).
- **Result Pattern**: MediatR handlers must return appropriate types/Results (Adhered).
- **No Domain DB Refs**: The Domain has zero references to EF Core (Adhered).
- **Arabic Messages**: All DomainExceptions use Arabic messages (Adhered).

All constitution checks pass.

## Project Structure

### Documentation (this feature)

```text
specs/018-accounting-foundation/
â”œâ”€â”€ plan.md              # This file
â”œâ”€â”€ research.md          # Phase 0 output
â”œâ”€â”€ data-model.md        # Phase 1 output
â”œâ”€â”€ quickstart.md        # Phase 1 output
â”œâ”€â”€ contracts/           # Phase 1 output
â””â”€â”€ tasks.md             # Phase 2 output (Pending)
```

### Source Code (repository root)

```text
SalesSystem/
â”œâ”€â”€ SalesSystem.Domain/
â”‚   â””â”€â”€ Accounting/
â”‚       â”œâ”€â”€ Entities/
â”‚       â”‚   â”œâ”€â”€ Account.cs
â”‚       â”‚   â”œâ”€â”€ JournalEntry.cs
â”‚       â”‚   â”œâ”€â”€ JournalEntryLine.cs
â”‚       â”‚   â””â”€â”€ SystemAccountMappings.cs
â”‚       â””â”€â”€ Enums/
â”‚           â”œâ”€â”€ AccountType.cs
â”‚           â””â”€â”€ JournalEntryType.cs
â”œâ”€â”€ SalesSystem.Application/
â”‚   â””â”€â”€ Accounting/
â”‚       â”œâ”€â”€ Commands/
â”‚       â”‚   â””â”€â”€ CreateJournalEntry/
â”‚       â”œâ”€â”€ Queries/
â”‚       â”‚   â”œâ”€â”€ GetAccountBalance/
â”‚       â”‚   â””â”€â”€ GetAccountStatement/
â”‚       â””â”€â”€ Services/
â”‚           â”œâ”€â”€ JournalEntryNumberGenerator.cs
â”‚           â””â”€â”€ SystemAccountService.cs
â”œâ”€â”€ SalesSystem.Infrastructure/
â”‚   â””â”€â”€ Persistence/
â”‚       â””â”€â”€ Configurations/
â”‚           â”œâ”€â”€ AccountConfiguration.cs
â”‚           â”œâ”€â”€ JournalEntryConfiguration.cs
â”‚           â””â”€â”€ JournalEntryLineConfiguration.cs
â””â”€â”€ SalesSystem.Api.Tests/
    â””â”€â”€ Domain/
        â””â”€â”€ JournalEntryTests.cs
```

**Structure Decision**: Clean Architecture with feature folders (`Accounting`) within each layer.

## Complexity Tracking

*(No complexity violations. The design adheres strictly to the existing architecture).*

