namespace Microled.Nfe.DesktopLauncher.Services;

public static class LauncherPaths
{
    public static string InstallRoot
    {
        get
        {
            var fromEnv = Environment.GetEnvironmentVariable("MICROLED_ONPREM_ROOT");
            if (!string.IsNullOrWhiteSpace(fromEnv) && Directory.Exists(fromEnv))
            {
                return Path.GetFullPath(fromEnv);
            }

            var baseDir = AppContext.BaseDirectory;
            if (File.Exists(Path.Combine(baseDir, "api", "Microled.Nfe.Service.Api.exe"))
                || File.Exists(Path.Combine(baseDir, "pgsql", "bin", "pg_ctl.exe")))
            {
                return baseDir;
            }

            var dist = FindAncestorFile(baseDir, "dist", "onprem-package");
            return dist ?? baseDir;
        }
    }

    public static string ApiExe => Path.Combine(InstallRoot, "api", "Microled.Nfe.Service.Api.exe");

    public static string AgentExe => Path.Combine(InstallRoot, "agent", "Microled.Nfe.LocalAgent.Api.exe");

    public static string PgCtlExe => Path.Combine(InstallRoot, "pgsql", "bin", "pg_ctl.exe");

    public static string InitDbExe => Path.Combine(InstallRoot, "pgsql", "bin", "initdb.exe");

    public static string PsqlExe => Path.Combine(InstallRoot, "pgsql", "bin", "psql.exe");

    public static string PgIsReadyExe => Path.Combine(InstallRoot, "pgsql", "bin", "pg_isready.exe");

    public static string ProgramDataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Microled",
        "Nfe");

    public static string LocalAppDataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microled",
        "Nfe");

    private static string? _dataRoot;

    public static string DataRoot => _dataRoot ??= ResolveDataRoot();

    private static string ResolveDataRoot()
    {
        var programDataLauncher = Path.Combine(ProgramDataRoot, "launcher");
        return JsonFile.CanWriteDirectory(programDataLauncher) ? ProgramDataRoot : LocalAppDataRoot;
    }

    public static string LauncherDataDir => Path.Combine(DataRoot, "launcher");

    public static string OnPremOptionsFile => Path.Combine(LauncherDataDir, "onprem.json");

    public static string PostgresDataDir => Path.Combine(DataRoot, "postgres", "data");

    public static string PostgresLogFile => Path.Combine(DataRoot, "postgres", "postgres.log");

    public static string AgentSettingsFile => Path.Combine(DataRoot, "localagent", "settings.json");

    public static string FrontUrl => "http://localhost:5249/";

    public static string ApiHealthUrl => "http://localhost:5249/health/database";

    public static string AgentHealthUrl => "http://localhost:5278/api/local/health";

    public static void EnsureDataDirectories()
    {
        try
        {
            Directory.CreateDirectory(LauncherDataDir);
            Directory.CreateDirectory(Path.Combine(DataRoot, "postgres"));
            Directory.CreateDirectory(Path.Combine(DataRoot, "localagent"));
            Directory.CreateDirectory(Path.Combine(DataRoot, "localagent", "logs"));
            Directory.CreateDirectory(Path.Combine(DataRoot, "localagent", "RpsOut"));
            Directory.CreateDirectory(Path.Combine(DataRoot, "localagent", "Validate"));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _dataRoot = LocalAppDataRoot;
            Directory.CreateDirectory(LauncherDataDir);
            Directory.CreateDirectory(Path.Combine(DataRoot, "postgres"));
            Directory.CreateDirectory(Path.Combine(DataRoot, "localagent"));
        }
    }

    private static string? FindAncestorFile(string start, params string[] relativeParts)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(relativeParts).ToArray());
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
