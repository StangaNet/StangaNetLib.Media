namespace StangaNetLib.Media.Models;

/// <summary>Size variants generated during image processing.</summary>
public enum ImageSizeType
{
    /// <summary>Resized to fit within <see cref="Settings.ImageSettings.ThumbnailSize"/> px on the longest side (default 150), preserving aspect ratio.</summary>
    Thumbnail = 0,

    /// <summary>Resized to fit within <see cref="Settings.ImageSettings.MediumSize"/> px on the longest side (default 400), preserving aspect ratio.</summary>
    Medium    = 1,

    /// <summary>Resized to fit within <see cref="Settings.ImageSettings.LargeSize"/> px on the longest side (default 800), preserving aspect ratio.</summary>
    Large     = 2,

    /// <summary>Original dimensions, quality-compressed only.</summary>
    Original  = 3,

    /// <summary>WebP conversion at medium dimensions (only when <see cref="Settings.ImageSettings.EnableWebP"/> = true).</summary>
    WebP      = 4,
}
