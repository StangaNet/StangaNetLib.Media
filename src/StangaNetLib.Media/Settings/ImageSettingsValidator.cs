using StangaNetLib.Core.Validators;

namespace StangaNetLib.Media.Settings;

/// <summary>
/// Validates <see cref="ImageSettings"/> on first options access.
/// Rejects out-of-range quality values and non-positive size/limit values.
/// </summary>
internal sealed class ImageSettingsValidator : SettingsValidatorBase<ImageSettings>
{
    /// <inheritdoc/>
    protected override void ValidateCore(ImageSettings options, List<string> errors)
    {
        if (options.ThumbnailSize <= 0)
            errors.Add($"{nameof(ImageSettings.ThumbnailSize)} must be greater than zero.");

        if (options.MediumSize <= 0)
            errors.Add($"{nameof(ImageSettings.MediumSize)} must be greater than zero.");

        if (options.LargeSize <= 0)
            errors.Add($"{nameof(ImageSettings.LargeSize)} must be greater than zero.");

        if (options.CompressionQuality is < 1 or > 100)
            errors.Add($"{nameof(ImageSettings.CompressionQuality)} must be between 1 and 100.");

        if (options.ThumbnailQuality is < 1 or > 100)
            errors.Add($"{nameof(ImageSettings.ThumbnailQuality)} must be between 1 and 100.");

        if (options.WebPQuality is < 1 or > 100)
            errors.Add($"{nameof(ImageSettings.WebPQuality)} must be between 1 and 100.");

        if (options.MaxUploadSizeBytes <= 0)
            errors.Add($"{nameof(ImageSettings.MaxUploadSizeBytes)} must be greater than zero.");
    }
}
