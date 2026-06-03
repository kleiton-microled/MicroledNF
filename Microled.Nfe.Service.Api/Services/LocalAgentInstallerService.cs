using Microled.Nfe.Service.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Microled.Nfe.Service.Api.Services;

public sealed class LocalAgentInstallerService : ILocalAgentInstallerService
{
    private readonly LocalAgentInstallerOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalAgentInstallerService> _logger;

    public LocalAgentInstallerService(
        IOptions<LocalAgentInstallerOptions> options,
        IWebHostEnvironment environment,
        ILogger<LocalAgentInstallerService> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public LocalAgentInstallerInfo? GetInstallerInfo()
    {
        var file = ResolveInstallerFile();
        if (file is null)
        {
            return null;
        }

        return new LocalAgentInstallerInfo
        {
            DisplayName = _options.DisplayName,
            FileName = file.Name,
            SizeBytes = file.Length,
            LastModified = file.LastWriteTimeUtc
        };
    }

    public (Stream Stream, string FileName, long Length)? OpenInstallerStream()
    {
        var file = ResolveInstallerFile();
        if (file is null)
        {
            return null;
        }

        var stream = new FileStream(
            file.FullName,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return (stream, file.Name, file.Length);
    }

    private FileInfo? ResolveInstallerFile()
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Local Agent installer download is disabled.");
            return null;
        }

        var directory = ResolveDirectory();
        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("Local Agent installer directory not found: {Directory}", directory);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(_options.FileName))
        {
            var exactPath = Path.Combine(directory, _options.FileName);
            if (!File.Exists(exactPath))
            {
                _logger.LogWarning("Local Agent installer file not found: {Path}", exactPath);
                return null;
            }

            return new FileInfo(exactPath);
        }

        var pattern = string.IsNullOrWhiteSpace(_options.FileNamePattern)
            ? "Microled-NFe-LocalAgent-*.exe"
            : _options.FileNamePattern;

        var candidates = Directory
            .EnumerateFiles(directory, pattern)
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();

        if (candidates.Count == 0)
        {
            _logger.LogWarning(
                "No Local Agent installer matching {Pattern} in {Directory}",
                pattern,
                directory);
            return null;
        }

        return candidates[0];
    }

    private string ResolveDirectory()
    {
        var configured = _options.Directory.Trim();
        if (Path.IsPathRooted(configured))
        {
            return configured;
        }

        return Path.Combine(_environment.ContentRootPath, configured);
    }
}
