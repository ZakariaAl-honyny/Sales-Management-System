# Data Model: Multi-Window & UI Polish (v4.5)

**Feature**: `012-multi-window-ui`
**Date**: 2026-05-25

---

## No Database Changes

This feature is strictly a UI/Client-side update. It does not introduce any new database tables, EF Core models, or API DTOs.

## Client-Side State

### `ScreenWindowService` State
- Maintains `List<WeakReference<Window>> _openWindows` to track non-modal instances.
- Cleans up dead references when enumerating the list to calculate cascade offsets.
