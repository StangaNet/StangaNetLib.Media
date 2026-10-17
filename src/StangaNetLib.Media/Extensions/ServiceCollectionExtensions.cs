using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StangaNetLib.Media.Abstractions;
using StangaNetLib.Media.Services;
using StangaNetLib.Media.Settings;
using StangaNetLib.Media.Storage;

namespace StangaNetLib.Media.Extensions;

/// <summary>
/// Extension methods to register StangaNetLib.Media services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IImageOptimizationService"/> and <see cref="IPhotoStorageService"/>.
    /// The storage backend is chosen by <c>StorageSettings:Provider</c> in configuration.
    /// </summary>
    /// <remarks>
    /// Required appsettings sections: <c>ImageSettings</c>, <c>StorageSettings</c>.
    /// When using Azure Blob Storage also add <c>AzureBlobStorage</c>.
    /// </remarks>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Application configuration used to bind settings sections.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibMedia(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        services.Configure<ImageSettings>(
            configuration.GetSection(ImageSettings.SectionName));
        services.AddSingleton<IValidateOptions<ImageSettings>, ImageSettingsValidator>();

        services.Configure<StorageSettings>(
            configuration.GetSection(StorageSettings.SectionName));

        services.AddSingleton<IImageOptimizationService, ImageOptimizationService>();

        var storageSettings = configuration
            .GetSection(StorageSettings.SectionName)
            .Get<StorageSettings>() ?? new StorageSettings();

        if (storageSettings.Provider == StorageProvider.AzureBlob)
        {
            services.Configure<AzureBlobStorageSettings>(
                configuration.GetSection(AzureBlobStorageSettings.SectionName));
            services.AddSingleton<IValidateOptions<AzureBlobStorageSettings>, AzureBlobStorageSettingsValidator>();

            services.AddSingleton<IPhotoStorageService, AzureBlobPhotoStorageService>();
        }
        else
        {
            services.AddSingleton<IPhotoStorageService, LocalPhotoStorageService>();
        }

        return services;
    }

    /// <summary>
    /// Convenience overload that registers the Local filesystem provider directly,
    /// without requiring an appsettings file. Useful in integration tests or development.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="basePath">Root directory for storing variant files (default "wwwroot/uploads/photos").</param>
    /// <param name="baseUrl">URL prefix returned in photo URLs (default "/uploads/photos").</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibMediaLocal(
        this IServiceCollection services,
        string basePath = "wwwroot/uploads/photos",
        string baseUrl  = "/uploads/photos")
    {
        services.Configure<ImageSettings>(_ => { });
        services.AddSingleton<IValidateOptions<ImageSettings>, ImageSettingsValidator>();

        services.Configure<StorageSettings>(opts =>
        {
            opts.Provider      = StorageProvider.Local;
            opts.LocalBasePath = basePath;
            opts.LocalBaseUrl  = baseUrl;
        });

        services.AddSingleton<IImageOptimizationService, ImageOptimizationService>();
        services.AddSingleton<IPhotoStorageService, LocalPhotoStorageService>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="IImageOptimizationService"/> and a <see cref="IPhotoStorageService"/>
    /// backed by an <c>IBlobStorageService</c> from <c>StangaNetLib.Storage</c>.
    /// </summary>
    /// <remarks>
    /// The caller must register <c>IBlobStorageService</c> separately (via <c>AddStangaNetLibStorage</c>)
    /// before or after calling this method.
    /// Required appsettings section: <c>ImageSettings</c>, <c>StorageBackedPhotoStorage</c>.
    /// </remarks>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">Application configuration used to bind settings sections.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMediaWithStorage(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        services.Configure<ImageSettings>(
            configuration.GetSection(ImageSettings.SectionName));
        services.AddSingleton<IValidateOptions<ImageSettings>, ImageSettingsValidator>();

        services.Configure<StorageBackedPhotoStorageSettings>(
            configuration.GetSection(StorageBackedPhotoStorageSettings.SectionName));

        services.AddSingleton<IImageOptimizationService, ImageOptimizationService>();
        services.AddSingleton<IPhotoStorageService, StorageBackedPhotoStorageService>();

        return services;
    }
}
