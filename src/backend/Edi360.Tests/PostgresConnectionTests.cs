using Edi360.Api.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Edi360.Tests;

public class PostgresConnectionTests
{
    [Fact]
    public void NeonUriBecomesKeywordConnectionString()
    {
        var cs = DatabaseHosting.NormalizePostgres(
            "postgres://neondb_owner:s3cret@ep-example.region.aws.neon.tech/neondb?sslmode=require&channel_binding=require");
        var builder = new NpgsqlConnectionStringBuilder(cs);
        Assert.Equal("ep-example.region.aws.neon.tech", builder.Host);
        Assert.Equal("neondb", builder.Database);
        Assert.Equal("neondb_owner", builder.Username);
        Assert.Equal("s3cret", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.DoesNotContain("channel_binding", cs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DevelopmentKeepsSqliteEvenWithDatabaseUrl()
    {
        var previousUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        var previousEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var previousForce = Environment.GetEnvironmentVariable("EDI_USE_POSTGRES");
        try
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", "postgres://user:secret@localhost/db");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("EDI_USE_POSTGRES", null);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Database:Provider"] = "Sqlite" })
                .Build();
            Assert.Equal(DatabaseHosting.Sqlite, DatabaseHosting.ResolveProvider(config));
        }
        finally
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", previousUrl);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousEnv);
            Environment.SetEnvironmentVariable("EDI_USE_POSTGRES", previousForce);
        }
    }
}
