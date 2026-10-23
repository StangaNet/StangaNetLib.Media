using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using StangaNetLib.Core.Common;
using StangaNetLib.Media.Abstractions;
using StangaNetLib.Media.Models;
using StangaNetLib.Media.Settings;
using StangaNetLib.Media.Validation;

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
            var contentType = DetectContentType(imageData);
            var validation = PhotoValidator.Validate(
                imageData,
                fileName: "upload",
                contentType: contentType,
                maxSizeBytes: _settings.MaxUploadSizeBytes,
                allowedMimeTypes: _settings.AllowedMimeTypes);
            if (validation.IsFailure)
                return Result<ImageVariants>.Failure(validation.Error);

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

    private static string DetectContentType(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "image/jpeg";
        if (data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return "image/png";
        if (data.Length >= 12 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) && data.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return "image/webp";
        if (data.Length >= 6 && data.AsSpan(0, 6).SequenceEqual("GIF89a"u8)
            || data.Length >= 6 && data.AsSpan(0, 6).SequenceEqual("GIF87a"u8))
            return "image/gif";
        return "application/octet-stream";
    }
}
