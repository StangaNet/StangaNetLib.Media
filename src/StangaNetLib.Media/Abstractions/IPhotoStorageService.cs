using StangaNetLib.Core.Common;
using StangaNetLib.Media.Models;

namespace StangaNetLib.Media.Abstractions;

/// <summary>
/// Persists and retrieves photo variants from a storage backend.
/// Implementations should be registered as <c>Singleton</c>.
/// </summary>
public interface IPhotoStorageService
{
    /// <summary>
    /// Stores all variants of a photo and returns the assigned <c>PhotoId</c> plus public URLs.
    /// </summary>
    /// <param name="ownerId">
    /// Logical owner of the photo (userId, partnerId, etc.).
    /// Used as a path prefix to keep photos grouped.
    /// </param>
    /// <param name="variants">The processed image variants to upload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing a <see cref="PhotoUploadResult"/> with the stable
    /// <c>PhotoId</c> and public URLs on success, or an <c>Error</c> with code
    /// <c>Storage.UploadFailed</c> on failure.
    /// </returns>
    Task<Result<PhotoUploadResult>> UploadVariantsAsync(
        string            ownerId,
        ImageVariants     variants,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes all variants belonging to <paramref name="photoId"/> for the given owner.
    /// Safe to call when the photo does not exist — returns success without error.
    /// </summary>
    /// <param name="ownerId">Logical owner of the photo.</param>
    /// <param name="photoId">The <c>PhotoId</c> returned by <see cref="UploadVariantsAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result"/> indicating success, or an <c>Error</c> with code
    /// <c>Storage.DeleteFailed</c> on failure.
    /// </returns>
    Task<Result> DeleteVariantsAsync(
        string            ownerId,
        string            photoId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns <see langword="true"/> if any variant for the given <paramref name="photoId"/>
    /// exists in storage.
    /// </summary>
    /// <param name="ownerId">Logical owner of the photo.</param>
    /// <param name="photoId">The <c>PhotoId</c> returned by <see cref="UploadVariantsAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> if at least one variant is found; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(
        string            ownerId,
        string            photoId,
        CancellationToken ct = default);
}
