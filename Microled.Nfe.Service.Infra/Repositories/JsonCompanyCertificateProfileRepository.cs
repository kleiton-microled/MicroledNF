using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.Models;
using Microled.Nfe.Service.Infra.Configuration;

namespace Microled.Nfe.Service.Infra.Repositories;

/// <summary>
/// Stores company certificate profiles in a local JSON file.
/// </summary>
public class JsonCompanyCertificateProfileRepository : ICompanyCertificateProfileRepository
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly LocalCertificateProfileStorageOptions _storageOptions;
    private readonly ILogger<JsonCompanyCertificateProfileRepository> _logger;

    public JsonCompanyCertificateProfileRepository(
        IOptions<LocalCertificateProfileStorageOptions> storageOptions,
        ILogger<JsonCompanyCertificateProfileRepository> logger)
    {
        _storageOptions = storageOptions?.Value ?? throw new ArgumentNullException(nameof(storageOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<CompanyCertificateProfile>> GetAllAsync(CancellationToken cancellationToken)
    {
        await FileLock.WaitAsync(cancellationToken);

        try
        {
            return await ReadProfilesUnsafeAsync(cancellationToken);
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task<CompanyCertificateProfile?> GetActiveAsync(CancellationToken cancellationToken)
    {
        var profiles = await GetAllAsync(cancellationToken);
        return profiles.FirstOrDefault(profile => profile.IsActive);
    }

    public async Task<CompanyCertificateProfile?> GetByThumbprintAsync(string thumbprint, CancellationToken cancellationToken)
    {
        var normalizedThumbprint = NormalizeThumbprint(thumbprint);
        var profiles = await GetAllAsync(cancellationToken);

        return profiles.FirstOrDefault(profile =>
            string.Equals(profile.Thumbprint, normalizedThumbprint, StringComparison.Ordinal));
    }

    public async Task SaveAsync(CompanyCertificateProfile profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await FileLock.WaitAsync(cancellationToken);

        try
        {
            var profiles = await ReadProfilesUnsafeAsync(cancellationToken);
            var normalizedThumbprint = NormalizeThumbprint(profile.Thumbprint);
            var existingIndex = profiles.FindIndex(item => item.Thumbprint == normalizedThumbprint);

            profile.Thumbprint = normalizedThumbprint;
            profile.StoreLocation = NormalizeStoreValue(profile.StoreLocation, "CurrentUser");
            profile.StoreName = NormalizeStoreValue(profile.StoreName, "My");
            profile.Subject = profile.Subject?.Trim() ?? string.Empty;
            profile.CompanyName = profile.CompanyName?.Trim() ?? string.Empty;
            profile.Cnpj = DigitsOnly(profile.Cnpj);
            profile.MunicipalRegistration = profile.MunicipalRegistration?.Trim() ?? string.Empty;
            profile.DefaultRemetenteCnpj = NormalizeOptionalDigits(profile.DefaultRemetenteCnpj);
            profile.Environment = NormalizeOptionalText(profile.Environment);
            profile.Notes = NormalizeOptionalText(profile.Notes);
            profile.UpdatedAtUtc = profile.UpdatedAtUtc == default ? DateTime.UtcNow : profile.UpdatedAtUtc;

            if (profile.IsActive)
            {
                foreach (var item in profiles)
                {
                    item.IsActive = false;
                }
            }

            if (existingIndex >= 0)
            {
                profiles[existingIndex] = profile;
            }
            else
            {
                profiles.Add(profile);
            }

            await WriteProfilesUnsafeAsync(profiles, cancellationToken);
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task SetActiveAsync(string thumbprint, CancellationToken cancellationToken)
    {
        var normalizedThumbprint = NormalizeThumbprint(thumbprint);

        await FileLock.WaitAsync(cancellationToken);

        try
        {
            var profiles = await ReadProfilesUnsafeAsync(cancellationToken);
            var activeProfile = profiles.FirstOrDefault(profile => profile.Thumbprint == normalizedThumbprint);

            if (activeProfile == null)
            {
                throw new InvalidOperationException("Nao foi encontrado perfil para o certificado selecionado.");
            }

            foreach (var profile in profiles)
            {
                profile.IsActive = profile.Thumbprint == normalizedThumbprint;
                if (profile.IsActive)
                {
                    profile.UpdatedAtUtc = DateTime.UtcNow;
                }
            }

            await WriteProfilesUnsafeAsync(profiles, cancellationToken);
        }
        finally
        {
            FileLock.Release();
        }
    }

    private async Task<List<CompanyCertificateProfile>> ReadProfilesUnsafeAsync(CancellationToken cancellationToken)
    {
        var filePath = EnsureStoragePath();

        if (!File.Exists(filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(filePath);
        var profiles = await JsonSerializer.DeserializeAsync<List<CompanyCertificateProfile>>(stream, SerializerOptions, cancellationToken);
        return profiles ?? [];
    }

    private async Task WriteProfilesUnsafeAsync(List<CompanyCertificateProfile> profiles, CancellationToken cancellationToken)
    {
        var filePath = EnsureStoragePath();
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, profiles, SerializerOptions, cancellationToken);

        _logger.LogInformation("Certificate profile repository persisted to {FilePath}", filePath);
    }

    private string EnsureStoragePath()
    {
        var dataDirectory = string.IsNullOrWhiteSpace(_storageOptions.DataDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "certificates")
            : _storageOptions.DataDirectory;

        Directory.CreateDirectory(dataDirectory);
        return Path.Combine(dataDirectory, _storageOptions.FileName);
    }

    private static string NormalizeThumbprint(string? thumbprint)
    {
        return string.IsNullOrWhiteSpace(thumbprint)
            ? string.Empty
            : thumbprint.Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace(":", string.Empty)
                .ToUpperInvariant();
    }

    private static string NormalizeStoreValue(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string DigitsOnly(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());
    }

    private static string? NormalizeOptionalDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = DigitsOnly(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
