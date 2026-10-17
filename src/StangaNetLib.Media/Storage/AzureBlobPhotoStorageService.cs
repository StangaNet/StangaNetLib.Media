using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using StangaNetLib.Core.Common;
using StangaNetLib.Media.Abstractions;
using StangaNetLib.Media.Models;
using StangaNetLib.Media.Settings;

namespace StangaNetLib.Media.Storage;

/// <summary>
/// Stores variants in Azure Blob Storage. Suitable for production multi-instance deployments.
/// The container is created automatically on first use with <c>PublicAccessType.Blob</c>.
/// </summary>
internal sealed class AzureBlobPhotoStorageService : IPhotoStorageService
{
    private readonly AzureBlobStorageSettings  _settings;
    private readonly Lazy<BlobContainerClient> _container;
    // Avoids repeated CreateIfNotExistsAsync round-trips after the container is confirmed ready.
    private volatile bool _containerReady;

    public AzureBlobPhotoStorageService(IOptions<AzureBlobStorageSettings> options)
    {
        _settings  = options.Value;
        _container = new Lazy<BlobContainerClient>(() =>
            new BlobContainerClient(_settings.ConnectionString, _settings.ContainerName));
    }

    public async Task<Result<PhotoUploadResult>> UploadVariantsAsync(
        string            ownerId,
        ImageVariants     variants,
        CancellationToken ct = default)
    {
        try
        {
            await EnsureContainerExistsAsync(ct);
            var photoId = Guid.NewGuid().ToString("N");

            Task<string> Upload(ImageVariant v, string suffix)
                => UploadBlobCoreAsync(v, $"{ownerId}/{photoId}{suffix}{v.Extension}", ct);

            var thumbTask    = Upload(variants.Thumbnail, "_thumb");
            var mediumTask   = Upload(variants.Medium,    "_medium");
            var largeTask    = Upload(variants.Large,     "_large");
            var originalTask = Upload(variants.Original,  "_original");
            var webpTask     = variants.WebP is not null ? Upload(variants.WebP, "_webp") : null;

            var taskList = new List<Task<string>> { thumbTask, mediumTask, largeTask, originalTask };
            if (webpTask is not null) taskList.Add(webpTask);
            await Task.WhenAll(taskList);

            var urls = new PhotoUrls(
                await thumbTask,
                await mediumTask,
                await largeTask,
                await originalTask,
                webpTask is not null ? await webpTask : null);

            return Result<PhotoUploadResult>.Success(new PhotoUploadResult(photoId, urls));
        }
        catch (Exception ex)
        {
            return new Error("Storage.UploadFailed", ex.Message);
        }
    }

    public async Task<Result> DeleteVariantsAsync(
        string            ownerId,
        string            photoId,
        CancellationToken ct = default)
    {
        try
        {
            await EnsureContainerExistsAsync(ct);

            await foreach (var item in _container.Value.GetBlobsAsync(
                               traits: BlobTraits.None,
                               states: BlobStates.None,
                               prefix: $"{ownerId}/{photoId}",
                               cancellationToken: ct))
            {
                await _container.Value
                                .GetBlobClient(item.Name)
                                .DeleteIfExistsAsync(cancellationToken: ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error("Storage.DeleteFailed", ex.Message));
        }
    }

    public async Task<bool> ExistsAsync(
        string            ownerId,
        string            photoId,
        CancellationToken ct = default)
    {
        await EnsureContainerExistsAsync(ct);

        await foreach (var _ in _container.Value.GetBlobsAsync(
                           traits: BlobTraits.None,
                           states: BlobStates.None,
                           prefix: $"{ownerId}/{photoId}",
                           cancellationToken: ct))
        {
            return true;
        }

        return false;
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private async Task<string> UploadBlobCoreAsync(
        ImageVariant      variant,
        string            blobName,
        CancellationToken ct)
    {
        var client = _container.Value.GetBlobClient(blobName);
        using var ms = new MemoryStream(variant.Data);
        await client.UploadAsync(ms,
            new BlobHttpHeaders { ContentType = variant.ContentType },
            cancellationToken: ct);
        return GetUrl(blobName);
    }

    private string GetUrl(string blobName)
    {
        if (!string.IsNullOrWhiteSpace(_settings.BaseUrl))
            return $"{_settings.BaseUrl.TrimEnd('/')}/{blobName}";

        return _container.Value.GetBlobClient(blobName).Uri.ToString();
    }

    private async Task EnsureContainerExistsAsync(CancellationToken ct)
    {
        if (_containerReady) return;
        await _container.Value.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
        _containerReady = true;
    }
}
