namespace StangaNetLib.Media.Models;

/// <summary>
/// Result returned by <see cref="Abstractions.IPhotoStorageService.UploadVariantsAsync"/>.
/// Store <see cref="PhotoId"/> alongside the URLs — you will need it to call
/// <see cref="Abstractions.IPhotoStorageService.DeleteVariantsAsync"/> later.
/// </summary>
public sealed record PhotoUploadResult(
    string    PhotoId,
    PhotoUrls Urls);
