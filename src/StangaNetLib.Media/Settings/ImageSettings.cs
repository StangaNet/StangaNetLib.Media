namespace StangaNetLib.Media.Settings;

/// <summary>Configuration for image resizing and quality. Bind to "ImageSettings" in appsettings.json.</summary>
public sealed class ImageSettings
{
    public const string SectionName = "ImageSettings";

    /// <summary>Max pixels for the Thumbnail variant (default 150).</summary>
    public int ThumbnailSize { get; set; } = 150;

    /// <summary>Max pixels for the Medium variant (default 400).</summary>
    public int MediumSize { get; set; } = 400;

    /// <summary>Max pixels for the Large variant (default 800).</summary>
    public int LargeSize { get; set; } = 800;

    /// <summary>JPEG quality 0-100 for Medium, Large, and Original variants (default 80).</summary>
    public int CompressionQuality { get; set; } = 80;

    /// <summary>JPEG quality 0-100 for Thumbnail (default 75).</summary>
    public int ThumbnailQuality { get; set; } = 75;

    /// <summary>Maximum allowed upload size in bytes (default 5 MB).</summary>
    public long MaxUploadSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Generate a WebP variant alongside the JPEG variants (default true).</summary>
    public bool EnableWebP { get; set; } = true;

    /// <summary>WebP lossy quality 0-100 (default 75).</summary>
    public int WebPQuality { get; set; } = 75;

    /// <summary>Encode JPEG as progressive (default true).</summary>
    public bool EnableProgressiveJpeg { get; set; } = true;

    /// <summary>Allowed MIME types. Any type not in this list is rejected by <see cref="Validation.PhotoValidator"/>.</summary>
    public string[] AllowedMimeTypes { get; set; } =
        ["image/jpeg", "image/png", "image/webp"];
}
