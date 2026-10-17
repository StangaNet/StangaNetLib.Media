using FluentAssertions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using StangaNetLib.Media.Services;
using StangaNetLib.Media.Settings;

namespace StangaNetLib.Media.Tests;

public sealed class ImageOptimizationEdgeCaseTests
{
    private static ImageOptimizationService BuildSut(ImageSettings? settings = null)
        => new(Options.Create(settings ?? new ImageSettings()));

    private static byte[] CreateTestJpeg(int width = 600, int height = 400)
    {
        using var image = new Image<Rgb24>(width, height);
        using var ms    = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    private static byte[] CreateTestPng(int width = 600, int height = 400)
    {
        using var image = new Image<Rgba32>(width, height);
        using var ms    = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    // ── Variant data correctness ──────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_AllJpegVariants_HaveNonEmptyBytes()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await BuildSut().ProcessAsync(CreateTestJpeg(), ct);

        result.IsSuccess.Should().BeTrue();
        var v = result.Value!;
        v.Thumbnail.Data.Should().NotBeEmpty();
        v.Medium.Data.Should().NotBeEmpty();
        v.Large.Data.Should().NotBeEmpty();
        v.Original.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_WebPVariant_StartsWithRiffMagicBytes()
    {
        var ct     = TestContext.Current.CancellationToken;
        var sut    = BuildSut(new ImageSettings { EnableWebP = true, WebPQuality = 75 });
        var result = await sut.ProcessAsync(CreateTestJpeg(), ct);

        result.IsSuccess.Should().BeTrue();
        var webp = result.Value!.WebP!;

        // WebP files begin with the "RIFF" header (ASCII 0x52 0x49 0x46 0x46)
        webp.Data[0].Should().Be(0x52); // R
        webp.Data[1].Should().Be(0x49); // I
        webp.Data[2].Should().Be(0x46); // F
        webp.Data[3].Should().Be(0x46); // F
    }

    // ── PNG input → JPEG output ───────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_PngInput_AllVariantsAreJpeg()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await BuildSut().ProcessAsync(CreateTestPng(), ct);

        result.IsSuccess.Should().BeTrue();
        var v = result.Value!;
        v.Thumbnail.ContentType.Should().Be("image/jpeg");
        v.Medium.ContentType.Should().Be("image/jpeg");
        v.Large.ContentType.Should().Be("image/jpeg");
        v.Original.ContentType.Should().Be("image/jpeg");
    }

    // ── ConvertToWebPAsync edge cases ─────────────────────────────────────

    [Fact]
    public async Task ConvertToWebPAsync_WithMaxDimension_OutputFitsWithinBounds()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await BuildSut().ConvertToWebPAsync(
            CreateTestJpeg(800, 600), quality: 75, maxDimension: 200, ct: ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Width.Should().BeLessThanOrEqualTo(200);
        result.Value!.Height.Should().BeLessThanOrEqualTo(200);
    }

    [Fact]
    public async Task ConvertToWebPAsync_ZeroMaxDimension_PreservesOriginalSize()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await BuildSut().ConvertToWebPAsync(
            CreateTestJpeg(300, 200), quality: 75, maxDimension: 0, ct: ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Width.Should().Be(300);
        result.Value!.Height.Should().Be(200);
    }

    [Fact]
    public async Task ConvertToWebPAsync_InvalidBytes_ReturnsProcessingFailure()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await BuildSut().ConvertToWebPAsync(
            [0xDE, 0xAD, 0xBE, 0xEF], quality: 75, ct: ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Media.ProcessingFailed");
    }

    // ── ResizeAsync edge cases ────────────────────────────────────────────

    [Fact]
    public async Task ResizeAsync_ImageSmallerThanMaxDimension_PreservesSize()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await BuildSut().ResizeAsync(
            CreateTestJpeg(100, 80), maxDimension: 500, quality: 80, ct: ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Width.Should().Be(100);
        result.Value!.Height.Should().Be(80);
    }

    [Fact]
    public async Task ResizeAsync_InvalidBytes_ReturnsProcessingFailure()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await BuildSut().ResizeAsync(
            [0x00, 0x01], maxDimension: 100, quality: 80, ct: ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Media.ProcessingFailed");
    }
}
