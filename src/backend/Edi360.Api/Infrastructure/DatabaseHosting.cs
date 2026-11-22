using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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
        if (!string.IsNullOrWhiteSpace(PostgresUrl(config))) return Postgres;
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
        var value = url.Trim();
        if (value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            value = "postgresql://" + value["postgres://".Length..];
        if (value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            if (!value.Contains("sslmode=", StringComparison.OrdinalIgnoreCase))
                value += value.Contains('?') ? "&sslmode=require" : "?sslmode=require";
            return value;
        }
        if (!value.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
            value += ";SSL Mode=Require;Trust Server Certificate=true";
        return value;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
