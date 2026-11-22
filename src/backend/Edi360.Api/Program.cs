using System.Text.Json;
using Edi360.Api.Application;
using Edi360.Api.Data;
using Edi360.Api.Data.Seed;
using Edi360.Api.Infrastructure;
using Edi360.Api.Security;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var paths = new AppPaths(builder.Environment);
builder.Services.AddSingleton(paths);

builder.Services.AddDbContext<EdiDbContext>(o => DatabaseHosting.ConfigureEdi(o, builder.Configuration, paths));
builder.Services.AddDbContext<LegacyDbContext>(o => DatabaseHosting.ConfigureLegacy(o, builder.Configuration, paths));

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<ICurrentUserAccessor>(sp => sp.GetRequiredService<CurrentUserAccessor>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IBusinessClock, BusinessClock>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<EventPublisher>();
builder.Services.AddScoped<CalendarService>();
if (DatabaseHosting.ResolveProvider(builder.Configuration) == DatabaseHosting.Postgres)
    builder.Services.AddScoped<IFileStorage, DatabaseFileStorage>();
else
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
builder.Services.AddSingleton<OutboxProcessor>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OutboxProcessor>());

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddCors(o => o.AddPolicy("local", p => p
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseForwardedHeaders();
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
        if (ctx.Request.Path.StartsWithSegments("/api")
            && !ctx.Request.Path.StartsWithSegments("/api/salud")
            && !ctx.Request.Path.StartsWithSegments("/api/eventos/procesar"))
        {
            var processor = ctx.RequestServices.GetRequiredService<OutboxProcessor>();
            await processor.ProcessOnce(ctx.RequestAborted);
        }
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
app.MapGet("/api/salud", (IConfiguration config) => Results.Ok(new
{
    estado = "ok",
    nombre = "EDI 360",
    persistencia = DatabaseHosting.IsDurable(config) ? "compartida" : "local"
}));

using (var scope = app.Services.CreateScope())
{
    var edi = scope.ServiceProvider.GetRequiredService<EdiDbContext>();
    var legacy = scope.ServiceProvider.GetRequiredService<LegacyDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<DemoSeeder>();
    await PrepareDatabaseAsync(edi, legacy, seeder);
}

app.Run();

static async Task PrepareDatabaseAsync(EdiDbContext edi, LegacyDbContext legacy, DemoSeeder seeder)
{
    if (edi.Database.IsNpgsql())
    {
        await edi.Database.OpenConnectionAsync();
        try
        {
            await edi.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(360360)");
            await edi.Database.EnsureCreatedAsync();
            await legacy.Database.EnsureCreatedAsync();
            await seeder.EnsureAsync();
        }
        finally
        {
            try { await edi.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(360360)"); }
            catch { /* ignore unlock on shutdown */ }
        }
        return;
    }

    await edi.Database.EnsureCreatedAsync();
    await legacy.Database.EnsureCreatedAsync();
    await seeder.EnsureAsync();
}

public partial class Program;
