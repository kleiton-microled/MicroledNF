using System.Diagnostics;

namespace Microled.Nfe.DesktopLauncher.Services;

public sealed class ChildProcessHost : IDisposable
{
    private Process? _process;

    public bool IsRunning => _process is { HasExited: false };

    public void Start(
        string exePath,
        string arguments,
        IDictionary<string, string> environment,
        Action<string> log)
    {
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException("Executável não encontrado: " + exePath, exePath);
        }

        Stop();
        var start = HealthProbe.Hidden(exePath, arguments, Path.GetDirectoryName(exePath)!);
        foreach (var pair in environment)
        {
            start.Environment[pair.Key] = pair.Value;
        }

        var process = Process.Start(start)
            ?? throw new InvalidOperationException("Não foi possível iniciar " + Path.GetFileName(exePath));
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                log(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                log(e.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _process = process;
        log("Iniciado: " + Path.GetFileName(exePath));
    }

    public void Stop()
    {
        var process = _process;
        _process = null;
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(8000);
            }
        }
        catch
        {
            // process already gone
        }
        finally
        {
            process.Dispose();
        }
    }

    public void Dispose() => Stop();
}
