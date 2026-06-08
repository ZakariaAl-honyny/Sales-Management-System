using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing product images.
/// </summary>
public interface IProductImageService
{
    /// <summary>
    /// Gets all active images for a product.
    /// </summary>
    Task<Result<List<ProductImageDto>>> GetByProductAsync(int productId, CancellationToken ct);

    /// <summary>
    /// Creates a new product image record.
    /// </summary>
    Task<Result<ProductImageDto>> CreateAsync(CreateProductImageRequest request, int userId, CancellationToken ct);

    /// <summary>
    /// Sets an image as the primary image for the product.
    /// Unsets any existing primary image.
    /// </summary>
    Task<Result> SetPrimaryAsync(int productId, int imageId, CancellationToken ct);

    /// <summary>
    /// Soft-deletes (deactivates) a product image.
    /// </summary>
    Task<Result> DeactivateAsync(int imageId, CancellationToken ct);
}
