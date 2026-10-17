using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StangaNetLib.Media.Abstractions;
using StangaNetLib.Media.Extensions;
using StangaNetLib.Media.Services;
using StangaNetLib.Media.Storage;

namespace StangaNetLib.Media.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddStangaNetLibMedia_Local_RegistersExpectedServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorageSettings:Provider"] = "Local",
                ["StorageSettings:LocalBasePath"] = "uploads",
                ["StorageSettings:LocalBaseUrl"] = "/uploads",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddStangaNetLibMedia(configuration);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IImageOptimizationService>().Should().BeOfType<ImageOptimizationService>();
        provider.GetRequiredService<IPhotoStorageService>().Should().BeOfType<LocalPhotoStorageService>();
    }

    [Fact]
    public void AddStangaNetLibMediaLocal_RegistersLocalStorage()
    {
        var services = new ServiceCollection();
        services.AddStangaNetLibMediaLocal(basePath: "tmp-photos", baseUrl: "/tmp-photos");

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IImageOptimizationService>().Should().BeOfType<ImageOptimizationService>();
        provider.GetRequiredService<IPhotoStorageService>().Should().BeOfType<LocalPhotoStorageService>();
    }

    [Fact]
    public void AddMediaWithStorage_RegistersStorageBackedPhotoService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorageBackedPhotoStorage:ContainerName"] = "photos",
                ["StorageBackedPhotoStorage:BaseUrl"] = "https://cdn.example/photos",
            })
            .Build();

        var services = new ServiceCollection();
        // Caller must register IBlobStorageService separately; resolve only optimizer here.
        services.AddMediaWithStorage(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IImageOptimizationService>().Should().BeOfType<ImageOptimizationService>();
        services.Should().Contain(d =>
            d.ServiceType == typeof(IPhotoStorageService) &&
            d.ImplementationType == typeof(StorageBackedPhotoStorageService));
    }
}
