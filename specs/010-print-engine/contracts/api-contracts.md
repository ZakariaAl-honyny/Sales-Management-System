# API Contracts: Print Engine (v4.3)

**Feature**: `010-print-engine`
**Date**: 2026-05-24

---

## `PrintController` — `/api/v1/print`

All endpoints require `[Authorize]`.

### `POST /api/v1/print/a4/{invoiceId}`

Generates an A4 PDF document for the given invoice.

**Request:** Empty body. `invoiceId` in path.
**Validation:** `invoiceId` must exist and invoice `Status` must be `Posted(2)`.
**Response:**
- `200 OK`: Returns the raw PDF byte array as `FileContentResult` (`application/pdf`) with a filename like `Invoice_{invoiceId}.pdf`.
- `400 Bad Request`: If invoice is Draft or Cancelled.
- `404 Not Found`: If invoice does not exist.
- `500 Internal Server Error`: If generation fails (caught and mapped from `Result.Failure`).

### `POST /api/v1/print/thermal/{invoiceId}`

Generates and sends ESC/POS commands directly to the configured Win32 thermal printer.

**Request:** Empty body. `invoiceId` in path.
**Validation:** `invoiceId` must exist and invoice `Status` must be `Posted(2)`.
**Response:**
- `200 OK`: Returns `Result.Success` (`PrintResult` mapped to `Result`).
- `400 Bad Request`: If invoice is Draft or Cancelled, or if printer name is not configured in settings.
- `404 Not Found`: If invoice does not exist.
- `500 Internal Server Error`: If Win32 spooler call fails (e.g., printer offline).

---

## `SettingsController` (Existing extensions)

The existing Settings controller will handle reading/updating the `Print` category settings. No new endpoints are technically required, but the UI must send the correct keys:

**Keys:**
- `Print.StoreName`
- `Print.StoreAddress`
- `Print.StorePhone`
- `Print.ReceiptHeader`
- `Print.ReceiptFooter`
- `Print.AutoPrintOnPost`
- `Print.LogoPath`
- `Print.ThermalPrinterName`
- `Print.EscPosCodePage`
