using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Edi360.Api.Data;
using Edi360.Api.Domain;
using Edi360.Api.Infrastructure;
using Edi360.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Edi360.Api.Application;

public interface IBusinessClock
{
    DateTimeOffset Now { get; }
    Task AdvanceDaysAsync(int days, CancellationToken ct);
}

public sealed class BusinessClock(EdiDbContext db) : IBusinessClock
{
    public DateTimeOffset Now
    {
        get
        {
            var state = db.DemoStates.AsNoTracking().FirstOrDefault();
            return state?.BusinessClock ?? DateTimeOffset.Now;
        }
    }

    public async Task AdvanceDaysAsync(int days, CancellationToken ct)
    {
        var state = await db.DemoStates.FirstAsync(ct);
        state.BusinessClock = state.BusinessClock.AddDays(days);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class AuditService(EdiDbContext db, IBusinessClock clock)
{
    public async Task WriteAsync(CurrentUser? user, string module, string action, string entityType, Guid? entityId, string detail, string result = "Ok", CancellationToken ct = default)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAt = clock.Now,
            UserId = user?.UserId,
            UserName = user?.DisplayName ?? "sistema",
            Action = action,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail,
            Result = result
        });
        await db.SaveChangesAsync(ct);
    }
}

public sealed class EventPublisher(EdiDbContext db, IBusinessClock clock)
{
    public async Task PublishAsync(string type, string source, string relatedType, Guid relatedId, object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload);
        var evt = new DomainEventLog
        {
            Id = Guid.NewGuid(),
            Type = type,
            Version = "1",
            Source = source,
            RelatedType = relatedType,
            RelatedId = relatedId,
            Payload = json,
            OccurredAt = clock.Now,
            ProcessingStatus = "Pendiente"
        };
        db.DomainEvents.Add(evt);
        db.Outbox.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Type = type,
            Payload = json,
            CreatedAt = clock.Now,
            Status = "Pendiente"
        });
        await db.SaveChangesAsync(ct);
    }
}

public sealed class CalendarService(EdiDbContext db)
{
    public DateTimeOffset AddBusinessDays(DateTimeOffset start, int days)
    {
        var holidays = db.Holidays.AsNoTracking().Select(h => h.Date).ToHashSet();
        var date = start;
        var added = 0;
        while (added < days)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (holidays.Contains(DateOnly.FromDateTime(date.DateTime))) continue;
            added++;
        }
        return date;
    }
}

public interface IFileStorage
{
    Task<(string path, string sha, long size)> SaveAsync(Stream content, string relativeDir, string fileName, CancellationToken ct);
    Task<Stream> OpenAsync(string path, CancellationToken ct);
    string ComputeHash(byte[] bytes);
}

public sealed class LocalFileStorage(AppPaths paths) : IFileStorage
{
    public async Task<(string path, string sha, long size)> SaveAsync(Stream content, string relativeDir, string fileName, CancellationToken ct)
    {
        var dir = Path.Combine(paths.Storage, relativeDir);
        Directory.CreateDirectory(dir);
        var full = Path.Combine(dir, fileName);
        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, ct);
        await fs.FlushAsync(ct);
        var bytes = await File.ReadAllBytesAsync(full, ct);
        return (full, ComputeHash(bytes), bytes.LongLength);
    }

    public Task<Stream> OpenAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("El documento no está disponible.", path);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public string ComputeHash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}

public sealed class LocalAssistanceEngine
{
    public const string Banner = "Motor local de asistencia — simulación basada en reglas y plantillas";

    public (string code, string name, string departmentCode, string priority, bool tutela, string explanation) Classify(string subject, string body)
    {
        var text = $"{subject} {body}".ToLowerInvariant();
        foreach (var rule in Rules)
        {
            if (rule.Keywords.Any(k => text.Contains(k)))
                return (rule.Code, rule.Name, rule.Department, rule.Priority, rule.Tutela,
                    $"Se sugirió «{rule.Name}» porque el texto incluye: {string.Join(", ", rule.Keywords.Where(text.Contains))}.");
        }
        return ("OTROS", "Comunicación general", "GD", "Media", false,
            "No se encontró una palabra clave del catálogo. Se sugiere clasificación general para confirmación humana.");
    }

