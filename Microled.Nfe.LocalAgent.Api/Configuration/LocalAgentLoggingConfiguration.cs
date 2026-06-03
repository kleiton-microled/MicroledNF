using Serilog;
using Serilog.Events;

namespace Microled.Nfe.LocalAgent.Api.Configuration;

public static class LocalAgentLoggingConfiguration
{
    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}";

    public static void ConfigureSerilog()
    {
        LocalAgentDataPaths.EnsureDirectoriesExist();

        var logFilePath = Path.Combine(LocalAgentDataPaths.LogsDirectory, "localagent-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: OutputTemplate)
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true,
                outputTemplate: OutputTemplate)
            .CreateLogger();
    }
}
