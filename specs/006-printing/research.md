# Research: Phase 6 — Printing

## Technical Context Unknowns

**1. Which library to use for printing in WinForms?**
- **Decision**: Use `System.Drawing.Printing` built into .NET WinForms.
- **Rationale**: It provides `PrintDocument`, `PrintPreviewDialog`, and `PrintDialog` natively, avoiding third-party dependencies for simple A4 and thermal receipt printing. It allows custom GDI+ drawing (`e.Graphics.DrawString`) which is perfect for precise 80mm and A4 layouts.
- **Alternatives considered**: PDF generation (e.g., iText7) then printing, but it adds unnecessary overhead and licensing concerns for a simple MVP desktop app.

**2. How to handle 80mm Thermal Receipt Layout?**
- **Decision**: Define a custom `PaperSize` with a width of approximately 3.14 inches (314 hundredths of an inch) and a dynamic height based on the number of invoice items.
- **Rationale**: Thermal printers use continuous roll paper. Setting a fixed large height might cause feed issues, but dynamically calculating the content height ensures no wasted paper.
- **Alternatives considered**: Fixed height A4 scaled down (would look terrible and waste paper).

**3. How to handle Arabic RTL in `System.Drawing`?**
- **Decision**: Use `StringFormat` with `StringFormatFlags.DirectionRightToLeft` when drawing text.
- **Rationale**: Native support in GDI+ for RTL rendering. Ensures Arabic text and numbers align correctly.

**4. How to fetch Store Settings (Logo, Address)?**
- **Decision**: Add a `StoreSettingsService` or load from existing application configuration/settings repository.
- **Rationale**: The specification requires store name, phone, address, and logo.

All unknowns are resolved.
