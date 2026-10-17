using StangaNetLib.Core.Common;
using StangaNetLib.Media.Models;

namespace StangaNetLib.Media.Abstractions;

/// <summary>
/// Processes raw image bytes into a set of optimized variants.
/// Implementations should be registered as <c>Singleton</c> (stateless).
/// </summary>
public interface IImageOptimizationService
{
    /// <summary>
    /// Decodes <paramref name="imageData"/>, generates Thumbnail / Medium / Large / Original
    /// JPEG variants, and optionally generates a WebP variant.
    /// </summary>
    /// <param name="imageData">Raw bytes of the source image (JPEG, PNG, or WebP).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing the full <see cref="ImageVariants"/> set on success,
    /// or an <c>Error</c> with code <c>Media.ProcessingFailed</c> on failure.
    /// </returns>
    Task<Result<ImageVariants>> ProcessAsync(
        byte[]            imageData,
        CancellationToken ct = default);

    /// <summary>
    /// Resizes <paramref name="imageData"/> so its longest side fits within
    /// <paramref name="maxDimension"/> pixels, preserving aspect ratio.
    /// Never upscales an image smaller than <paramref name="maxDimension"/>.
    /// </summary>
    /// <param name="imageData">Raw bytes of the source image.</param>
    /// <param name="maxDimension">Maximum pixels on the longest side.</param>
    /// <param name="quality">JPEG quality 0–100.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing a single JPEG <see cref="ImageVariant"/> on success,
    /// or an <c>Error</c> with code <c>Media.ProcessingFailed</c> on failure.
    /// </returns>
    Task<Result<ImageVariant>> ResizeAsync(
        byte[]            imageData,
        int               maxDimension,
        int               quality,
        CancellationToken ct = default);

    /// <summary>
    /// Converts <paramref name="imageData"/> to lossy WebP at the given quality.
    /// If <paramref name="maxDimension"/> is greater than zero the image is also resized
    /// so its longest side fits within that limit.
    /// </summary>
    /// <param name="imageData">Raw bytes of the source image.</param>
    /// <param name="quality">WebP lossy quality 0–100.</param>
    /// <param name="maxDimension">
    /// Optional maximum pixels on the longest side. Pass <c>0</c> (default) to skip resizing.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result{T}"/> containing a WebP <see cref="ImageVariant"/> on success,
    /// or an <c>Error</c> with code <c>Media.ProcessingFailed</c> on failure.
    /// </returns>
    Task<Result<ImageVariant>> ConvertToWebPAsync(
        byte[]            imageData,
        int               quality,
        int               maxDimension = 0,
        CancellationToken ct = default);
}
