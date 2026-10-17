using FluentAssertions;
using Microsoft.Extensions.Options;
using StangaNetLib.Core.Common;
using StangaNetLib.Media.Models;
using StangaNetLib.Media.Settings;
using StangaNetLib.Media.Storage;
using StangaNetLib.Storage.Abstractions;
using StangaNetLib.Storage.Models;

namespace StangaNetLib.Media.Tests;

public sealed class StorageBackedPhotoStorageServiceTests
{
    private readonly FakeBlobStorageService        _fake;
    private readonly StorageBackedPhotoStorageService _sut;

    public StorageBackedPhotoStorageServiceTests()
    {
        _fake = new FakeBlobStorageService();
        _sut  = new StorageBackedPhotoStorageService(
            _fake,
            Options.Create(new StorageBackedPhotoStorageSettings
            {
                ContainerName = "photos",
                BaseUrl       = "https://cdn.example.com",
            }));
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
    public async Task UploadVariantsAsync_UploadsAllVariants()
    {
        var result = await _sut.UploadVariantsAsync("user1", FakeVariants(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _fake.Blobs.Should().HaveCount(4);
    }

    [Fact]
    public async Task UploadVariantsAsync_WithWebP_UploadsFiveVariants()
    {
        var result = await _sut.UploadVariantsAsync("user2", FakeVariants(includeWebP: true), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _fake.Blobs.Should().HaveCount(5);
    }

    [Fact]
    public async Task UploadVariantsAsync_ReturnsPhotoIdAndUrls()
    {
        var result = await _sut.UploadVariantsAsync("user3", FakeVariants(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var r = result.Value!;
        r.PhotoId.Should().NotBeNullOrEmpty();
        r.Urls.ThumbnailUrl.Should().StartWith("https://cdn.example.com/user3/").And.Contain("_thumb");
        r.Urls.MediumUrl.Should().StartWith("https://cdn.example.com/user3/").And.Contain("_medium");
        r.Urls.LargeUrl.Should().StartWith("https://cdn.example.com/user3/").And.Contain("_large");
        r.Urls.OriginalUrl.Should().StartWith("https://cdn.example.com/user3/").And.Contain("_original");
        r.Urls.WebPUrl.Should().BeNull();
    }

    [Fact]
    public async Task UploadVariantsAsync_WithWebP_ReturnsWebPUrl()
    {
        var result = await _sut.UploadVariantsAsync("user4", FakeVariants(includeWebP: true), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Urls.WebPUrl.Should().NotBeNullOrEmpty()
              .And.Contain("_webp").And.Contain(".webp");
    }

    [Fact]
    public async Task UploadVariantsAsync_TwoUploads_HaveDifferentPhotoIds()
    {
        var r1 = await _sut.UploadVariantsAsync("user5", FakeVariants(), TestContext.Current.CancellationToken);
        var r2 = await _sut.UploadVariantsAsync("user5", FakeVariants(), TestContext.Current.CancellationToken);

        r1.Value!.PhotoId.Should().NotBe(r2.Value!.PhotoId);
    }

    [Fact]
    public async Task UploadVariantsAsync_BlobKeysIncludeOwnerId()
    {
        await _sut.UploadVariantsAsync("owner42", FakeVariants(), TestContext.Current.CancellationToken);

        _fake.Blobs.Keys.Should().AllSatisfy(k => k.Should().StartWith("owner42/"));
    }

    [Fact]
    public async Task UploadVariantsAsync_WhenBlobUploadFails_ReturnsFailure()
    {
        _fake.FailOnUpload = true;

        var result = await _sut.UploadVariantsAsync("user6", FakeVariants(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Storage.UploadFailed");
    }

    [Fact]
    public async Task UploadVariantsAsync_WithEmptyBaseUrl_ReturnsRelativePaths()
    {
        var sut = new StorageBackedPhotoStorageService(
            _fake,
            Options.Create(new StorageBackedPhotoStorageSettings
            {
                ContainerName = "photos",
                BaseUrl       = string.Empty,
            }));

        var result = await sut.UploadVariantsAsync("user7", FakeVariants(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Urls.ThumbnailUrl.Should().StartWith("/user7/");
    }

    [Fact]
    public async Task UploadVariantsAsync_UrlsContainPhotoId()
    {
        var result = await _sut.UploadVariantsAsync("user8", FakeVariants(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var photoId = result.Value!.PhotoId;
        result.Value.Urls.ThumbnailUrl.Should().Contain(photoId);
        result.Value.Urls.MediumUrl.Should().Contain(photoId);
        result.Value.Urls.LargeUrl.Should().Contain(photoId);
        result.Value.Urls.OriginalUrl.Should().Contain(photoId);
    }

    [Fact]
    public async Task UploadVariantsAsync_ContentTypeIsPropagatedToBlob()
    {
        await _sut.UploadVariantsAsync("user9", FakeVariants(), TestContext.Current.CancellationToken);

        _fake.Blobs.Values.Where(b => b.BlobKey.Contains("_thumb"))
             .Should().AllSatisfy(b => b.ContentType.Should().Be("image/jpeg"));
    }

    // ─── DeleteVariantsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteVariantsAsync_RemovesAllMatchingBlobs()
    {
        var upload = await _sut.UploadVariantsAsync("user10", FakeVariants(), TestContext.Current.CancellationToken);

        var del = await _sut.DeleteVariantsAsync("user10", upload.Value!.PhotoId, TestContext.Current.CancellationToken);

        del.IsSuccess.Should().BeTrue();
        _fake.Blobs.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteVariantsAsync_DoesNotRemoveOtherOwnerBlobs()
    {
        var u1 = await _sut.UploadVariantsAsync("owner1", FakeVariants(), TestContext.Current.CancellationToken);
        await _sut.UploadVariantsAsync("owner2", FakeVariants(), TestContext.Current.CancellationToken);

        await _sut.DeleteVariantsAsync("owner1", u1.Value!.PhotoId, TestContext.Current.CancellationToken);

        _fake.Blobs.Keys.Should().AllSatisfy(k => k.Should().StartWith("owner2/"));
        _fake.Blobs.Should().HaveCount(4);
    }

    [Fact]
    public async Task DeleteVariantsAsync_NonExistentPhotoId_ReturnsSuccess()
    {
        var result = await _sut.DeleteVariantsAsync("user11", "doesnotexist", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteVariantsAsync_NonExistentOwner_ReturnsSuccess()
    {
        var result = await _sut.DeleteVariantsAsync("ghostowner", "ghostphoto", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteVariantsAsync_WhenListFails_ReturnsDeleteFailed()
    {
        _fake.FailOnList = true;

        var result = await _sut.DeleteVariantsAsync("user12", "anyid", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Storage.DeleteFailed");
    }

    [Fact]
    public async Task DeleteVariantsAsync_WhenDeleteFails_ReturnsDeleteFailed()
    {
        var upload = await _sut.UploadVariantsAsync("user13", FakeVariants(), TestContext.Current.CancellationToken);
        _fake.FailOnDelete = true;

        var result = await _sut.DeleteVariantsAsync("user13", upload.Value!.PhotoId, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Storage.DeleteFailed");
    }

    // ─── ExistsAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_AfterUpload_ReturnsTrue()
    {
        var upload = await _sut.UploadVariantsAsync("user14", FakeVariants(), TestContext.Current.CancellationToken);

        var exists = await _sut.ExistsAsync("user14", upload.Value!.PhotoId, TestContext.Current.CancellationToken);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_AfterDelete_ReturnsFalse()
    {
        var upload = await _sut.UploadVariantsAsync("user15", FakeVariants(), TestContext.Current.CancellationToken);
        await _sut.DeleteVariantsAsync("user15", upload.Value!.PhotoId, TestContext.Current.CancellationToken);

        var exists = await _sut.ExistsAsync("user15", upload.Value!.PhotoId, TestContext.Current.CancellationToken);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_UnknownId_ReturnsFalse()
    {
        var exists = await _sut.ExistsAsync("user16", "doesnotexist", TestContext.Current.CancellationToken);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WrongOwner_ReturnsFalse()
    {
        var upload = await _sut.UploadVariantsAsync("ownerA", FakeVariants(), TestContext.Current.CancellationToken);

        var exists = await _sut.ExistsAsync("ownerB", upload.Value!.PhotoId, TestContext.Current.CancellationToken);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WhenListFails_ReturnsFalse()
    {
        _fake.FailOnList = true;

        var exists = await _sut.ExistsAsync("user17", "someid", TestContext.Current.CancellationToken);

        exists.Should().BeFalse();
    }
}

// ─── fake ────────────────────────────────────────────────────────────────────

internal sealed class FakeBlobStorageService : IBlobStorageService
{
    public record BlobEntry(string BlobKey, string ContentType);

    public Dictionary<string, BlobEntry> Blobs { get; } = new();
    public bool FailOnUpload  { get; set; }
    public bool FailOnDelete  { get; set; }
    public bool FailOnList    { get; set; }

    public Task<Result<BlobUploadResult>> UploadAsync(
        string containerName, string blobKey, Stream content, string contentType,
        IReadOnlyDictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        if (FailOnUpload)
            return Task.FromResult(Result<BlobUploadResult>.Failure(
                new Error("Storage.Error", "Simulated upload failure.")));

        Blobs[blobKey] = new BlobEntry(blobKey, contentType);
        return Task.FromResult(Result<BlobUploadResult>.Success(
            new BlobUploadResult(containerName, blobKey, contentType, "etag", 8)));
    }

    public Task<Result<BlobDownloadResult>> DownloadAsync(
        string containerName, string blobKey, CancellationToken ct = default)
    {
        if (!Blobs.TryGetValue(blobKey, out var entry))
            return Task.FromResult(Result<BlobDownloadResult>.Failure(
                Error.NotFound("Blob", blobKey)));

        return Task.FromResult(Result<BlobDownloadResult>.Success(
            new BlobDownloadResult(new MemoryStream(new byte[8]), entry.ContentType, 8,
                new Dictionary<string, string>())));
    }

    public Task<Result> DeleteAsync(
        string containerName, string blobKey, CancellationToken ct = default)
    {
        if (FailOnDelete)
            return Task.FromResult(Result.Failure(
                new Error("Storage.Error", "Simulated delete failure.")));

        Blobs.Remove(blobKey);
        return Task.FromResult(Result.Success());
    }

    public Task<bool> ExistsAsync(
        string containerName, string blobKey, CancellationToken ct = default)
        => Task.FromResult(Blobs.ContainsKey(blobKey));

    public Task<Result<BlobMetadata>> GetMetadataAsync(
        string containerName, string blobKey, CancellationToken ct = default)
    {
        if (!Blobs.TryGetValue(blobKey, out var entry))
            return Task.FromResult(Result<BlobMetadata>.Failure(Error.NotFound("Blob", blobKey)));

        return Task.FromResult(Result<BlobMetadata>.Success(
            new BlobMetadata(containerName, blobKey, entry.ContentType, 8, DateTimeOffset.UtcNow,
                new Dictionary<string, string>())));
    }

    public Task<Result<IReadOnlyList<BlobItem>>> ListBlobsAsync(
        string containerName, string? prefix = null, CancellationToken ct = default)
    {
        if (FailOnList)
            return Task.FromResult(Result<IReadOnlyList<BlobItem>>.Failure(
                new Error("Storage.Error", "Simulated list failure.")));

        IReadOnlyList<BlobItem> items = Blobs.Keys
            .Where(k => prefix is null || k.StartsWith(prefix, StringComparison.Ordinal))
            .Select(k => new BlobItem(containerName, k, 8, DateTimeOffset.UtcNow))
            .ToList()
            .AsReadOnly();

        return Task.FromResult(Result<IReadOnlyList<BlobItem>>.Success(items));
    }

    public Task<Result> CopyAsync(
        string sourceContainer, string sourceBlobKey,
        string destContainer,   string destBlobKey,
        CancellationToken ct = default)
    {
        if (!Blobs.TryGetValue(sourceBlobKey, out var entry))
            return Task.FromResult(Result.Failure(Error.NotFound("Blob", sourceBlobKey)));

        Blobs[destBlobKey] = entry with { BlobKey = destBlobKey };
        return Task.FromResult(Result.Success());
    }

    public Task<Result> MoveAsync(
        string sourceContainer, string sourceBlobKey,
        string destContainer,   string destBlobKey,
        CancellationToken ct = default)
    {
        if (!Blobs.TryGetValue(sourceBlobKey, out var entry))
            return Task.FromResult(Result.Failure(Error.NotFound("Blob", sourceBlobKey)));

        Blobs.Remove(sourceBlobKey);
        Blobs[destBlobKey] = entry with { BlobKey = destBlobKey };
        return Task.FromResult(Result.Success());
    }
}
