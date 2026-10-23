using Microsoft.Extensions.Options;
using StangaNetLib.Core.Common;
using StangaNetLib.Media.Abstractions;
using StangaNetLib.Media.Models;
using StangaNetLib.Media.Settings;

namespace StangaNetLib.Media.Storage;

/// <summary>
/// Stores variants on the local filesystem under <see cref="StorageSettings.LocalBasePath"/>.
/// Suitable for local-only / single-instance self-hosted deployments (no Azure required).
/// </summary>
internal sealed class LocalPhotoStorageService(IOptions<StorageSettings> options)
    : IPhotoStorageService
{
    private readonly StorageSettings _settings = options.Value;

    public async Task<Result<PhotoUploadResult>> UploadVariantsAsync(
        string            ownerId,
        ImageVariants     variants,
        CancellationToken ct = default)
    {
        try
        {
            var photoId = Guid.NewGuid().ToString("N");
            var dir     = Path.Combine(_settings.LocalBasePath, ownerId);
            Directory.CreateDirectory(dir);

            async Task<string> SaveAsync(ImageVariant v, string suffix)
            {
                var fileName = $"{photoId}{suffix}{v.Extension}";
                await File.WriteAllBytesAsync(Path.Combine(dir, fileName), v.Data, ct);
                return $"{_settings.LocalBaseUrl.TrimEnd('/')}/{ownerId}/{fileName}";
            }

            var thumbTask    = SaveAsync(variants.Thumbnail, "_thumb");
            var mediumTask   = SaveAsync(variants.Medium,    "_medium");
            var largeTask    = SaveAsync(variants.Large,     "_large");
            var originalTask = SaveAsync(variants.Original,  "_original");
            var webpTask     = variants.WebP is not null ? SaveAsync(variants.WebP, "_webp") : null;

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

    public Task<Result> DeleteVariantsAsync(
        string            ownerId,
        string            photoId,
        CancellationToken ct = default)
    {
        try
        {
            var dir = Path.Combine(_settings.LocalBasePath, ownerId);
            if (!Directory.Exists(dir))
                return Task.FromResult(Result.Success());

            foreach (var file in Directory.GetFiles(dir, $"{photoId}*"))
                File.Delete(file);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                Result.Failure(new Error("Storage.DeleteFailed", ex.Message)));
        }
    }

    public Task<bool> ExistsAsync(
        string            ownerId,
        string            photoId,
        CancellationToken ct = default)
    {
        var dir = Path.Combine(_settings.LocalBasePath, ownerId);
        var exists = Directory.Exists(dir) &&
                     Directory.GetFiles(dir, $"{photoId}*").Any();
        return Task.FromResult(exists);
    }
}
