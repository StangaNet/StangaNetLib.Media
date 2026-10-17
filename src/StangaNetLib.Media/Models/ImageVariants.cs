namespace StangaNetLib.Media.Models;

/// <summary>
/// Container for all variants produced by <see cref="Abstractions.IImageOptimizationService.ProcessAsync"/>.
/// </summary>
public sealed class ImageVariants
{
    /// <summary>JPEG variant resized to fit within <see cref="Settings.ImageSettings.ThumbnailSize"/> px.</summary>
    public required ImageVariant  Thumbnail { get; init; }

    /// <summary>JPEG variant resized to fit within <see cref="Settings.ImageSettings.MediumSize"/> px.</summary>
    public required ImageVariant  Medium    { get; init; }

    /// <summary>JPEG variant resized to fit within <see cref="Settings.ImageSettings.LargeSize"/> px.</summary>
    public required ImageVariant  Large     { get; init; }

    /// <summary>JPEG variant at the original dimensions, quality-compressed only.</summary>
    public required ImageVariant  Original  { get; init; }

    /// <summary>WebP variant at medium dimensions. <see langword="null"/> when <see cref="Settings.ImageSettings.EnableWebP"/> is <see langword="false"/>.</summary>
    public ImageVariant?          WebP      { get; init; }
}
