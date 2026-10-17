using FluentAssertions;
using StangaNetLib.Media.Validation;

namespace StangaNetLib.Media.Tests;

public sealed class PhotoValidatorEdgeCaseTests
{
    private static readonly string[] AllowedMimes = ["image/jpeg", "image/png", "image/webp"];
    private const           long     MaxSize       = 5 * 1024 * 1024; // 5 MB

    private static byte[] JpegBytes(int length = 200)
    {
        var data = new byte[length];
        data[0] = 0xFF; data[1] = 0xD8; data[2] = 0xFF;
        return data;
    }

    private static byte[] GifBytes89a(int length = 100)
    {
        var data = new byte[length];
        // GIF89a magic bytes
        data[0] = 0x47; data[1] = 0x49; data[2] = 0x46;
        data[3] = 0x38; data[4] = 0x39; data[5] = 0x61;
        return data;
    }

    // ── Size boundary ─────────────────────────────────────────────────────

    [Fact]
    public void Validate_ExactlyAtMaxSize_ReturnsSuccess()
    {
        var data   = JpegBytes((int)MaxSize);
        var result = PhotoValidator.Validate(data, "photo.jpg", "image/jpeg", MaxSize, AllowedMimes);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_OneBytePastMaxSize_ReturnsTooLarge()
    {
        var data   = JpegBytes((int)MaxSize + 1);
        var result = PhotoValidator.Validate(data, "photo.jpg", "image/jpeg", MaxSize, AllowedMimes);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Photo.TooLarge");
    }

    // ── GIF format ────────────────────────────────────────────────────────

    [Fact]
    public void Validate_GifBytes_WhenGifIsAllowed_ReturnsSuccess()
    {
        var gifMimes = new[] { "image/gif" };

        var result = PhotoValidator.Validate(GifBytes89a(), "anim.gif", "image/gif", MaxSize, gifMimes);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_GifMimeType_WhenNotInAllowedList_ReturnsInvalidMimeType()
    {
        var result = PhotoValidator.Validate(GifBytes89a(), "anim.gif", "image/gif", MaxSize, AllowedMimes);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Photo.InvalidMimeType");
    }

    [Fact]
    public void Validate_ClaimedJpegButGifContent_ReturnsInvalidSignature()
    {
        var result = PhotoValidator.Validate(GifBytes89a(), "photo.jpg", "image/jpeg", MaxSize, AllowedMimes);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Photo.InvalidSignature");
    }

    // ── Unknown MIME skips magic-byte check ───────────────────────────────

    [Fact]
    public void Validate_UnknownMime_InAllowedList_SkipsMagicByteCheck()
    {
        // "image/avif" has no registered signature → should pass magic check by default
        var avifMimes = new[] { "image/avif" };
        var data      = new byte[100]; // arbitrary bytes, no known signature

        var result = PhotoValidator.Validate(data, "photo.avif", "image/avif", MaxSize, avifMimes);

        result.IsSuccess.Should().BeTrue("unknown MIME types have no signature to verify");
    }

    // ── Content-type whitespace normalization ─────────────────────────────

    [Fact]
    public void Validate_ContentTypeWithLeadingWhitespace_IsNormalized()
    {
        var result = PhotoValidator.Validate(JpegBytes(), "photo.jpg", "  image/jpeg  ", MaxSize, AllowedMimes);

        result.IsSuccess.Should().BeTrue("content type is trimmed before comparison");
    }
}
