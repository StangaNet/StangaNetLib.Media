using StangaNetLib.Core.Validators;

namespace StangaNetLib.Media.Settings;

/// <summary>
/// Validates <see cref="AzureBlobStorageSettings"/> on first options access.
/// Rejects empty <see cref="AzureBlobStorageSettings.ConnectionString"/> or
/// <see cref="AzureBlobStorageSettings.ContainerName"/>.
/// </summary>
internal sealed class AzureBlobStorageSettingsValidator : SettingsValidatorBase<AzureBlobStorageSettings>
{
    /// <inheritdoc/>
    protected override void ValidateCore(AzureBlobStorageSettings options, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            errors.Add("AzureBlobStorage:ConnectionString is required when StorageProvider = AzureBlob.");

        if (string.IsNullOrWhiteSpace(options.ContainerName))
            errors.Add("AzureBlobStorage:ContainerName is required.");
    }
}