    public string SummarizeCase(CaseFile c, IEnumerable<LegalAction> actions, IEnumerable<StoredDocument> docs)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Expediente {c.Number} — {c.Title}.");
        sb.AppendLine($"Estado: {c.Status}. Etapa: {c.Stage}. Prioridad: {c.Priority}.");
        if (c.DueAt is not null) sb.AppendLine($"Plazo: {c.DueAt:yyyy-MM-dd}.");
        sb.AppendLine($"Actuaciones registradas: {actions.Count()}.");
        foreach (var a in actions.OrderByDescending(x => x.OccurredAt).Take(5))
            sb.AppendLine($"- {a.OccurredAt:yyyy-MM-dd}: {a.Title} ({a.Type}).");
        sb.AppendLine($"Documentos: {docs.Count()}.");
        sb.AppendLine("Este resumen se arma con los registros del expediente. No es un modelo generativo.");
        return sb.ToString();
    }

    private static readonly ClassifyRule[] Rules =
    [
        new("TUTELA", "Tutela", "OAJ", "Crítica", true, ["tutela", "amparo", "acción de tutela", "fallo de tutela"]),
        new("INSOLV", "Insolvencia — reorganización o liquidación", "DPI", "Alta", false, ["reorganización", "reorganizacion", "liquidación judicial", "liquidacion judicial", "insolvencia", "acuerdo de reorganización"]),
        new("REQJUD", "Requerimiento judicial", "OAJ", "Alta", false, ["juzgado", "despacho judicial", "oficio judicial", "requerimiento del juez"]),
        new("PQRS", "PQRSD", "REC", "Media", false, ["petición", "queja", "reclamo", "sugerencia", "denuncia", "acceso a la información"]),
        new("FINREP", "Informe financiero / estados financieros", "IEE", "Media", false, ["estados financieros", "xbrl", "notas a los estados", "dictamen del revisor"]),
        new("CONTR", "Contribución / cartera", "CAR", "Media", false, ["contribución", "cartera", "pago de contribución", "factura de contribución"]),
        new("AUX", "Auxiliares de la justicia", "GRE", "Media", false, ["auxiliar de la justicia", "promotor", "liquidador", "interventor"]),
        new("CONT", "Contratación", "GCO", "Media", false, ["contrato", "póliza", "ops", "acta de inicio"]),
        new("SOC", "Trámite societario", "DAES", "Media", false, ["reforma estatutaria", "fusión", "escisión", "disminución de capital"])
    ];

    private sealed record ClassifyRule(string Code, string Name, string Department, string Priority, bool Tutela, string[] Keywords);
}

