namespace StangaNetLib.Media.Settings;

/// <summary>
/// Azure Blob Storage connection settings. Bind to "AzureBlobStorage".
/// Only used when <see cref="StorageSettings.Provider"/> = <see cref="StorageProvider.AzureBlob"/>.
/// </summary>
public sealed class AzureBlobStorageSettings
{
    public const string SectionName = "AzureBlobStorage";

    /// <summary>Azure Storage connection string (required when <see cref="StorageProvider.AzureBlob"/> is active).</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Blob container name — created automatically on first use (default "photos").</summary>
    public string ContainerName    { get; set; } = "photos";

    /// <summary>
    /// Optional CDN/custom domain prefix.
    /// When null the Azure blob URI is used directly.
    /// Example: "https://cdn.example.com"
    /// </summary>
    public string? BaseUrl { get; set; }
}
