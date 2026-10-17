using FluentAssertions;
using StangaNetLib.Media.Models;
using StangaNetLib.Media.Storage;

namespace StangaNetLib.Media.Tests;

public sealed class NullPhotoStorageServiceTests
{
    private static readonly NullPhotoStorageService Sut = new();

    private static ImageVariants FakeVariants(bool includeWebP = false)
    {
        static ImageVariant Fake(string ct = "image/jpeg", string ext = ".jpg")
            => new(Data: new byte[4], ContentType: ct, Extension: ext, Width: 100, Height: 100, SizeBytes: 4);

        return new ImageVariants
        {
            Thumbnail = Fake(),
            Medium    = Fake(),
            Large     = Fake(),
            Original  = Fake(),
            WebP      = includeWebP ? Fake("image/webp", ".webp") : null,
        };
    }

    // ── UploadVariantsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UploadVariantsAsync_AlwaysReturnsSuccess()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await Sut.UploadVariantsAsync("owner1", FakeVariants(), ct);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UploadVariantsAsync_ReturnsNonEmptyPhotoId()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await Sut.UploadVariantsAsync("owner1", FakeVariants(), ct);

        result.Value!.PhotoId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UploadVariantsAsync_WithWebP_WebPUrlIsNotNull()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await Sut.UploadVariantsAsync("owner1", FakeVariants(includeWebP: true), ct);

        result.Value!.Urls.WebPUrl.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UploadVariantsAsync_WithoutWebP_WebPUrlIsNull()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await Sut.UploadVariantsAsync("owner1", FakeVariants(includeWebP: false), ct);

        result.Value!.Urls.WebPUrl.Should().BeNull();
    }

    [Fact]
    public async Task UploadVariantsAsync_TwoUploads_HaveDifferentPhotoIds()
    {
        var ct = TestContext.Current.CancellationToken;
        var r1 = await Sut.UploadVariantsAsync("owner1", FakeVariants(), ct);
        var r2 = await Sut.UploadVariantsAsync("owner1", FakeVariants(), ct);

        r1.Value!.PhotoId.Should().NotBe(r2.Value!.PhotoId);
    }

    // ── DeleteVariantsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteVariantsAsync_AlwaysReturnsSuccess()
    {
        var ct     = TestContext.Current.CancellationToken;
        var result = await Sut.DeleteVariantsAsync("owner1", "any-photo-id", ct);

        result.IsSuccess.Should().BeTrue();
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_AlwaysReturnsFalse()
    {
        var ct     = TestContext.Current.CancellationToken;
        var exists = await Sut.ExistsAsync("owner1", "any-photo-id", ct);

        exists.Should().BeFalse();
    }
}
