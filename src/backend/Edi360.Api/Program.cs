using System.Text.Json;
using Edi360.Api.Application;
using Edi360.Api.Data;
using Edi360.Api.Data.Seed;
using Edi360.Api.Domain;
using Edi360.Api.Infrastructure;
using Edi360.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var paths = new AppPaths(builder.Environment);
builder.Services.AddSingleton(paths);

var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
builder.Services.AddDbContext<EdiDbContext>(o =>
{
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    else
        o.UseSqlite(BuildSqlite(paths.EdiDb).ToString());
});
builder.Services.AddDbContext<LegacyDbContext>(o =>
{
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerLegacy"));
    else
        o.UseSqlite(BuildSqlite(paths.LegacyDb).ToString());
});

builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<ICurrentUserAccessor>(sp => sp.GetRequiredService<CurrentUserAccessor>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IBusinessClock, BusinessClock>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<EventPublisher>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<LocalAssistanceEngine>();
builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddScoped<DocumentalService>();
builder.Services.AddScoped<WorkService>();
builder.Services.AddScoped<CaseService>();
builder.Services.AddScoped<SocietyService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<ThirdPartyService>();
builder.Services.AddScoped<AuxiliaryService>();
builder.Services.AddScoped<FinancialService>();
builder.Services.AddScoped<MigrationService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<DemoService>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddScoped<IntegrationService>();
builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddCors(o => o.AddPolicy("local", p => p
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseCors("local");
app.Use(async (ctx, next) =>
{
    try
    {
        var accessor = ctx.RequestServices.GetRequiredService<CurrentUserAccessor>();
        if (ctx.Request.Cookies.TryGetValue(AuthService.CookieName, out var sid) && Guid.TryParse(sid, out var id))
        {
            var auth = ctx.RequestServices.GetRequiredService<AuthService>();
            accessor.User = await auth.ResolveAsync(id, ctx.RequestAborted);
        }
        await next();
    }
    catch (UnauthorizedAccessException ex)
    {
        ctx.Response.StatusCode = 403;
        await ctx.Response.WriteAsJsonAsync(new { mensaje = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsJsonAsync(new { mensaje = ex.Message });
    }
    catch (KeyNotFoundException ex)
    {
        ctx.Response.StatusCode = 404;
        await ctx.Response.WriteAsJsonAsync(new { mensaje = ex.Message });
    }
});

app.MapControllers();
app.MapGet("/api/salud", () => Results.Ok(new { estado = "ok", nombre = "EDI 360" }));

using (var scope = app.Services.CreateScope())
{
    var edi = scope.ServiceProvider.GetRequiredService<EdiDbContext>();
    var legacy = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();
    await edi.Database.EnsureCreatedAsync();
    await legacy.Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<DemoSeeder>().EnsureAsync();
}

app.Run();

static SqliteConnectionStringBuilder BuildSqlite(string file) => new()
{
    DataSource = file,
    Mode = SqliteOpenMode.ReadWriteCreate,
    Cache = SqliteCacheMode.Shared
};

public partial class Program;
