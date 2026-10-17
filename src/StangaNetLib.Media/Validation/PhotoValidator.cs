using StangaNetLib.Core.Common;

namespace StangaNetLib.Media.Validation;

/// <summary>
/// Validates raw image bytes before processing.
/// Checks size limits, allowed MIME types, and magic-byte signatures to prevent
/// content-type spoofing and oversized uploads from reaching the processing pipeline.
/// </summary>
public static class PhotoValidator
{
    // Magic-byte signatures per MIME type.
    // Multiple signatures per type = multiple valid headers (e.g. different JPEG sub-types).
    private static readonly Dictionary<string, byte[][]> _signatures = new()
    {
        ["image/jpeg"] =
        [
            [0xFF, 0xD8, 0xFF],
        ],
        ["image/png"]  =
        [
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
        ],
        ["image/webp"] =
        [
            // RIFF header — WebP files start with "RIFF"
            [0x52, 0x49, 0x46, 0x46],
        ],
        ["image/gif"]  =
        [
            [0x47, 0x49, 0x46, 0x38, 0x37, 0x61],   // GIF87a
            [0x47, 0x49, 0x46, 0x38, 0x39, 0x61],   // GIF89a
        ],
    };

    /// <summary>
    /// Validates <paramref name="data"/> against size, MIME type, and magic-bytes.
    /// </summary>
    /// <param name="data">Raw image bytes.</param>
    /// <param name="fileName">Original filename (used only for the error message).</param>
    /// <param name="contentType">Declared MIME type from the client.</param>
    /// <param name="maxSizeBytes">Max allowed size in bytes.</param>
    /// <param name="allowedMimeTypes">Whitelist of accepted MIME types (case-insensitive).</param>
    /// <returns>
    /// <see cref="Result.Success()"/> when all checks pass; otherwise a failure <see cref="Result"/>
    /// with one of the error codes: <c>Photo.Empty</c>, <c>Photo.TooLarge</c>,
    /// <c>Photo.InvalidMimeType</c>, or <c>Photo.InvalidSignature</c>.
    /// </returns>
    public static Result Validate(
        byte[]   data,
        string   fileName,
        string   contentType,
        long     maxSizeBytes,
        string[] allowedMimeTypes)
    {
        if (data is null || data.Length == 0)
            return new Error("Photo.Empty", "Photo data cannot be empty.");

        if (data.Length > maxSizeBytes)
            return new Error(
                "Photo.TooLarge",
                $"Photo exceeds the maximum allowed size of {maxSizeBytes / 1024 / 1024} MB.");

        var mime = contentType.Trim().ToLowerInvariant();

        if (!allowedMimeTypes.Any(m => string.Equals(m, mime, StringComparison.OrdinalIgnoreCase)))
            return new Error(
                "Photo.InvalidMimeType",
                $"Content type '{contentType}' is not allowed.");

        if (!ValidateMagicBytes(data, mime))
            return new Error(
                "Photo.InvalidSignature",
                $"File '{fileName}' content does not match the declared content type '{contentType}'.");

        return Result.Success();
    }

    // ─── private ────────────────────────────────────────────────────────────

    private static bool ValidateMagicBytes(byte[] data, string mime)
    {
        if (!_signatures.TryGetValue(mime, out var signatures))
            return true;    // unknown MIME → no signature to check, allow through

        return signatures.Any(sig =>
            data.Length >= sig.Length &&
            data.AsSpan(0, sig.Length).SequenceEqual(sig));
    }
}
