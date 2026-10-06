using System.Diagnostics;

namespace Microled.Nfe.DesktopLauncher.Services;

public sealed class StackOrchestrator : IDisposable
{
    private readonly PostgresHost _postgres = new();
    private readonly ChildProcessHost _api = new();
    private readonly ChildProcessHost _agent = new();
    private CancellationTokenSource? _startCts;

    public bool PostgresUp { get; private set; }

    public bool ApiUp { get; private set; }

    public bool AgentUp { get; private set; }

    public bool FrontUp => ApiUp;

    public async Task StartAsync(OnPremOptions options, Action<string> log, CancellationToken cancellationToken)
    {
        _startCts?.Cancel();
        _startCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _startCts.Token;

        AgentUserSettingsWriter.WriteAccessAndApi(options.AccessDatabasePath, options.ApiUrl);
        _api.Stop();
        _agent.Stop();
        OccupiedPort.Release(5249, "Microled.Nfe.Service.Api", log);
        OccupiedPort.Release(5278, "Microled.Nfe.LocalAgent.Api", log);

        await _postgres.StartAsync(options, log, token);
        PostgresUp = true;

        log("Iniciando API...");
        _api.Start(
            LauncherPaths.ApiExe,
            "--urls http://localhost:5249",
            new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "OnPrem",
                ["ASPNETCORE_URLS"] = "http://localhost:5249",
                ["ConnectionStrings__NfeDatabase"] = options.ConnectionString
            },
            log);

        await WaitUntil(() => HealthProbe.HttpOkAsync(LauncherPaths.ApiHealthUrl, token), token, "API");
        ApiUp = true;

        log("Iniciando agente de comunicação...");
        _agent.Start(
            LauncherPaths.AgentExe,
            string.Empty,
            new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            },
            log);

        await WaitUntil(() => HealthProbe.HttpOkAsync(LauncherPaths.AgentHealthUrl, token), token, "agente");
        AgentUp = true;
        log("Ambiente pronto. Use Abrir sistema.");
    }

    public void Stop(Action<string> log)
    {
        _startCts?.Cancel();
        log("Parando agente...");
        _agent.Stop();
        AgentUp = false;
        log("Parando API...");
        _api.Stop();
        ApiUp = false;
    }

    public Task RefreshHealthAsync(OnPremOptions options, CancellationToken cancellationToken)
    {
        PostgresUp = HealthProbe.TcpOpen("127.0.0.1", options.PostgresPort);
        ApiUp = _api.IsRunning;
        AgentUp = _agent.IsRunning;
        return Task.CompletedTask;
    }

    public static void OpenFront()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = LauncherPaths.FrontUrl,
            UseShellExecute = true
        });
    }

    private static async Task WaitUntil(Func<Task<bool>> probe, CancellationToken cancellationToken, string name)
    {
        for (var i = 0; i < 60; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await probe())
            {
                return;
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException($"O serviço {name} não respondeu. Veja o log acima.");
    }

    public void Dispose()
    {
        _startCts?.Cancel();
        _startCts?.Dispose();
        _api.Dispose();
        _agent.Dispose();
    }
}
