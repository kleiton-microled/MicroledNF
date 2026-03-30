using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Domain.Interfaces;
using Microled.Nfe.Service.Domain.Models;

namespace Microled.Nfe.Service.Infra.Services;

/// <summary>
/// Discovers certificates from the Windows certificate store without forcing PIN prompts.
/// </summary>
public class WindowsCertificateDiscoveryService : ICertificateDiscoveryService
{
    private static readonly (StoreLocation Location, StoreName Name)[] SupportedStores =
    {
        (StoreLocation.CurrentUser, StoreName.My),
        (StoreLocation.LocalMachine, StoreName.My)
    };

    private static readonly string[] A3ProviderKeywords =
    {
        "smartcard",
        "smart card",
        "token",
        "etoken",
        "safenet",
        "safeweb",
        "gemalto",
        "cryptovision",
        "hardware",
        "cartao"
    };

    private readonly ILogger<WindowsCertificateDiscoveryService> _logger;

    public WindowsCertificateDiscoveryService(ILogger<WindowsCertificateDiscoveryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<CertificateDiscoveryItem>> GetAvailableCertificatesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Starting certificate discovery in machine stores");

        var certificates = new List<CertificateDiscoveryItem>();

        foreach (var (location, name) in SupportedStores)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var store = new X509Store(name, location);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                foreach (var certificate in store.Certificates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var item = MapCertificate(certificate, location, name);
                        certificates.Add(item);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to inspect certificate from {StoreLocation}/{StoreName}",
                            location,
                            name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to open certificate store {StoreLocation}/{StoreName}",
                    location,
                    name);
            }
        }

        var result = certificates
            .GroupBy(item => $"{NormalizeThumbprint(item.Thumbprint)}|{item.StoreLocation}|{item.StoreName}")
            .Select(group => group.First())
            .OrderByDescending(item => item.HasPrivateKey)
            .ThenByDescending(item => item.IsA3Candidate)
            .ThenBy(item => item.SimpleName ?? item.Subject, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogInformation("Certificate discovery finished. {Count} certificate(s) found", result.Count);

        return Task.FromResult<IReadOnlyList<CertificateDiscoveryItem>>(result);
    }

    public Task<CertificateDiscoveryItem?> FindByThumbprintAsync(
        string thumbprint,
        string storeLocation,
        string storeName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Enum.TryParse<StoreLocation>(storeLocation, true, out var parsedStoreLocation))
        {
            throw new ArgumentException($"Invalid store location '{storeLocation}'.", nameof(storeLocation));
        }

        if (!Enum.TryParse<StoreName>(storeName, true, out var parsedStoreName))
        {
            throw new ArgumentException($"Invalid store name '{storeName}'.", nameof(storeName));
        }

        using var store = new X509Store(parsedStoreName, parsedStoreLocation);
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

        var normalizedThumbprint = NormalizeThumbprint(thumbprint);
        var certificate = store.Certificates
            .OfType<X509Certificate2>()
            .FirstOrDefault(item => NormalizeThumbprint(item.Thumbprint) == normalizedThumbprint);

        return Task.FromResult(
            certificate == null
                ? null
                : MapCertificate(certificate, parsedStoreLocation, parsedStoreName));
    }

    private CertificateDiscoveryItem MapCertificate(X509Certificate2 certificate, StoreLocation storeLocation, StoreName storeName)
    {
        var thumbprint = NormalizeThumbprint(certificate.Thumbprint);
        var providerInfo = TryGetProviderInfo(certificate);
        var silentAcquireSucceeded = TryAcquirePrivateKeySilently(certificate, out _);
        var simpleName = SafeGetSimpleName(certificate);
        var cnpj = ExtractIdentifier(certificate.Subject, 14);
        var cpf = ExtractIdentifier(certificate.Subject, 11);
        var isA3Candidate = certificate.HasPrivateKey &&
                            (ContainsProviderKeyword(providerInfo.ProviderName) ||
                             ContainsProviderKeyword(providerInfo.ContainerName) ||
                             !silentAcquireSucceeded);

        return new CertificateDiscoveryItem
        {
            Thumbprint = thumbprint,
            Subject = certificate.Subject ?? string.Empty,
            Issuer = certificate.Issuer ?? string.Empty,
            SerialNumber = certificate.SerialNumber ?? string.Empty,
            NotBefore = certificate.NotBefore,
            NotAfter = certificate.NotAfter,
            HasPrivateKey = certificate.HasPrivateKey,
            StoreLocation = storeLocation.ToString(),
            StoreName = storeName.ToString(),
            SimpleName = simpleName,
            Cnpj = cnpj,
            Cpf = cpf,
            IsA3Candidate = isA3Candidate
        };
    }

    private static string SafeGetSimpleName(X509Certificate2 certificate)
    {
        try
        {
            return certificate.GetNameInfo(X509NameType.SimpleName, false);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? ExtractIdentifier(string? subject, int digits)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var match = Regex.Match(subject, $@"(?<!\d)(\d{{{digits}}})(?!\d)");
        return match.Success ? match.Groups[1].Value : null;
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

    private static bool ContainsProviderKeyword(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return A3ProviderKeywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static ProviderInfo TryGetProviderInfo(X509Certificate2 certificate)
    {
        if (!OperatingSystem.IsWindows())
        {
            return ProviderInfo.Empty;
        }

        const uint CertKeyProvInfoPropId = 2;
        uint size = 0;

        if (!CertGetCertificateContextProperty(certificate.Handle, CertKeyProvInfoPropId, IntPtr.Zero, ref size) || size == 0)
        {
            return ProviderInfo.Empty;
        }

        var buffer = Marshal.AllocHGlobal((int)size);

        try
        {
            if (!CertGetCertificateContextProperty(certificate.Handle, CertKeyProvInfoPropId, buffer, ref size))
            {
                return ProviderInfo.Empty;
            }

            var info = Marshal.PtrToStructure<CryptKeyProvInfo>(buffer);
            return new ProviderInfo(
                Marshal.PtrToStringUni(info.ProviderNamePointer),
                Marshal.PtrToStringUni(info.ContainerNamePointer));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool TryAcquirePrivateKeySilently(X509Certificate2 certificate, out int keySpec)
    {
        keySpec = 0;

        if (!OperatingSystem.IsWindows() || !certificate.HasPrivateKey)
        {
            return certificate.HasPrivateKey;
        }

        const uint flags = CryptAcquireSilentFlag | CryptAcquireAllowNcryptKeyFlag;

        if (!CryptAcquireCertificatePrivateKey(
                certificate.Handle,
                flags,
                IntPtr.Zero,
                out var keyHandle,
                out keySpec,
                out var callerMustFree))
        {
            return false;
        }

        if (callerMustFree)
        {
            ReleaseKeyHandle(keyHandle, keySpec);
        }

        return true;
    }

    private static void ReleaseKeyHandle(IntPtr keyHandle, int keySpec)
    {
        if (keyHandle == IntPtr.Zero)
        {
            return;
        }

        if (keySpec == CertNcryptKeySpec)
        {
            NCryptFreeObject(keyHandle);
            return;
        }

        CryptReleaseContext(keyHandle, 0);
    }

    private readonly record struct ProviderInfo(string? ProviderName, string? ContainerName)
    {
        public static ProviderInfo Empty => new(null, null);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CryptKeyProvInfo
    {
        public IntPtr ContainerNamePointer;
        public IntPtr ProviderNamePointer;
        public uint ProviderType;
        public uint Flags;
        public uint ParameterCount;
        public IntPtr ParametersPointer;
        public uint KeySpec;
    }

    private const uint CryptAcquireSilentFlag = 0x00000040;
    private const uint CryptAcquireAllowNcryptKeyFlag = 0x00010000;
    private const int CertNcryptKeySpec = unchecked((int)0xFFFFFFFF);

    [DllImport("crypt32.dll", SetLastError = true)]
    private static extern bool CryptAcquireCertificatePrivateKey(
        IntPtr certificateContext,
        uint flags,
        IntPtr reserved,
        out IntPtr cryptoProviderOrKeyHandle,
        out int keySpec,
        out bool callerMustFree);

    [DllImport("crypt32.dll", SetLastError = true)]
    private static extern bool CertGetCertificateContextProperty(
        IntPtr certificateContext,
        uint propertyId,
        IntPtr data,
        ref uint size);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CryptReleaseContext(IntPtr cryptoProvider, uint flags);

    [DllImport("ncrypt.dll", SetLastError = true)]
    private static extern int NCryptFreeObject(IntPtr keyHandle);
}
