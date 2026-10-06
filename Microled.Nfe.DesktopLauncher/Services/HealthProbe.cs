using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;

namespace Microled.Nfe.DesktopLauncher.Services;

public static class HealthProbe
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    public static async Task<bool> HttpOkAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Http.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static bool TcpOpen(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync(host, port);
            return task.Wait(TimeSpan.FromMilliseconds(400)) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    public static ProcessStartInfo Hidden(string fileName, string arguments, string workingDirectory)
    {
        return new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }
}
