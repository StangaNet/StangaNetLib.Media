using Microsoft.Extensions.Options;
using StangaNetLib.Core.Common;
using StangaNetLib.Media.Abstractions;
using StangaNetLib.Media.Models;
using StangaNetLib.Media.Settings;
using StangaNetLib.Storage.Abstractions;

namespace StangaNetLib.Media.Storage;

/// <summary>
/// Persists photo variants to a configurable blob store via <see cref="IBlobStorageService"/>.
/// Blob keys follow the pattern <c>{ownerId}/{photoId}_{suffix}{extension}</c>.
/// </summary>
internal sealed class StorageBackedPhotoStorageService(
    IBlobStorageService                         blobStorage,
    IOptions<StorageBackedPhotoStorageSettings> options) : IPhotoStorageService
{
    private readonly StorageBackedPhotoStorageSettings _settings = options.Value;

    /// <inheritdoc/>
    public async Task<Result<PhotoUploadResult>> UploadVariantsAsync(
        string            ownerId,
        ImageVariants     variants,
        CancellationToken ct = default)
    {
        var photoId   = Guid.NewGuid().ToString("N");
        var container = _settings.ContainerName;
        var baseUrl   = _settings.BaseUrl.TrimEnd('/');

        string MakeUrl(string blobKey)
            => baseUrl.Length > 0 ? $"{baseUrl}/{blobKey}" : $"/{blobKey}";

        async Task<Result<string>> UploadOneAsync(ImageVariant v, string suffix)
        {
            var blobKey = $"{ownerId}/{photoId}_{suffix}{v.Extension}";
            using var stream = new MemoryStream(v.Data);
            var r = await blobStorage.UploadAsync(container, blobKey, stream, v.ContentType, ct: ct);
            return r.IsSuccess
                ? Result<string>.Success(MakeUrl(blobKey))
                : new Error("Storage.UploadFailed", r.Error.Description);
        }

        var t1 = UploadOneAsync(variants.Thumbnail, "thumb");
        var t2 = UploadOneAsync(variants.Medium,    "medium");
        var t3 = UploadOneAsync(variants.Large,     "large");
        var t4 = UploadOneAsync(variants.Original,  "original");
        var t5 = variants.WebP is not null ? UploadOneAsync(variants.WebP, "webp") : null;

        if (t5 is null)
            await Task.WhenAll(t1, t2, t3, t4);
        else
            await Task.WhenAll(t1, t2, t3, t4, t5);

        var r1 = t1.Result; if (!r1.IsSuccess) return r1.Error;
        var r2 = t2.Result; if (!r2.IsSuccess) return r2.Error;
        var r3 = t3.Result; if (!r3.IsSuccess) return r3.Error;
        var r4 = t4.Result; if (!r4.IsSuccess) return r4.Error;

        string? webpUrl = null;
        if (t5 is not null)
        {
            var r5 = t5.Result;
            if (!r5.IsSuccess) return r5.Error;
            webpUrl = r5.Value;
        }

        return Result<PhotoUploadResult>.Success(new PhotoUploadResult(
            photoId,
            new PhotoUrls(r1.Value!, r2.Value!, r3.Value!, r4.Value!, webpUrl)));
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteVariantsAsync(
        string            ownerId,
        string            photoId,
        CancellationToken ct = default)
    {
        var prefix     = $"{ownerId}/{photoId}";
        var listResult = await blobStorage.ListBlobsAsync(_settings.ContainerName, prefix, ct);
        if (!listResult.IsSuccess)
            return new Error("Storage.DeleteFailed", listResult.Error.Description);

        foreach (var blob in listResult.Value!)
        {
            var del = await blobStorage.DeleteAsync(_settings.ContainerName, blob.BlobKey, ct);
            if (!del.IsSuccess)
                return new Error("Storage.DeleteFailed", del.Error.Description);
        }

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(
        string            ownerId,
        string            photoId,
        CancellationToken ct = default)
    {
        var prefix     = $"{ownerId}/{photoId}";
        var listResult = await blobStorage.ListBlobsAsync(_settings.ContainerName, prefix, ct);
        return listResult.IsSuccess && listResult.Value!.Count > 0;
    }
}