public sealed class OutboxProcessor(IServiceProvider sp, ILogger<OutboxProcessor> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessOnce(stoppingToken); }
            catch (Exception ex) { log.LogWarning(ex, "Procesamiento de eventos"); }
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    public async Task ProcessOnce(CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EdiDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IBusinessClock>();
        var pending = await db.Outbox.Where(x => x.Status == "Pendiente" || x.Status == "Reintento")
            .OrderBy(x => x.CreatedAt).Take(20).ToListAsync(ct);
        foreach (var msg in pending)
        {
            try
            {
                await Consume(db, clock, msg, ct);
                msg.Status = "Procesado";
                msg.ProcessedAt = clock.Now;
                msg.Attempts++;
                var evt = await db.DomainEvents.FirstOrDefaultAsync(e => e.Id == msg.EventId, ct);
                if (evt is not null) evt.ProcessingStatus = "Procesado";
            }
            catch (Exception ex)
            {
                msg.Attempts++;
                msg.LastError = ex.Message;
                msg.Status = msg.Attempts >= 5 ? "Fallido" : "Reintento";
            }
        }
        if (pending.Count > 0) await db.SaveChangesAsync(ct);
    }

    private static async Task Consume(EdiDbContext db, IBusinessClock clock, OutboxMessage msg, CancellationToken ct)
    {
        async Task Deliver(string consumer, Func<Task> action)
        {
            var existing = await db.EventDeliveries.AnyAsync(d => d.EventId == msg.EventId && d.Consumer == consumer && d.Status == "Entregado", ct);
            if (existing)
            {
                db.EventDeliveries.Add(new EventDelivery
                {
                    Id = Guid.NewGuid(), EventId = msg.EventId, Consumer = consumer,
                    Status = "Omitido (ya aplicado)", AttemptedAt = clock.Now
                });
                return;
            }
            await action();
            db.EventDeliveries.Add(new EventDelivery
            {
                Id = Guid.NewGuid(), EventId = msg.EventId, Consumer = consumer,
                Status = "Entregado", AttemptedAt = clock.Now
            });
        }

        if (msg.Type == EventTypes.AcuerdoConfirmado)
        {
            await Deliver("D1.Sociedad", async () =>
            {
                var evt = await db.DomainEvents.FirstAsync(e => e.Id == msg.EventId, ct);
                var caso = await db.Cases.Include(c => c.Society).FirstOrDefaultAsync(c => c.Id == evt.RelatedId, ct);
                if (caso?.Society is null) return;
                caso.Society.Situation = "Acuerdo en ejecución";
                caso.Society.Status = "En reorganización";
                caso.Society.Notes = "Actualizado por confirmación de acuerdo. No se declara recuperación automática.";
            });
            await Deliver("D6.Alertas", async () =>
            {
                var evt = await db.DomainEvents.FirstAsync(e => e.Id == msg.EventId, ct);
                var caso = await db.Cases.FirstOrDefaultAsync(c => c.Id == evt.RelatedId, ct);
                if (caso?.SocietyId is null) return;
                if (!await db.Alerts.AnyAsync(a => a.SocietyId == caso.SocietyId && a.RuleCode == "ACUERDO_VIGENTE" && a.Status == "Abierta", ct))
                {
                    db.Alerts.Add(new RiskAlert
                    {
                        Id = Guid.NewGuid(),
                        RuleCode = "ACUERDO_VIGENTE",
                        RuleVersion = "1.0",
                        SocietyId = caso.SocietyId.Value,
                        TriggerData = "Se confirmó un acuerdo. Se abre seguimiento de cumplimiento, no una sanción.",
                        RaisedAt = clock.Now,
                        Level = "Media",
                        Status = "Abierta"
                    });
                }
            });
            await Deliver("D9.Calidad", async () =>
            {
                var rule = await db.QualityRules.FirstOrDefaultAsync(r => r.Code == "COHERENCIA_ACUERDO", ct);
                if (rule is null) return;
                var evt = await db.DomainEvents.FirstAsync(e => e.Id == msg.EventId, ct);
                db.QualityIssues.Add(new DataQualityIssue
                {
                    Id = Guid.NewGuid(),
                    RuleId = rule.Id,
                    SubjectType = "Case",
                    SubjectId = evt.RelatedId,
                    Description = "Verificar que la sociedad, el expediente y el cronograma queden alineados tras el acuerdo.",
                    Status = "Abierta",
                    DetectedAt = clock.Now
                });
            });
        }

        if (msg.Type == EventTypes.AcuerdoIncumplido)
        {
            await Deliver("D1.Sociedad", async () =>
            {
                var evt = await db.DomainEvents.FirstAsync(e => e.Id == msg.EventId, ct);
                var caso = await db.Cases.Include(c => c.Society).FirstOrDefaultAsync(c => c.Id == evt.RelatedId, ct);
                if (caso?.Society is null) return;
                caso.Society.Situation = "Incumplimiento de acuerdo";
            });
            await Deliver("D6.Alertas", async () =>
            {
                var evt = await db.DomainEvents.FirstAsync(e => e.Id == msg.EventId, ct);
                var caso = await db.Cases.FirstOrDefaultAsync(c => c.Id == evt.RelatedId, ct);
                if (caso?.SocietyId is null) return;
                db.Alerts.Add(new RiskAlert
                {
                    Id = Guid.NewGuid(),
                    RuleCode = "INCUMPLIMIENTO_ACUERDO",
                    RuleVersion = "1.0",
                    SocietyId = caso.SocietyId.Value,
                    TriggerData = "Cuota vencida sin pago. La alerta no genera sanción automática.",
                    RaisedAt = clock.Now,
                    Level = "Alta",
                    Status = "Abierta"
                });
            });
        }

        if (msg.Type == EventTypes.ProcesoCerrado)
        {
            await Deliver("D1.Sociedad", async () =>
            {
                var evt = await db.DomainEvents.FirstAsync(e => e.Id == msg.EventId, ct);
                var caso = await db.Cases.Include(c => c.Society).FirstOrDefaultAsync(c => c.Id == evt.RelatedId, ct);
                if (caso?.Society is null) return;
                caso.Society.Situation = caso.Outcome switch
                {
                    "Recuperación" => "Recuperada tras proceso",
                    "Liquidación concluida" => "Liquidada",
                    "Terminación anormal" => "Proceso cerrado sin recuperación",
                    _ => caso.Society.Situation
                };
            });
        }

        if (msg.Type == EventTypes.ReporteFinancieroAprobado)
        {
            await Deliver("D6.Alertas", async () =>
            {
                var evt = await db.DomainEvents.FirstAsync(e => e.Id == msg.EventId, ct);
                var report = await db.FinancialReports.FirstOrDefaultAsync(r => r.Id == evt.RelatedId, ct);
                if (report is null) return;
                if (report.Equity < 0)
                {
                    db.Alerts.Add(new RiskAlert
                    {
                        Id = Guid.NewGuid(),
                        RuleCode = "PATRIMONIO_NEGATIVO",
                        RuleVersion = "1.0",
                        SocietyId = report.SocietyId,
                        TriggerData = $"Patrimonio {report.Equity:N0} en {report.Period}.",
                        RaisedAt = clock.Now,
                        Level = "Alta",
                        Status = "Abierta"
                    });
                }
            });
        }

        await Task.CompletedTask;
    }
}
