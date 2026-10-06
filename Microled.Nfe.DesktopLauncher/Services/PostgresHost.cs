using System.Diagnostics;
using System.Text;

namespace Microled.Nfe.DesktopLauncher.Services;

public sealed class PostgresHost
{
    public async Task StartAsync(OnPremOptions options, Action<string> log, CancellationToken cancellationToken)
    {
        if (!File.Exists(LauncherPaths.PgCtlExe))
        {
            throw new InvalidOperationException(
                "PostgreSQL portátil não encontrado (pgsql\\bin\\pg_ctl.exe). Execute o script Download-PortablePostgres.ps1 no pacote.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(LauncherPaths.PostgresLogFile)!);

        if (!IsClusterInitialized())
        {
            log("Preparando o banco de dados local na primeira execução...");
            InitCluster(options, log);
        }

        PatchConfig(options);
        if (HealthProbe.TcpOpen("127.0.0.1", options.PostgresPort))
        {
            log("Banco local já estava em execução.");
        }
        else
        {
            log("Iniciando PostgreSQL...");
            RunTool(
                LauncherPaths.PgCtlExe,
                $"-D \"{LauncherPaths.PostgresDataDir}\" -l \"{LauncherPaths.PostgresLogFile}\" start",
                Path.GetDirectoryName(LauncherPaths.PgCtlExe)!,
                log);
        }

        await WaitReadyAsync(options, log, cancellationToken);
        EnsureRoleAndDatabase(options, log);
    }

    public void Stop(OnPremOptions options, Action<string> log)
    {
        if (!File.Exists(LauncherPaths.PgCtlExe) || !IsClusterInitialized())
        {
            return;
        }

        try
        {
            log("Parando PostgreSQL...");
            RunTool(
                LauncherPaths.PgCtlExe,
                $"-D \"{LauncherPaths.PostgresDataDir}\" stop -m fast",
                Path.GetDirectoryName(LauncherPaths.PgCtlExe)!,
                log);
        }
        catch (Exception ex)
        {
            log("Não foi possível parar o PostgreSQL: " + ex.Message);
        }
    }

    private static bool IsClusterInitialized()
    {
        return File.Exists(Path.Combine(LauncherPaths.PostgresDataDir, "PG_VERSION"));
    }

    private static void InitCluster(OnPremOptions options, Action<string> log)
    {
        Directory.CreateDirectory(LauncherPaths.PostgresDataDir);
        var pwFile = Path.Combine(Path.GetTempPath(), "microled-pg-pw.txt");
        File.WriteAllText(pwFile, options.PostgresPassword, Encoding.ASCII);
        try
        {
            RunTool(
                LauncherPaths.InitDbExe,
                $"-D \"{LauncherPaths.PostgresDataDir}\" -U {options.PostgresUser} -A scram-sha-256 --pwfile=\"{pwFile}\" -E UTF8 --no-locale",
                Path.GetDirectoryName(LauncherPaths.InitDbExe)!,
                log);
        }
        finally
        {
            try { File.Delete(pwFile); } catch { /* ignore */ }
        }
    }

    private static void PatchConfig(OnPremOptions options)
    {
        var confPath = Path.Combine(LauncherPaths.PostgresDataDir, "postgresql.conf");
        if (!File.Exists(confPath))
        {
            return;
        }

        var lines = File.ReadAllLines(confPath).ToList();
        SetOrReplace(lines, "listen_addresses", "'127.0.0.1'");
        SetOrReplace(lines, "port", options.PostgresPort.ToString());
        File.WriteAllLines(confPath, lines);

        var hbaPath = Path.Combine(LauncherPaths.PostgresDataDir, "pg_hba.conf");
        if (!File.Exists(hbaPath))
        {
            return;
        }

        var hba = File.ReadAllText(hbaPath);
        if (!hba.Contains("127.0.0.1/32", StringComparison.Ordinal))
        {
            File.AppendAllText(
                hbaPath,
                Environment.NewLine + "host all all 127.0.0.1/32 scram-sha-256" + Environment.NewLine);
        }
    }

    private static void SetOrReplace(List<string> lines, string key, string value)
    {
        var prefix = key + " =";
        var found = false;
        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith(key + " =", StringComparison.Ordinal) ||
                trimmed.StartsWith("#" + key + " =", StringComparison.Ordinal) ||
                trimmed.StartsWith("#" + key + "=", StringComparison.Ordinal))
            {
                lines[i] = $"{prefix} {value}";
                found = true;
                break;
            }
        }

        if (!found)
        {
            lines.Add($"{prefix} {value}");
        }
    }

    private static async Task WaitReadyAsync(OnPremOptions options, Action<string> log, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 40; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(LauncherPaths.PgIsReadyExe))
            {
                var code = RunToolExitCode(
                    LauncherPaths.PgIsReadyExe,
                    $"-h 127.0.0.1 -p {options.PostgresPort} -U {options.PostgresUser}",
                    Path.GetDirectoryName(LauncherPaths.PgIsReadyExe)!);
                if (code == 0)
                {
                    return;
                }
            }
            else if (HealthProbe.TcpOpen("127.0.0.1", options.PostgresPort))
            {
                return;
            }

            await Task.Delay(500, cancellationToken);
        }

        log("O PostgreSQL não respondeu a tempo. Veja o log em " + LauncherPaths.PostgresLogFile);
        throw new TimeoutException("PostgreSQL não ficou pronto.");
    }

    private static void EnsureRoleAndDatabase(OnPremOptions options, Action<string> log)
    {
        if (!File.Exists(LauncherPaths.PsqlExe))
        {
            return;
        }

        var existsSql =
            $"SELECT 1 FROM pg_database WHERE datname = '{options.PostgresDatabase.Replace("'", "''")}';";
        var output = RunToolCapture(
            LauncherPaths.PsqlExe,
            $"-h 127.0.0.1 -p {options.PostgresPort} -U {options.PostgresUser} -d postgres -tAc \"{existsSql}\"",
            options.PostgresPassword);

        if (output.Contains('1', StringComparison.Ordinal))
        {
            return;
        }

        log($"Criando banco {options.PostgresDatabase}...");
        RunToolCapture(
            LauncherPaths.PsqlExe,
            $"-h 127.0.0.1 -p {options.PostgresPort} -U {options.PostgresUser} -d postgres -c \"CREATE DATABASE \\\"{options.PostgresDatabase}\\\";\"",
            options.PostgresPassword);
    }

    private static void RunTool(string fileName, string arguments, string workingDirectory, Action<string> log)
    {
        var start = HealthProbe.Hidden(fileName, arguments, workingDirectory);
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Falha ao iniciar " + Path.GetFileName(fileName));
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (!string.IsNullOrWhiteSpace(stdOut))
        {
            log(stdOut.Trim());
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{Path.GetFileName(fileName)} falhou ({process.ExitCode}): {stdErr}".Trim());
        }
    }

    private static int RunToolExitCode(string fileName, string arguments, string workingDirectory)
    {
        var start = HealthProbe.Hidden(fileName, arguments, workingDirectory);
        using var process = Process.Start(start);
        if (process is null)
        {
            return -1;
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    private static string RunToolCapture(string fileName, string arguments, string password)
    {
        var start = HealthProbe.Hidden(fileName, arguments, Path.GetDirectoryName(fileName)!);
        start.Environment["PGPASSWORD"] = password;
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Falha ao iniciar " + Path.GetFileName(fileName));
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{Path.GetFileName(fileName)} falhou ({process.ExitCode}): {stdErr} {stdOut}".Trim());
        }

        return stdOut;
    }
}
