# StangaNetLib.Media

[![.NET CI](https://github.com/StangaNet/StangaNetLib.Media/actions/workflows/main.yml/badge.svg)](https://github.com/StangaNet/StangaNetLib.Media/actions/workflows/main.yml)
![NuGet](https://img.shields.io/badge/nuget-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)

StangaNetLib.Media provides image optimization (multi-variant JPEG/WebP), magic-byte photo validation, and photo storage abstractions (local, Azure Blob, or `StangaNetLib.Storage`-backed). Operations return `Result` / `Result<T>` from `StangaNetLib.Core`.

## Design Philosophy

- **Domain-Centric**: Organized around media capabilities (optimization, validation, photo storage) rather than infrastructure details.
- **Functional Error Handling**: All public operations return `Result` / `Result<T>` from `StangaNetLib.Core`.
- **Pluggable Storage**: Local filesystem, Azure Blob, null (no-op), or any `IBlobStorageService` from `StangaNetLib.Storage`.
- **Modern Standards**: Optimized for .NET 8.0 and 9.0 with SixLabors.ImageSharp.

## Installation

The package is hosted on **GitHub Packages**.

```xml
<!-- NuGet.config — add the GitHub Packages source -->
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/StangaNet/index.json" />
  </packageSources>
</configuration>
```

```xml
<!-- .csproj -->
<PackageReference Include="StangaNetLib.Media" Version="1.0.0" />
```

**Dependencies**: `StangaNetLib.Core`, `StangaNetLib.Storage`, `SixLabors.ImageSharp`, `Azure.Storage.Blobs`, `Microsoft.Extensions.*`.

---

## Core Components

*The library is organized into functional namespaces. For implementation details, refer directly to the source code.*

| Namespace | Core Components | Purpose |
| :--- | :--- | :--- |
| `StangaNetLib.Media.Abstractions` | `IImageOptimizationService`, `IPhotoStorageService` | Public contracts. |
| `StangaNetLib.Media.Models` | `ImageVariants`, `ImageVariant`, `PhotoUploadResult`, `PhotoUrls`, `ImageSizeType` | Domain models. |
| `StangaNetLib.Media.Services` | `ImageOptimizationService` | Multi-size JPEG/WebP generation. |
| `StangaNetLib.Media.Storage` | `LocalPhotoStorageService`, `AzureBlobPhotoStorageService`, `StorageBackedPhotoStorageService`, `NullPhotoStorageService` | Photo persistence. |
| `StangaNetLib.Media.Validation` | `PhotoValidator` | Magic-byte and size validation. |
| `StangaNetLib.Media.Settings` | `ImageSettings`, `StorageSettings`, `AzureBlobStorageSettings`, … | Configuration. |
| `StangaNetLib.Media.Extensions` | `ServiceCollectionExtensions` | DI registration. |

---

## Project Structure

```
StangaNetLib.Media/
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

---

## Quick Start

```csharp
builder.Services.AddStangaNetLibMedia(builder.Configuration);
// Requires ImageSettings (+ StorageSettings / AzureBlobStorageSettings as needed)
```

```csharp
public class PhotoService(IImageOptimizationService optimizer, IPhotoStorageService storage)
{
    public async Task<Result<PhotoUploadResult>> UploadAsync(Stream image, string ownerId, CancellationToken ct)
    {
        var variants = await optimizer.ProcessAsync(image, ct);
        if (variants.IsFailure) return variants.Error;
        return await storage.UploadVariantsAsync(ownerId, variants.Value!, ct);
    }
}
```

## About

Image optimization (multi-variant JPEG/WebP), magic-byte validation, and cloud/local photo storage abstraction for .NET applications.
