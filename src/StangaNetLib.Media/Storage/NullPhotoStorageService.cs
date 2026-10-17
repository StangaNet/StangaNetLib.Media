using StangaNetLib.Core.Common;
using StangaNetLib.Media.Abstractions;
using StangaNetLib.Media.Models;

namespace StangaNetLib.Media.Storage;

/// <summary>
/// No-op implementation for use in unit tests of code that depends on
/// <see cref="IPhotoStorageService"/>. Always returns success without touching the filesystem.
/// Register manually in your test DI setup:
/// <code>services.AddSingleton&lt;IPhotoStorageService, NullPhotoStorageService&gt;();</code>
/// </summary>
public sealed class NullPhotoStorageService : IPhotoStorageService
{
    /// <inheritdoc/>
    public Task<Result<PhotoUploadResult>> UploadVariantsAsync(
        string            ownerId,
        ImageVariants     variants,
        CancellationToken ct = default)
    {
        var photoId = Guid.NewGuid().ToString("N");
        var urls    = new PhotoUrls(
            ThumbnailUrl: $"/null/{ownerId}/{photoId}_thumb.jpg",
            MediumUrl:    $"/null/{ownerId}/{photoId}_medium.jpg",
            LargeUrl:     $"/null/{ownerId}/{photoId}_large.jpg",
            OriginalUrl:  $"/null/{ownerId}/{photoId}_original.jpg",
            WebPUrl:      variants.WebP is not null
                              ? $"/null/{ownerId}/{photoId}_webp.webp"
                              : null);

        return Task.FromResult(
            Result<PhotoUploadResult>.Success(new PhotoUploadResult(photoId, urls)));
    }

    /// <inheritdoc/>
    public Task<Result> DeleteVariantsAsync(
        string            ownerId,
        string            photoId,
        CancellationToken ct = default)
        => Task.FromResult(Result.Success());

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(
        string            ownerId,
        string            photoId,
        CancellationToken ct = default)
        => Task.FromResult(false);
}
