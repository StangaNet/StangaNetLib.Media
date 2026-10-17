using FluentAssertions;
using StangaNetLib.Media.Validation;

namespace StangaNetLib.Media.Tests;

public sealed class PhotoValidatorTests
{
    private static readonly string[] AllowedMimes  = ["image/jpeg", "image/png", "image/webp"];
    private const           long     MaxSize        = 5 * 1024 * 1024; // 5 MB

    // ─── helpers ────────────────────────────────────────────────────────────

    private static byte[] JpegBytes(int length = 200)
    {
        var data = new byte[length];
        data[0] = 0xFF; data[1] = 0xD8; data[2] = 0xFF;
        return data;
    }

    private static byte[] PngBytes(int length = 200)
    {
        var data = new byte[length];
        byte[] sig = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        sig.CopyTo(data, 0);
        return data;
    }

    private static byte[] WebPBytes(int length = 200)
    {
        var data = new byte[length];
        // RIFF header
        data[0] = 0x52; data[1] = 0x49; data[2] = 0x46; data[3] = 0x46;
        return data;
    }

    // ─── success cases ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidJpeg_ReturnsSuccess()
    {
        var result = PhotoValidator.Validate(JpegBytes(), "photo.jpg", "image/jpeg", MaxSize, AllowedMimes);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidPng_ReturnsSuccess()
    {
        var result = PhotoValidator.Validate(PngBytes(), "photo.png", "image/png", MaxSize, AllowedMimes);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidWebP_ReturnsSuccess()
    {
        var result = PhotoValidator.Validate(WebPBytes(), "photo.webp", "image/webp", MaxSize, AllowedMimes);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_ContentType_CaseInsensitive()
    {
        var result = PhotoValidator.Validate(JpegBytes(), "photo.jpg", "IMAGE/JPEG", MaxSize, AllowedMimes);
        result.IsSuccess.Should().BeTrue();
    }

    // ─── failure cases ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyArray_ReturnsPhotoEmpty()
    {
        var result = PhotoValidator.Validate([], "photo.jpg", "image/jpeg", MaxSize, AllowedMimes);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Photo.Empty");
    }

    [Fact]
    public void Validate_NullArray_ReturnsPhotoEmpty()
    {
        var result = PhotoValidator.Validate(null!, "photo.jpg", "image/jpeg", MaxSize, AllowedMimes);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Photo.Empty");
    }

    [Fact]
    public void Validate_ExceedsMaxSize_ReturnsPhotoTooLarge()
    {
        var big    = JpegBytes(6 * 1024 * 1024);
        var result = PhotoValidator.Validate(big, "big.jpg", "image/jpeg", MaxSize, AllowedMimes);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Photo.TooLarge");
    }

    [Fact]
    public void Validate_DisallowedMimeType_ReturnsInvalidMimeType()
    {
        var result = PhotoValidator.Validate(JpegBytes(), "photo.bmp", "image/bmp", MaxSize, AllowedMimes);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Photo.InvalidMimeType");
    }

    [Fact]
    public void Validate_MismatchedMagicBytes_ReturnsInvalidSignature()
    {
        // Claim JPEG but provide PNG bytes
        var result = PhotoValidator.Validate(PngBytes(), "photo.jpg", "image/jpeg", MaxSize, AllowedMimes);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Photo.InvalidSignature");
    }

    [Fact]
    public void Validate_JpegClaimedButWebPBytes_ReturnsInvalidSignature()
    {
        var result = PhotoValidator.Validate(WebPBytes(), "photo.jpg", "image/jpeg", MaxSize, AllowedMimes);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Photo.InvalidSignature");
    }
}
