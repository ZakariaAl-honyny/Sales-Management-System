using System.Text;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

/// <summary>
/// Implements bulk product import from structured JSON data.
/// The Desktop client parses Excel using ClosedXML and sends the rows to the API.
/// This service validates and persists products, auto-creating categories as needed.
/// </summary>
public class ProductImportService : IProductImportService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductImportService> _logger;

    public ProductImportService(IUnitOfWork uow, ILogger<ProductImportService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<ProductImportResultDto>> PreviewAsync(List<ProductImportRowDto> rows, CancellationToken ct)
    {
        if (rows == null || rows.Count == 0)
            return Result<ProductImportResultDto>.Failure("لا توجد بيانات للاستيراد. يرجى رفع ملفExcel يحتوي على بيانات.");

        var errors = new List<ProductImportErrorDto>();
        var seenProductNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNum = i + 2; // 1-based + header row
            var rowErrors = new List<string>();

            var productName = row.ProductName?.Trim();

            if (string.IsNullOrWhiteSpace(productName))
                rowErrors.Add("اسم المنتج مطلوب");

            if (!row.BaseUnitId.HasValue || row.BaseUnitId.Value <= 0)
                rowErrors.Add("معرف الوحدة الأساسية مطلوب");

            // Check for duplicate product names within the import file
            if (!string.IsNullOrWhiteSpace(productName))
            {
                if (seenProductNames.Contains(productName))
                    rowErrors.Add($"اسم المنتج '{productName}' مكرر في ملف الاستيراد");
                else
                    seenProductNames.Add(productName);
            }

            // Barcode uniqueness check against existing products
            if (!string.IsNullOrWhiteSpace(row.Barcode))
            {
                var existing = await _uow.Products.FirstOrDefaultAsync(
                    p => p.Barcode == row.Barcode, ct);
                if (existing != null)
                    rowErrors.Add($"الباركود '{row.Barcode}' مستخدم بالفعل للمنتج '{existing.Name}'");
            }

            // MinStockLevel validation
            if (row.MinStockLevel.HasValue && row.MinStockLevel.Value < 0)
                rowErrors.Add("الحد الأدنى للمخزون لا يمكن أن يكون سالباً");

            // Validate BaseUnitId references an existing Unit
            if (row.BaseUnitId.HasValue && row.BaseUnitId.Value > 0)
            {
                var unitExists = await _uow.Units.AnyAsync(u => u.Id == row.BaseUnitId.Value, ct);
                if (!unitExists)
                    rowErrors.Add($"الوحدة ذات المعرف {row.BaseUnitId.Value} غير موجودة في النظام");
            }

            if (rowErrors.Count > 0)
            {
                errors.Add(new ProductImportErrorDto(rowNum, productName ?? "(بدون اسم)", string.Join(" ; ", rowErrors)));
            }
        }

        var successCount = rows.Count - errors.Count;

        return Result<ProductImportResultDto>.Success(new ProductImportResultDto(
            TotalRows: rows.Count,
            SuccessCount: successCount,
            FailureCount: errors.Count,
            Errors: errors
        ));
    }

    /// <inheritdoc/>
    public async Task<Result<ProductImportResultDto>> ExecuteAsync(List<ProductImportRowDto> rows, int userId, CancellationToken ct)
    {
        if (rows == null || rows.Count == 0)
            return Result<ProductImportResultDto>.Failure("لا توجد بيانات للاستيراد");

        // Run preview first to catch basic validation errors
        var previewResult = await PreviewAsync(rows, ct);
        if (!previewResult.IsSuccess || previewResult.Value == null)
            return previewResult;

        var errors = new List<ProductImportErrorDto>(previewResult.Value.Errors);
        var successCount = 0;

        // Use ExecuteTransactionAsync for atomicity per RULE-275
        // (SqlServerRetryingExecutionStrategy is configured, so we cannot use BeginTransactionAsync directly)
        return await _uow.ExecuteTransactionAsync(async () =>
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNum = i + 2;

                // Skip rows with preview errors
                if (errors.Any(e => e.RowNumber == rowNum))
                    continue;

                try
                {
                    // Find or create category by name
                    int? categoryId = null;
                    if (!string.IsNullOrWhiteSpace(row.CategoryName))
                    {
                        var categoryName = row.CategoryName.Trim();
                        var category = await _uow.ProductCategories.FirstOrDefaultAsync(
                            c => c.Name == categoryName, ct);
                        if (category == null)
                        {
                            category = ProductCategory.Create(categoryName, createdByUserId: userId);
                            await _uow.ProductCategories.AddAsync(category, ct);
                            await _uow.SaveChangesAsync(ct);
                            categoryId = category.Id;
                        }
                        else
                        {
                            categoryId = category.Id;
                        }
                    }

                    // Create the product entity
                    var product = Product.Create(
                        name: row.ProductName!.Trim(),
                        categoryId: categoryId ?? 0,
                        description: row.Description?.Trim(),
                        barcode: row.Barcode?.Trim(),
                        reorderLevel: row.MinStockLevel ?? 0,
                        trackExpiry: false,
                        createdByUserId: userId
                    );
                    await _uow.Products.AddAsync(product, ct);
                    await _uow.SaveChangesAsync(ct);

                    // Create the base product unit (required per RULE-067)
                    // Phase 25: UnitId replaces UnitName, cost managed via InventoryBatches
                    var baseUnit = ProductUnit.CreateBaseUnit(
                        product.Id,
                        row.BaseUnitId!.Value
                    );
                    await _uow.ProductUnits.AddAsync(baseUnit, ct);
                    // Add the unit to the product's in-memory collection for consistency
                    product.AddUnit(baseUnit);

                    successCount++;
                }
                catch (DomainException ex)
                {
                    _logger.LogWarning(ex, "Product import domain rule violation at row {RowNum}: {ProductName}",
                        rowNum, row.ProductName);
                    errors.Add(new ProductImportErrorDto(rowNum, row.ProductName ?? "(بدون اسم)", ex.Message));
                }
                catch (Exception ex) when (ex.GetType().Name.Contains("DbUpdate") || ex.GetType().Name.Contains("Sql"))
                {
                    _logger.LogError(ex, "Database error importing product row {RowNum}: {ProductName}",
                        rowNum, row.ProductName);
                    errors.Add(new ProductImportErrorDto(rowNum, row.ProductName ?? "(بدون اسم)",
                        "خطأ في قاعدة البيانات أثناء استيراد المنتج"));
                }
            }

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Product import completed: {Success} succeeded, {Failed} failed out of {Total}",
                successCount, errors.Count, rows.Count);

            return Result<ProductImportResultDto>.Success(new ProductImportResultDto(
                TotalRows: rows.Count,
                SuccessCount: successCount,
                FailureCount: errors.Count,
                Errors: errors
            ));
        }, ct);
    }

    /// <inheritdoc/>
    public byte[] GenerateTemplate()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Product Name,Category,Barcode,Base Unit ID,Min Stock Level,Description");
        sb.AppendLine("Example Product,General,1234567890,1,5,Optional description");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
