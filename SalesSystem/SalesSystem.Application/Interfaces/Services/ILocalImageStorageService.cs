using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Handles saving, deleting, and resolving product images on the local file system.
/// Images are stored at %AppData%\SalesSystem\Images with per-product subfolders.
/// Only relative paths are persisted in the database.
/// </summary>
public interface ILocalImageStorageService
{
    /// <summary>
    /// Saves an image from byte array to the local file system.
    /// Validates extension (.jpg, .jpeg, .png only) and size (&lt; 2 MB).
    /// </summary>
    /// <param name="imageBytes">The image bytes.</param>
    /// <param name="fileName">Original file name (used to determine extension).</param>
    /// <param name="productId">The product ID for folder organization.</param>
    /// <returns>Result with the relative image path (e.g., "product_42/abc.jpg").</returns>
    Task<Result<string>> SaveImageAsync(byte[] imageBytes, string fileName, int productId);

    /// <summary>
    /// Deletes an image from the local file system by its relative path.
    /// Returns success if the path is null, empty, or the file does not exist.
    /// </summary>
    Task<Result> DeleteImageAsync(string? imagePath);

    /// <summary>
    /// Gets the absolute file system path for a relative image path.
    /// Returns string.Empty if the relative path is null or whitespace.
    /// </summary>
    string GetAbsolutePath(string? relativePath);
}
