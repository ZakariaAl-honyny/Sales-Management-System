using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Infrastructure.Services;

/// <summary>
/// Saves product images to the local file system at %AppData%\SalesSystem\Images.
/// Only relative paths (e.g. "product_42/abc.jpg") are stored in the database.
/// </summary>
public class LocalImageStorageService : ILocalImageStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalImageStorageService> _logger;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png"
    };

    private const long MaxFileSize = 2 * 1024 * 1024; // 2 MB

    public LocalImageStorageService(ILogger<LocalImageStorageService> logger)
    {
        _logger = logger;
        _basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SalesSystem",
            "Images");

        Directory.CreateDirectory(_basePath);
    }

    public async Task<Result<string>> SaveImageAsync(byte[] imageBytes, string fileName, int productId)
    {
        // Validate bytes
        if (imageBytes is null || imageBytes.Length == 0)
        {
            _logger.LogWarning("Attempted to save empty image for product {ProductId}", productId);
            return Result<string>.Failure("ملف الصورة فارغ");
        }

        // Validate size
        if (imageBytes.Length > MaxFileSize)
        {
            _logger.LogWarning(
                "Image size {Size} bytes exceeds 2 MB limit for product {ProductId}",
                imageBytes.Length,
                productId);
            return Result<string>.Failure("حجم الصورة يتجاوز 2 ميجابايت");
        }

        // Validate extension
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            _logger.LogWarning(
                "Invalid image extension '{Ext}' for product {ProductId}",
                ext,
                productId);
            return Result<string>.Failure("يُسمح فقط بملفات JPG و PNG");
        }

        // Create per-product folder
        var productFolder = Path.Combine(_basePath, $"product_{productId}");
        Directory.CreateDirectory(productFolder);

        // Generate unique filename to prevent collisions
        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var relativePath = $"product_{productId}/{uniqueName}";
        var fullPath = Path.Combine(_basePath, relativePath);

        try
        {
            await File.WriteAllBytesAsync(fullPath, imageBytes);

            _logger.LogInformation(
                "Image saved for product {ProductId}: {RelativePath} ({Size} bytes)",
                productId,
                relativePath,
                imageBytes.Length);

            return Result<string>.Success(relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save image to {FullPath} for product {ProductId}", fullPath, productId);
            return Result<string>.Failure("فشل في حفظ الصورة");
        }
    }

    public Task<Result> DeleteImageAsync(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return Task.FromResult(Result.Success());
        }

        var fullPath = GetAbsolutePath(imagePath);

        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Image deleted: {ImagePath}", imagePath);
            }
            else
            {
                _logger.LogWarning("Image not found for deletion: {ImagePath}", imagePath);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image at {FullPath}", fullPath);
            return Task.FromResult(Result.Failure("فشل في حذف الصورة"));
        }
    }

    public string GetAbsolutePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));

        // Guard: prevent path traversal outside the base directory
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Path traversal blocked: {RelativePath} resolved to {FullPath}",
                relativePath, fullPath);
            return string.Empty;
        }

        return fullPath;
    }
}
