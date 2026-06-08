using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class ProductImageService : IProductImageService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductImageService> _logger;

    public ProductImageService(IUnitOfWork uow, ILogger<ProductImageService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<ProductImageDto>>> GetByProductAsync(int productId, CancellationToken ct)
    {
        try
        {
            var images = await _uow.ProductImages.ToListAsync(
                i => i.ProductId == productId,
                q => q.OrderBy(i => i.SortOrder).ThenByDescending(i => i.IsPrimary),
                ct);

            var dtos = images.Select(MapToDto).ToList();
            return Result<List<ProductImageDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving images for product {ProductId}", productId);
            return Result<List<ProductImageDto>>.Failure("حدث خطأ أثناء استرجاع الصور");
        }
    }

    public async Task<Result<ProductImageDto>> CreateAsync(CreateProductImageRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Validate product exists
            var product = await _uow.Products.GetByIdAsync(request.ProductId, ct);
            if (product == null)
                return Result<ProductImageDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

            // If this image is set as primary, unset any existing primary
            if (request.IsPrimary)
            {
                var existingPrimary = await _uow.ProductImages.FirstOrDefaultAsync(
                    i => i.ProductId == request.ProductId && i.IsPrimary && i.IsActive, ct);
                if (existingPrimary != null)
                    existingPrimary.UnsetPrimary();
            }

            var image = ProductImage.Create(
                request.ProductId,
                request.ImagePath,
                request.IsPrimary,
                request.SortOrder,
                userId);

            await _uow.ProductImages.AddAsync(image, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Product image created: Product={ProductId}, Path={ImagePath}, IsPrimary={IsPrimary} by User {UserId}",
                request.ProductId, request.ImagePath, request.IsPrimary, userId);

            return Result<ProductImageDto>.Success(MapToDto(image));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating product image: {Message}", ex.Message);
            return Result<ProductImageDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product image");
            return Result<ProductImageDto>.Failure("حدث خطأ أثناء إضافة الصورة");
        }
    }

    public async Task<Result> SetPrimaryAsync(int productId, int imageId, CancellationToken ct)
    {
        try
        {
            var image = await _uow.ProductImages.FirstOrDefaultAsync(
                i => i.Id == imageId && i.ProductId == productId, ct);
            if (image == null)
                return Result.Failure("الصورة غير موجودة", ErrorCodes.NotFound);

            // Unset any existing primary for this product
            var existingPrimary = await _uow.ProductImages.FirstOrDefaultAsync(
                i => i.ProductId == productId && i.IsPrimary && i.IsActive && i.Id != imageId, ct);
            if (existingPrimary != null)
                existingPrimary.UnsetPrimary();

            // Set this image as primary
            image.SetPrimary();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Product image {ImageId} set as primary for product {ProductId}",
                imageId, productId);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation setting primary image: {Message}", ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary image {ImageId} for product {ProductId}", imageId, productId);
            return Result.Failure("حدث خطأ أثناء تعيين الصورة الأساسية");
        }
    }

    public async Task<Result> DeactivateAsync(int imageId, CancellationToken ct)
    {
        try
        {
            var image = await _uow.ProductImages.FirstOrDefaultAsync(
                i => i.Id == imageId, ct);
            if (image == null)
                return Result.Failure("الصورة غير موجودة", ErrorCodes.NotFound);

            image.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Product image {ImageId} deactivated", imageId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating product image {ImageId}", imageId);
            return Result.Failure("حدث خطأ أثناء حذف الصورة");
        }
    }

    // ─── Mapping ─────────────────────────────────

    private static ProductImageDto MapToDto(ProductImage image) => new(
        image.Id,
        image.ProductId,
        image.ImagePath,
        image.IsPrimary,
        image.SortOrder,
        image.IsActive);
}
