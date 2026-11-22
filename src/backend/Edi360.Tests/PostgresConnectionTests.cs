using Edi360.Api.Infrastructure;
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
}
