namespace StangaNetLib.Media.Settings;

/// <summary>
/// Settings for <see cref="Storage.StorageBackedPhotoStorageService"/>, which delegates
/// photo variant persistence to an <c>IBlobStorageService</c> from <c>StangaNetLib.Storage</c>.
/// </summary>
public sealed class StorageBackedPhotoStorageSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "StorageBackedPhotoStorage";

    /// <summary>Container (bucket / directory) name for photo variants. Default: <c>"photos"</c>.</summary>
    public string ContainerName { get; set; } = "photos";

    /// <summary>
    /// Base URL prepended to blob keys when constructing public photo URLs.
    /// Example: <c>"https://cdn.example.com"</c>. Leave empty to return relative paths.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}
