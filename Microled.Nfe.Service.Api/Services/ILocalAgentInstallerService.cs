namespace Microled.Nfe.Service.Api.Services;

public interface ILocalAgentInstallerService
{
    LocalAgentInstallerInfo? GetInstallerInfo();

    (Stream Stream, string FileName, long Length)? OpenInstallerStream();
}

public sealed class LocalAgentInstallerInfo
{
    public required string DisplayName { get; init; }
    public required string FileName { get; init; }
    public long SizeBytes { get; init; }
    public DateTimeOffset LastModified { get; init; }
    public string DownloadPath { get; init; } = "/api/v1/local-agent/installer";
}
