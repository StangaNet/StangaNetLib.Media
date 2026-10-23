# StangaNetLib.Media

[![.NET CI](https://github.com/StangaNet/StangaNetLib.Media/actions/workflows/main.yml/badge.svg)](https://github.com/StangaNet/StangaNetLib.Media/actions/workflows/main.yml)
![NuGet](https://img.shields.io/badge/nuget-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4)

Image optimization (multi-variant JPEG/WebP), magic-byte photo validation, and photo storage abstractions (local, Azure Blob, or [`StangaNetLib.Storage`](https://github.com/StangaNet/StangaNetLib.Storage)-backed). Operations return `Result` / `Result<T>` from [`StangaNetLib.Core`](https://github.com/StangaNet/StangaNetLib.Core).

## Design Philosophy

- **Domain-centric**: optimization, validation, and photo storage as capabilities.
- **Functional failures**: expected outcomes map to `Result` / `Result<T>`.
- **Pluggable storage**: local, Azure Blob, null, or any `IBlobStorageService`.
- **Modern .NET**: `net8.0` / `net9.0` / `net10.0` with SixLabors.ImageSharp.

## Installation

The package is hosted on **GitHub Packages** (pulls Storage / Core from the same feed).

```xml
<!-- NuGet.config — source key must match your packageSourceCredentials -->
<configuration>
  <packageSources>
    <add key="StangaNet" value="https://nuget.pkg.github.com/StangaNet/index.json" />
  </packageSources>
</configuration>
```

```xml
<!-- .csproj -->
<PackageReference Include="StangaNetLib.Media" Version="1.0.1" />
```

See [CHANGELOG.md](./CHANGELOG.md) for release history.

## Core Components

| Namespace | Components | Purpose |
|---|---|---|
| `StangaNetLib.Media.Abstractions` | `IImageOptimizationService`, `IPhotoStorageService` | Public contracts |
| `StangaNetLib.Media.Models` | `ImageVariants`, `PhotoUploadResult`, `PhotoUrls`, … | Domain models |
| `StangaNetLib.Media.Services` | `ImageOptimizationService` | Multi-size JPEG/WebP |
| `StangaNetLib.Media.Storage` | Local / Azure / Storage-backed / Null | Photo persistence |
| `StangaNetLib.Media.Validation` | `PhotoValidator` | Magic-byte and size checks |
| `StangaNetLib.Media.Settings` | `ImageSettings`, storage options + validators | Configuration |
| `StangaNetLib.Media.Extensions` | `AddStangaNetLibMedia`, … | DI |

## Project Structure

```
StangaNetLib.Media/
├── assets/
│   └── package-icon.png
├── build/
│   └── StangaNetLib.Media.props
├── src/
│   └── StangaNetLib.Media/
│       ├── Abstractions/
│       ├── Extensions/
│       ├── Models/
│       ├── Services/
│       ├── Settings/
│       ├── Storage/
│       └── Validation/
└── tests/
    └── StangaNetLib.Media.Tests/
```

## Quick Start

```csharp
builder.Services.AddStangaNetLibMedia(builder.Configuration);
```

```csharp
public class PhotoService(
    IImageOptimizationService optimizer,
    IPhotoStorageService storage)
{
    public async Task<Result<PhotoUploadResult>> UploadAsync(Stream image, CancellationToken ct)
    {
        var variants = await optimizer.OptimizeAsync(image, ct);
        if (variants.IsFailure)
            return Result.Failure<PhotoUploadResult>(variants.Error);

        return await storage.UploadAsync(variants.Value, ct);
    }
}
```

## CI/CD

On push/PR to `main` and `workflow_dispatch`, the shared StangaNet workflow:

1. Restores, builds, and tests on .NET 8 and .NET 9
2. Packs the NuGet package (with symbols)
3. Publishes to GitHub Packages outside pull requests
4. Uploads the `.nupkg` as a build artifact
