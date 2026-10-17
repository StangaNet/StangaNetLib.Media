using FluentAssertions;
using Microsoft.Extensions.Options;
using StangaNetLib.Media.Models;
using StangaNetLib.Media.Settings;
using StangaNetLib.Media.Storage;

namespace StangaNetLib.Media.Tests;

/// <summary>
/// Integration-style tests for LocalPhotoStorageService using a temporary directory.
/// </summary>
public sealed class LocalPhotoStorageServiceTests : IAsyncLifetime
{
    private string                   _tempDir = null!;
    private LocalPhotoStorageService _sut     = null!;

    public ValueTask InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _sut = new LocalPhotoStorageService(Options.Create(new StorageSettings
        {
            Provider      = StorageProvider.Local,
            LocalBasePath = _tempDir,
            LocalBaseUrl  = "/uploads",
        }));

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        return ValueTask.CompletedTask;
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private static ImageVariants FakeVariants(bool includeWebP = false)
    {
        static ImageVariant Fake(int w, int h, string ct = "image/jpeg", string ext = ".jpg")
            => new(Data: new byte[8], ContentType: ct, Extension: ext,
                   Width: w, Height: h, SizeBytes: 8);

        return new ImageVariants
        {
            Thumbnail = Fake(150, 150),
            Medium    = Fake(400, 400),
            Large     = Fake(800, 800),
            Original  = Fake(1200, 900),
            WebP      = includeWebP ? Fake(400, 300, "image/webp", ".webp") : null,
        };
    }

    // ─── UploadVariantsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task UploadVariantsAsync_CreatesFilesOnDisk()
    {
        var result = await _sut.UploadVariantsAsync("user1", FakeVariants(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var dir   = Path.Combine(_tempDir, "user1");
        var files = Directory.GetFiles(dir);
        files.Length.Should().Be(4);    // thumb + medium + large + original (no WebP)
    }

    [Fact]
    public async Task UploadVariantsAsync_WithWebP_CreatesFiveFiles()
    {
        var result = await _sut.UploadVariantsAsync("user2", FakeVariants(includeWebP: true), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var dir   = Path.Combine(_tempDir, "user2");
        Directory.GetFiles(dir).Length.Should().Be(5);
    }

    [Fact]
    public async Task UploadVariantsAsync_ReturnsCorrectUrls()
    {
        var result = await _sut.UploadVariantsAsync("user3", FakeVariants(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var r = result.Value!;
        r.PhotoId.Should().NotBeEmpty();
        r.Urls.ThumbnailUrl.Should().StartWith("/uploads/user3/").And.Contain("_thumb");
        r.Urls.MediumUrl.Should().StartWith("/uploads/user3/").And.Contain("_medium");
        r.Urls.LargeUrl.Should().StartWith("/uploads/user3/").And.Contain("_large");
        r.Urls.OriginalUrl.Should().StartWith("/uploads/user3/").And.Contain("_original");
        r.Urls.WebPUrl.Should().BeNull();
    }

    [Fact]
    public async Task UploadVariantsAsync_TwoUploads_HaveDifferentPhotoIds()
    {
        var r1 = await _sut.UploadVariantsAsync("user4", FakeVariants(), TestContext.Current.CancellationToken);
        var r2 = await _sut.UploadVariantsAsync("user4", FakeVariants(), TestContext.Current.CancellationToken);

        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();
        r1.Value!.PhotoId.Should().NotBe(r2.Value!.PhotoId);
    }

    // ─── DeleteVariantsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteVariantsAsync_RemovesOnlyMatchingFiles()
    {
        var upload1 = await _sut.UploadVariantsAsync("user5", FakeVariants(), TestContext.Current.CancellationToken);
        var upload2 = await _sut.UploadVariantsAsync("user5", FakeVariants(), TestContext.Current.CancellationToken);

        await _sut.DeleteVariantsAsync("user5", upload1.Value!.PhotoId, TestContext.Current.CancellationToken);

        var dir            = Path.Combine(_tempDir, "user5");
        var remainingFiles = Directory.GetFiles(dir);

        // upload2 files should remain (4 files)
        remainingFiles.Length.Should().Be(4);
        remainingFiles.Should().AllSatisfy(f => f.Should().Contain(upload2.Value!.PhotoId));
    }

    [Fact]
    public async Task DeleteVariantsAsync_NonExistentPhotoId_ReturnsSuccess()
    {
        var result = await _sut.DeleteVariantsAsync("user6", "nonexistentphotoid", TestContext.Current.CancellationToken);
        result.IsSuccess.Should().BeTrue();
    }

    // ─── ExistsAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_AfterUpload_ReturnsTrue()
    {
        var upload = await _sut.UploadVariantsAsync("user7", FakeVariants(), TestContext.Current.CancellationToken);
        var exists = await _sut.ExistsAsync("user7", upload.Value!.PhotoId, TestContext.Current.CancellationToken);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_AfterDelete_ReturnsFalse()
    {
        var upload = await _sut.UploadVariantsAsync("user8", FakeVariants(), TestContext.Current.CancellationToken);
        await _sut.DeleteVariantsAsync("user8", upload.Value!.PhotoId, TestContext.Current.CancellationToken);
        var exists = await _sut.ExistsAsync("user8", upload.Value!.PhotoId, TestContext.Current.CancellationToken);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_UnknownId_ReturnsFalse()
    {
        var exists = await _sut.ExistsAsync("user9", "doesnotexist", TestContext.Current.CancellationToken);
        exists.Should().BeFalse();
    }
}
