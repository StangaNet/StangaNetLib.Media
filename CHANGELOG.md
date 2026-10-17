# Changelog

All notable changes to **StangaNetLib.Media** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.1] - 2026-10-18

### Fixed
- Depend on `StangaNetLib.Storage` 1.0.1 (ME 8.x graph). The feed's Storage 1.0.0 nuspec pulled ME 10.x / Azure.Storage.Blobs 12.29.x and caused NU1605 downgrade errors under `TreatWarningsAsErrors`.

## [1.0.0] - 2026-10-17

### Added
- Multi-variant image optimization (JPEG/WebP) via SixLabors.ImageSharp.
- Magic-byte photo validation and size checks (`PhotoValidator`).
- Photo storage: local filesystem, Azure Blob, null, and `StangaNetLib.Storage`-backed.
- DI registration (`AddStangaNetLibMedia` / local / storage-backed) with options validators.
- Dual targeting `net8.0` / `net9.0`, XML docs, symbols (`snupkg`), package icon, and MSBuild consumer metadata stamp.
- Depends on `StangaNetLib.Storage` 1.0.0 (brings `StangaNetLib.Core`); Microsoft.Extensions.* pinned to 8.x.
- SixLabors.ImageSharp 3.1.12 (last version without commercial license MSBuild gate).
- CI via shared StangaNet reusable workflow (push / PR / manual; publish outside PRs).

[Unreleased]: https://github.com/StangaNet/StangaNetLib.Media/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/StangaNet/StangaNetLib.Media/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/StangaNet/StangaNetLib.Media/releases/tag/v1.0.0
