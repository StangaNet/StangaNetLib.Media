# StangaNetLib.Media — Copilot instructions

- Image optimization (ImageSharp), magic-byte validation, and photo storage (local / Azure Blob / StangaNetLib.Storage-backed).
- Prefer `Result` / `Result<T>` from `StangaNetLib.Core` for expected failures; reserve exceptions for invalid arguments / configuration.
- Keep public APIs documented (`///`) and provider-agnostic at the call site via `IPhotoStorageService` / `IImageOptimizationService`.
- DI entry point is `AddStangaNetLibMedia` in `StangaNetLib.Media.Extensions`.
- Match existing naming, file-scoped namespaces, and EditorConfig / Roslyn conventions.
- Tests use xUnit v3 + FluentAssertions; cover both `net8.0` and `net9.0` when changing shared behavior.
