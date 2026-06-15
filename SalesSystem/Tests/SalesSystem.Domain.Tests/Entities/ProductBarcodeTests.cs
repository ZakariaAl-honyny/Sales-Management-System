using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesSystem.Domain.Tests.Entities;

/// <summary>
/// ProductBarcode entity was removed in v4.10.
/// Barcodes are now stored as Product.Barcode (string) and via UnitBarcode table.
/// This test file is intentionally empty — barcode validation is covered by
/// ProductTests and UnitBarcodeTests.
/// </summary>
public class ProductBarcodeTests
{
    // No tests — entity removed.
}
