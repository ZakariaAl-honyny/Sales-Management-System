# API Contracts: Touch-Optimized Quick POS Interface

**Feature**: `015-touch-pos-interface`

## API Endpoints

*Note: This feature is a frontend interface overhaul. It does not introduce any new API endpoints. It strictly consumes the existing API architecture.*

### Reused Endpoints

1. **Get Categories**: `GET /api/v1/categories`
2. **Get Products by Category**: `GET /api/v1/products?categoryId={id}`
3. **Submit Draft/Posted Invoice**: `POST /api/v1/sales`
   - Payload: `CreateSalesInvoiceRequest`
   - Uses exactly the same logic to compute totals, validate stock, and generate journal entries/inventory movements on the backend.
