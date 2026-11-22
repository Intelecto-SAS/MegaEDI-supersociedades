using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Edi360.Api.Infrastructure;

public static class DatabaseHosting
{
    public const string Sqlite = "Sqlite";
    public const string Postgres = "Postgres";
    public const string SqlServer = "SqlServer";

    public static string? PostgresUrl(IConfiguration config) =>
        FirstNonEmpty(
            Environment.GetEnvironmentVariable("POSTGRES_URL"),
            Environment.GetEnvironmentVariable("DATABASE_URL"),
            config.GetConnectionString("Postgres"));

    public static string ResolveProvider(IConfiguration config)
    {
        // En el equipo, una DATABASE_URL de Vercel no debe cambiar la demo local.
        var environment = config["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var localSqlite = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase)
            && Environment.GetEnvironmentVariable("EDI_USE_POSTGRES") != "1";
        if (!localSqlite && !string.IsNullOrWhiteSpace(PostgresUrl(config))) return Postgres;
        var configured = config["Database:Provider"];
        return string.IsNullOrWhiteSpace(configured) ? Sqlite : configured;
    }

    public static bool IsDurable(IConfiguration config) =>
        ResolveProvider(config) is Postgres or SqlServer;

    public static void ConfigureEdi(DbContextOptionsBuilder o, IConfiguration config, AppPaths paths)
    {
        var provider = ResolveProvider(config);
        if (provider.Equals(SqlServer, StringComparison.OrdinalIgnoreCase))
            o.UseSqlServer(config.GetConnectionString("SqlServer"));
        else if (provider.Equals(Postgres, StringComparison.OrdinalIgnoreCase))
            o.UseNpgsql(NormalizePostgres(PostgresUrl(config)!), x => x.MigrationsHistoryTable("__EFMigrationsHistory", "edi"));
        else
            o.UseSqlite(BuildSqlite(paths.EdiDb).ToString());
    }

    public static void ConfigureLegacy(DbContextOptionsBuilder o, IConfiguration config, AppPaths paths)
    {
        var provider = ResolveProvider(config);
        if (provider.Equals(SqlServer, StringComparison.OrdinalIgnoreCase))
            o.UseSqlServer(config.GetConnectionString("SqlServerLegacy"));
        else if (provider.Equals(Postgres, StringComparison.OrdinalIgnoreCase))
        {
            var url = FirstNonEmpty(
                Environment.GetEnvironmentVariable("DATABASE_URL_LEGACY"),
                PostgresUrl(config))!;
            o.UseNpgsql(NormalizePostgres(url), x => x.MigrationsHistoryTable("__EFMigrationsHistory", "gedess"));
        }
        else
            o.UseSqlite(BuildSqlite(paths.LegacyDb).ToString());
    }

    public static SqliteConnectionStringBuilder BuildSqlite(string file) => new()
    {
        DataSource = file,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared
    };

    public static string NormalizePostgres(string url)
    {
        var value = url.Trim().Trim('"');
        if (value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return FromPostgresUri(value);

        if (!value.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
            value += ";SSL Mode=Require;Trust Server Certificate=true";
        return value;
    }

    // Npgsql 10 no acepta postgres:// en el connection string: lanza
    // "Couldn't set postgres://..." y el proceso aborta al abrir la base.
    private static string FromPostgresUri(string url)
    {
        var normalized = url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            ? "postgresql://" + url["postgres://".Length..]
            : url;
        var uri = new Uri(normalized);
        var userInfo = Uri.UnescapeDataString(uri.UserInfo);
        var colon = userInfo.IndexOf(':');
        var username = colon >= 0 ? userInfo[..colon] : userInfo;
        var password = colon >= 0 ? userInfo[(colon + 1)..] : "";
        var database = uri.AbsolutePath.Trim('/');

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Username = username,
            Password = password,
            Database = string.IsNullOrEmpty(database) ? "postgres" : database,
            SslMode = SslMode.Require
        };
        if (uri.Port > 0)
            builder.Port = uri.Port;

        var query = ParseQuery(uri.Query);
        if (query.TryGetValue("sslmode", out var ssl) && Enum.TryParse<SslMode>(ssl, true, out var mode))
            builder.SslMode = mode;
        return builder.ConnectionString;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var raw = query.TrimStart('?');
        if (raw.Length == 0) return map;
        foreach (var part in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            var key = Uri.UnescapeDataString(eq >= 0 ? part[..eq] : part);
            var val = Uri.UnescapeDataString(eq >= 0 ? part[(eq + 1)..] : "");
            map[key] = val;
        }
        return map;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
