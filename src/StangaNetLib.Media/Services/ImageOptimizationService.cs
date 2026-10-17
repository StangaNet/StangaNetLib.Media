using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using StangaNetLib.Core.Common;
using StangaNetLib.Media.Abstractions;
using StangaNetLib.Media.Models;
using StangaNetLib.Media.Settings;

namespace StangaNetLib.Media.Services;

internal sealed class ImageOptimizationService(IOptions<ImageSettings> options)
    : IImageOptimizationService
{
    private readonly ImageSettings _settings = options.Value;

    // ─── IImageOptimizationService ───────────────────────────────────────────

    public async Task<Result<ImageVariants>> ProcessAsync(
        byte[]            imageData,
        CancellationToken ct = default)
    {
        try
        {
            // Decode once; each variant clones the source to avoid re-decode overhead.
            using var source = Image.Load(imageData);

            var thumbnail = await ResizeCloneAsync(source, _settings.ThumbnailSize, _settings.ThumbnailQuality,    ct);
            var medium    = await ResizeCloneAsync(source, _settings.MediumSize,    _settings.CompressionQuality, ct);
            var large     = await ResizeCloneAsync(source, _settings.LargeSize,     _settings.CompressionQuality, ct);
            var original  = await EncodeJpegAsync(source, _settings.CompressionQuality, ct);

            ImageVariant? webp = null;
            if (_settings.EnableWebP)
                webp = await WebPCloneAsync(source, _settings.WebPQuality, _settings.MediumSize, ct);

            return Result<ImageVariants>.Success(new ImageVariants
            {
                Thumbnail = thumbnail,
                Medium    = medium,
                Large     = large,
                Original  = original,
                WebP      = webp,
            });
        }
        catch (Exception ex)
        {
            return new Error("Media.ProcessingFailed", ex.Message);
        }
    }

    public Task<Result<ImageVariant>> ResizeAsync(
        byte[]            imageData,
        int               maxDimension,
        int               quality,
        CancellationToken ct = default)
        => WrapAsync(() => ResizeCoreAsync(imageData, maxDimension, quality, ct));

    public Task<Result<ImageVariant>> ConvertToWebPAsync(
        byte[]            imageData,
        int               quality,
        int               maxDimension = 0,
        CancellationToken ct = default)
        => WrapAsync(() => ConvertToWebPCoreAsync(imageData, quality, maxDimension, ct));

    // ─── helpers for ProcessAsync (operate on already-loaded Image) ──────────

    private async Task<ImageVariant> ResizeCloneAsync(
        Image             source,
        int               maxDimension,
        int               quality,
        CancellationToken ct)
    {
        using var clone = source.Clone(ctx =>
        {
            if (source.Width > maxDimension || source.Height > maxDimension)
                ctx.Resize(new ResizeOptions
                {
                    Size    = new Size(maxDimension, maxDimension),
                    Mode    = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3,
                });
        });
        return await EncodeJpegAsync(clone, quality, ct);
    }

    private async Task<ImageVariant> WebPCloneAsync(
        Image             source,
        int               quality,
        int               maxDimension,
        CancellationToken ct)
    {
        using var clone = source.Clone(ctx =>
        {
            if (maxDimension > 0 && (source.Width > maxDimension || source.Height > maxDimension))
                ctx.Resize(new ResizeOptions
                {
                    Size    = new Size(maxDimension, maxDimension),
                    Mode    = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3,
                });
        });

        var encoder = new WebpEncoder
        {
            Quality = quality,
            Method  = WebpEncodingMethod.Default,
        };

        using var ms = new MemoryStream();
        await clone.SaveAsync(ms, encoder, ct);
        var bytes = ms.ToArray();

        return new ImageVariant(
            Data:        bytes,
            ContentType: "image/webp",
            Extension:   ".webp",
            Width:       clone.Width,
            Height:      clone.Height,
            SizeBytes:   bytes.Length);
    }

    // ─── helpers for public single-variant methods (load from bytes) ─────────

    private async Task<ImageVariant> ResizeCoreAsync(
        byte[]            data,
        int               maxDimension,
        int               quality,
        CancellationToken ct)
    {
        using var image = Image.Load(data);
        if (image.Width > maxDimension || image.Height > maxDimension)
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size    = new Size(maxDimension, maxDimension),
                Mode    = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3,
            }));
        return await EncodeJpegAsync(image, quality, ct);
    }

    private async Task<ImageVariant> ConvertToWebPCoreAsync(
        byte[]            data,
        int               quality,
        int               maxDimension,
        CancellationToken ct)
    {
        using var image = Image.Load(data);
        if (maxDimension > 0 && (image.Width > maxDimension || image.Height > maxDimension))
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size    = new Size(maxDimension, maxDimension),
                Mode    = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3,
            }));

        var encoder = new WebpEncoder
        {
            Quality = quality,
            Method  = WebpEncodingMethod.Default,
        };

        using var ms = new MemoryStream();
        await image.SaveAsync(ms, encoder, ct);
        var bytes = ms.ToArray();

        return new ImageVariant(
            Data:        bytes,
            ContentType: "image/webp",
            Extension:   ".webp",
            Width:       image.Width,
            Height:      image.Height,
            SizeBytes:   bytes.Length);
    }

    // ─── shared ──────────────────────────────────────────────────────────────

    private async Task<ImageVariant> EncodeJpegAsync(
        Image             image,
        int               quality,
        CancellationToken ct)
    {
        var encoder = new JpegEncoder
        {
            Quality     = quality,
            // Progressive JPEG: Interleaved = false → progressive, true → baseline
            Interleaved = !_settings.EnableProgressiveJpeg,
        };

        using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, encoder, ct);
        var bytes = ms.ToArray();

        return new ImageVariant(
            Data:        bytes,
            ContentType: "image/jpeg",
            Extension:   ".jpg",
            Width:       image.Width,
            Height:      image.Height,
            SizeBytes:   bytes.Length);
    }

    private static async Task<Result<ImageVariant>> WrapAsync(Func<Task<ImageVariant>> action)
    {
        try   { return Result<ImageVariant>.Success(await action()); }
        catch (Exception ex) { return new Error("Media.ProcessingFailed", ex.Message); }
    }
}
