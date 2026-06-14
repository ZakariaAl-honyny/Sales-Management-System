using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesSystem.Domain.Tests.Entities;

/// <summary>
/// ProductPriceHistory entity was removed in v4.10.
/// Price and cost history is now tracked via ProductPrice.EffectiveFrom/To
/// and inventory cost records. This test file is intentionally empty.
/// </summary>
public class ProductPriceHistoryTests
{
    // No tests — entity removed.
}
