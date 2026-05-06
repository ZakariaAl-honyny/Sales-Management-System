# Research: Foundation Setup

**Date**: 2026-05-06
**Feature**: Phase 1 — Foundation
**Branch**: `001-foundation-setup`

## Summary

All technology decisions for this feature are pre-determined by the project
constitution and PRD. No external research was required. This document
records the decisions and rationale for traceability.

---

## Decision 1: Solution Structure

**Decision**: 6-project Clean Architecture solution
**Rationale**: Mandated by constitution (Principle VII). Enforces strict
dependency direction: Domain → Application → Infrastructure → Api/Desktop.
Contracts is a shared project referenced by all layers.
**Alternatives considered**:
- Monolithic single project — rejected (violates Clean Architecture mandate)
- Vertical slice architecture — rejected (not aligned with team's domain model)

## Decision 2: ORM and Database Strategy

**Decision**: EF Core 10 Code-First with Fluent API configurations
**Rationale**: Constitution Principle XI mandates Fluent API only (no
DataAnnotations on entities). Code-First approach ensures schema is defined
in C# and versioned with the codebase.
**Alternatives considered**:
- Database-First — rejected (harder to version control, entity classes
  would need manual editing to remove DataAnnotations)
- Dapper — rejected (no migration support, more manual SQL required)

## Decision 3: BaseEntity Pattern

**Decision**: Abstract `BaseEntity` with `Id (int)`, `CreatedAt (DateTime)`,
`UpdatedAt (DateTime?)`, `IsActive (bool)` properties
**Rationale**: All 22 tables share these audit/lifecycle fields. BaseEntity
reduces duplication and enables global query filter for soft delete.
**Alternatives considered**:
- No base class — rejected (excessive duplication across 22+ entities)
- Multiple base classes (AuditableEntity, SoftDeletableEntity) — rejected
  (over-engineering for Phase 1; can refactor later if needed)

## Decision 4: Financial Type Strategy

**Decision**: `decimal` for ALL money (18,2) and quantity (18,3) fields
**Rationale**: Constitution Principle I (NON-NEGOTIABLE). Floating-point
types introduce rounding errors unacceptable in financial systems.
**Alternatives considered**: None — this is a constitutional mandate.

## Decision 5: Seed Data Approach

**Decision**: EF Core `HasData()` in entity configurations
**Rationale**: Seed data is applied during migration, ensuring every fresh
database has the required records. Idempotent by design.
**Alternatives considered**:
- SQL scripts — rejected (separate from migration pipeline, easy to forget)
- Runtime seeding in Program.cs — rejected (runs every startup, needs
  duplicate-check logic)

## Decision 6: Password Hashing for Seed User

**Decision**: Pre-computed BCrypt hash stored in seed data configuration
**Rationale**: Constitution Principle VIII mandates BCrypt with work
factor 12. The admin seed password hash is computed once and embedded
in the `UserConfiguration.HasData()` call.
**Alternatives considered**:
- Runtime hashing during seed — rejected (BCrypt is slow by design; would
  slow migration unnecessarily)

## Decision 7: Domain Exception Strategy

**Decision**: Three exception types — `DomainException`, `NotFoundException`,
`ValidationException`
**Rationale**: Clear separation of business rule violations (Domain), missing
entity lookups (NotFound), and input validation failures (Validation).
Services catch these and return appropriate `Result<T>` in Phase 2.
**Alternatives considered**:
- Single exception type with error codes — rejected (less expressive)
- No exceptions, pure Result everywhere — rejected (domain entities need
  guard clauses that throw; services wrap in Result<T>)

---

## All NEEDS CLARIFICATION: Resolved

No unresolved clarifications. All technology choices locked by constitution.
