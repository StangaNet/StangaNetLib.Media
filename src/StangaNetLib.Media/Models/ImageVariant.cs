namespace StangaNetLib.Media.Models;

/// <summary>
/// A single processed image variant ready to be stored.
/// Contains the encoded bytes (<c>Data</c>), MIME type, file extension, dimensions, and byte size.
/// </summary>
public sealed record ImageVariant(
    byte[] Data,
    string ContentType,
    string Extension,
    int    Width,
    int    Height,
    long   SizeBytes);
