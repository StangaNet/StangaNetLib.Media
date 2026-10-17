namespace StangaNetLib.Media.Settings;

/// <summary>Chooses which storage backend is registered. Bind to "StorageSettings".</summary>
public sealed class StorageSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "StorageSettings";

    /// <summary>Storage backend to use (default <see cref="StorageProvider.Local"/>).</summary>
    public StorageProvider Provider      { get; set; } = StorageProvider.Local;

    /// <summary>Root directory for <see cref="StorageProvider.Local"/> (default "wwwroot/uploads/photos").</summary>
    public string LocalBasePath { get; set; } = "wwwroot/uploads/photos";

    /// <summary>URL prefix for local files served as static files (default "/uploads/photos").</summary>
    public string LocalBaseUrl  { get; set; } = "/uploads/photos";
}

/// <summary>Identifies the storage backend to register.</summary>
public enum StorageProvider
{
    /// <summary>Local filesystem — development only.</summary>
    Local    = 0,

    /// <summary>Azure Blob Storage — production.</summary>
    AzureBlob = 1,
}
