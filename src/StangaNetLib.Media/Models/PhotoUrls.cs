namespace StangaNetLib.Media.Models;

/// <summary>
/// Public URLs for all variants of an uploaded photo.
/// Store these in the database; use <c>PhotoId</c> from
/// <see cref="PhotoUploadResult"/> to delete later.
/// </summary>
public sealed record PhotoUrls(
    string  ThumbnailUrl,
    string  MediumUrl,
    string  LargeUrl,
    string  OriginalUrl,
    string? WebPUrl);
