using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Microled.Nfe.DesktopLauncher.Services;

public static class OccupiedPort
{
    public static void Release(int port, string processName, Action<string> log)
    {
        var pids = new HashSet<int>(FindListeningPids(port));
        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                pids.Add(process.Id);
            }
            finally
            {
                process.Dispose();
            }
        }

        foreach (var pid in pids)
        {
            KillPid(pid, port, log);
        }

        for (var i = 0; i < 20; i++)
        {
            if (!HealthProbe.TcpOpen("127.0.0.1", port) && FindListeningPids(port).Count == 0)
            {
                return;
            }

            Thread.Sleep(200);
        }

        if (HealthProbe.TcpOpen("127.0.0.1", port) || FindListeningPids(port).Count > 0)
        {
            throw new InvalidOperationException(
                $"A porta {port} continua em uso. Feche o processo manualmente e tente de novo.");
        }
    }

    private static IReadOnlyCollection<int> FindListeningPids(int port)
    {
        var output = RunNetstat();
        var pids = new HashSet<int>();
        foreach (var raw in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0 || !line.Contains("LISTENING", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = Regex.Split(line, @"\s+");
            if (parts.Length < 4)
            {
                continue;
            }

            if (!LocalAddressUsesPort(parts[1], port))
            {
                continue;
            }

            if (int.TryParse(parts[^1], out var pid) && pid > 0)
            {
                pids.Add(pid);
            }
        }

        return pids;
    }

    private static bool LocalAddressUsesPort(string localAddress, int port)
    {
        var colon = localAddress.LastIndexOf(':');
        if (colon < 0 || colon == localAddress.Length - 1)
        {
            return false;
        }

        return int.TryParse(localAddress[(colon + 1)..], out var value) && value == port;
    }

    private static string RunNetstat()
    {
        var start = HealthProbe.Hidden("netstat.exe", "-ano -p tcp", Environment.SystemDirectory);
        start.RedirectStandardError = false;
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Não foi possível consultar as portas em uso (netstat).");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(5000);
        return output;
    }

    private static void KillPid(int pid, int port, Action<string> log)
    {
        if (pid <= 4 || pid == Environment.ProcessId)
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(pid);
            var name = process.ProcessName;
            log($"Porta {port} já estava em uso por {name} (PID {pid}). Encerrando o processo anterior...");
            process.Kill(entireProcessTree: true);
            process.WaitForExit(8000);
            log($"Processo anterior encerrado (PID {pid}).");
        }
        catch (ArgumentException)
        {
            // already gone
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Não foi possível encerrar o processo na porta {port} (PID {pid}): {ex.Message}",
                ex);
        }
    }
}
