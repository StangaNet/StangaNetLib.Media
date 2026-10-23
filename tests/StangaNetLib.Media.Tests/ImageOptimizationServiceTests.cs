using FluentAssertions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using StangaNetLib.Media.Services;
using StangaNetLib.Media.Settings;

namespace StangaNetLib.Media.Tests;

public sealed class ImageOptimizationServiceTests
{
    private readonly ImageOptimizationService _sut;

    public ImageOptimizationServiceTests()
    {
        _sut = BuildSut(new ImageSettings
        {
            ThumbnailSize          = 150,
            MediumSize             = 400,
            LargeSize              = 800,
            CompressionQuality     = 80,
            ThumbnailQuality       = 75,
            EnableWebP             = true,
            WebPQuality            = 75,
            EnableProgressiveJpeg  = true,
        });
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private static ImageOptimizationService BuildSut(ImageSettings settings)
        => new(Options.Create(settings));

    /// <summary>Creates a valid JPEG in memory using ImageSharp (no external files needed).</summary>
    private static byte[] CreateTestJpeg(int width = 1200, int height = 900)
    {
        using var image = new Image<Rgb24>(width, height);
        using var ms    = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    // ─── ProcessAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_ValidImage_ReturnsSuccess()
    {
        var result = await _sut.ProcessAsync(CreateTestJpeg(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_LargeImage_DownscalesAllVariants()
    {
        var result = await _sut.ProcessAsync(CreateTestJpeg(1200, 900), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var v = result.Value!;
        v.Thumbnail.Width.Should().BeLessThanOrEqualTo(150);
        v.Medium.Width.Should().BeLessThanOrEqualTo(400);
        v.Large.Width.Should().BeLessThanOrEqualTo(800);
        v.Original.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_SmallImage_DoesNotUpscale()
    {
        // 100×75 is smaller than all thresholds — widths must stay at 100 or less
        var result = await _sut.ProcessAsync(CreateTestJpeg(100, 75), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Thumbnail.Width.Should().BeLessThanOrEqualTo(100);
        result.Value!.Medium.Width.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task ProcessAsync_WebPEnabled_GeneratesWebPVariant()
    {
        var result = await _sut.ProcessAsync(CreateTestJpeg(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WebP.Should().NotBeNull();
        result.Value!.WebP!.ContentType.Should().Be("image/webp");
        result.Value!.WebP.Extension.Should().Be(".webp");
    }

    [Fact]
    public async Task ProcessAsync_WebPDisabled_WebPVariantIsNull()
    {
        var sut    = BuildSut(new ImageSettings { EnableWebP = false });
        var result = await sut.ProcessAsync(CreateTestJpeg(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WebP.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_AllVariants_AreJpegExceptWebP()
    {
        var result = await _sut.ProcessAsync(CreateTestJpeg(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var v = result.Value!;
        v.Thumbnail.ContentType.Should().Be("image/jpeg");
        v.Medium.ContentType.Should().Be("image/jpeg");
        v.Large.ContentType.Should().Be("image/jpeg");
        v.Original.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task ProcessAsync_InvalidBytes_ReturnsFailure()
    {
        var result = await _sut.ProcessAsync([0x00, 0x01, 0x02, 0x03], TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Photo.InvalidMimeType");
    }

    // ─── ResizeAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ResizeAsync_LargeImage_FitsWithinMaxDimension()
    {
        var result = await _sut.ResizeAsync(CreateTestJpeg(1200, 900), maxDimension: 300, quality: 80, ct: TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Width.Should().BeLessThanOrEqualTo(300);
        result.Value!.Height.Should().BeLessThanOrEqualTo(300);
    }

    // ─── ConvertToWebPAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ConvertToWebPAsync_ValidJpeg_ReturnsWebP()
    {
        var result = await _sut.ConvertToWebPAsync(CreateTestJpeg(), quality: 75, ct: TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be("image/webp");
        result.Value!.Data.Should().NotBeEmpty();
    }
}
