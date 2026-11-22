using System.Security.Cryptography;
using System.Text;
using Edi360.Api.Contracts;
using Edi360.Api.Data;
using Edi360.Api.Data.Seed;
using Edi360.Api.Domain;
using Edi360.Api.Security;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Edi360.Api.Application;

public sealed class MigrationService(EdiDbContext db, LegacyDbContext legacy, AuditService audit, EventPublisher events, IBusinessClock clock, IFileStorage files)
{
    public async Task<MigrationOverviewDto> OverviewAsync(CurrentUser user, CancellationToken ct)
    {
        Access.Require(user, "GD.MIGRAR");
        var legacyCount = await legacy.Records.CountAsync(ct);
        var mapped = await db.LegacyMappings.ToListAsync(ct);
        var caps = await db.Capabilities.OrderBy(c => c.Capability).Select(c => new CapabilityDto(c.Capability, c.Owner, c.MigrationState)).ToListAsync(ct);
        var batches = await db.MigrationBatches.OrderByDescending(b => b.CreatedAt)
            .Select(b => new MigrationBatchDto(b.Id, b.Name, b.Status, b.LegacyCount, b.MigratedCount, b.PendingCount, b.RejectedCount, b.DuplicateCount, b.ReconciledCount)).ToListAsync(ct);
        var history = await db.MigrationCheckpoints.OrderByDescending(c => c.OccurredAt).Take(20)
            .Select(c => new ActivityDto(c.Step, c.Detail, c.OccurredAt)).ToListAsync(ct);
        return new MigrationOverviewDto(
            legacyCount,
            mapped.Count(m => m.Status is "Migrado" or "Conciliado"),
            mapped.Count(m => m.Status == "Pendiente") + Math.Max(0, legacyCount - mapped.Count),
            mapped.Count(m => m.Status == "Rechazado"),
            mapped.Count(m => m.Status == "Duplicado"),
            mapped.Count(m => m.Status == "Conciliado"),
            mapped.Count(m => m.NewRecordId != null),
            caps, batches, history,
            "Simulación local. No es una migración sobre GeDeSS institucional."
        );
    }

    public async Task InventoryAsync(CurrentUser user, Guid batchId, CancellationToken ct)
    {
        Access.Require(user, "GD.MIGRAR");
        var batch = await db.MigrationBatches.FirstAsync(b => b.Id == batchId, ct);
        var records = await legacy.Records.ToListAsync(ct);
        batch.LegacyCount = records.Count;
        batch.DuplicateCount = records.Count(r => r.DuplicateSuspect);
        batch.Status = MigrationStates.Inventariado;
        batch.LastRunAt = clock.Now;
        db.MigrationCheckpoints.Add(new MigrationCheckpoint { Id = Guid.NewGuid(), BatchId = batchId, Step = "Inventario", OccurredAt = clock.Now, Detail = $"{records.Count} radicados heredados." });
        foreach (var r in records.Where(r => r.QualityFlags.Length > 0).Take(25))
        {
            db.MigrationIssues.Add(new MigrationIssue
            {
                Id = Guid.NewGuid(), BatchId = batchId, LegacyRecordId = r.Id, Severity = r.DuplicateSuspect ? "Alta" : "Media",
                Description = r.QualityFlags, Status = "Abierta"
            });
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "GeDeSS", "Inventario", "MigrationBatch", batchId, "Inventario de lote histórico.", ct: ct);
    }

    public async Task TestMigrateAsync(CurrentUser user, Guid batchId, CancellationToken ct)
    {
        Access.Require(user, "GD.MIGRAR");
        var batch = await db.MigrationBatches.FirstAsync(b => b.Id == batchId, ct);
        var records = await legacy.Records.ToListAsync(ct);
        var existing = await db.LegacyMappings.Select(m => m.LegacyRecordId).ToListAsync(ct);
        var created = 0;
        foreach (var r in records)
        {
            if (existing.Contains(r.Id)) continue; // idempotente
            if (r.DuplicateSuspect)
            {
                db.LegacyMappings.Add(new LegacyMapping
                {
                    Id = Guid.NewGuid(), BatchId = batchId, LegacyRecordId = r.Id, LegacyNumber = r.LegacyNumber,
                    FileHash = r.DocumentHash ?? "", Status = "Duplicado"
                });
                continue;
            }
            var rec = new FilingRecord
            {
                Id = Guid.NewGuid(),
                Number = $"EDI-MIG-{r.LegacyNumber}",
                Channel = r.Channel,
                Direction = "Entrada",
                ReceivedAt = r.ArrivalDate ?? clock.Now,
                FiledAt = r.FilingDate ?? clock.Now,
                IncorporatedAt = r.CaseDate,
                Subject = r.Subject,
                Body = "Migrado desde GeDeSS simulado. " + r.RawPayload,
                Status = "Migrado",
                ClassificationName = "Histórico",
                ClassificationCode = "OTROS",
                DepartmentId = DemoIds.Of("dep:GD"),
                DataOrigin = "Migración GeDeSS simulada",
                SuggestionDecision = "No aplica"
            };
            db.Records.Add(rec);
            if (!string.IsNullOrWhiteSpace(r.DocumentPath) && File.Exists(r.DocumentPath))
            {
                await using var fs = File.OpenRead(r.DocumentPath);
                var saved = await files.SaveAsync(fs, Path.Combine("migrated", rec.Id.ToString()), Path.GetFileName(r.DocumentPath), ct);
                var doc = new StoredDocument
                {
                    Id = Guid.NewGuid(), Title = r.DocumentName ?? "Documento heredado", DocumentType = "Histórico",
                    RecordId = rec.Id, Classification = "Ordinario", CurrentVersion = 1,
                    CreatedById = user.UserId, CreatedAt = clock.Now
                };
                db.Documents.Add(doc);
                db.DocumentVersions.Add(new DocumentVersion
                {
                    Id = Guid.NewGuid(), DocumentId = doc.Id, VersionNumber = 1, FileName = doc.Title,
                    ContentType = "text/plain", Size = saved.size, StoragePath = saved.path, Sha256 = saved.sha,
                    CreatedById = user.UserId, CreatedAt = clock.Now, ChangeNote = "Transferido con verificación de integridad"
                });
                if (!string.Equals(saved.sha, r.DocumentHash, StringComparison.OrdinalIgnoreCase))
                {
                    db.MigrationIssues.Add(new MigrationIssue
                    {
                        Id = Guid.NewGuid(), BatchId = batchId, LegacyRecordId = r.Id, Severity = "Alta",
                        Description = "El hash del archivo no coincide.", Status = "Abierta"
                    });
                }
            }
            db.LegacyMappings.Add(new LegacyMapping
            {
                Id = Guid.NewGuid(), BatchId = batchId, LegacyRecordId = r.Id, NewRecordId = rec.Id,
                LegacyNumber = r.LegacyNumber, NewNumber = rec.Number, FileHash = r.DocumentHash ?? "", Status = "Migrado"
            });
            created++;
        }
        batch.MigratedCount += created;
        batch.PendingCount = Math.Max(0, batch.LegacyCount - batch.MigratedCount - batch.DuplicateCount);
        batch.Status = MigrationStates.MigracionEnPrueba;
        batch.LastRunAt = clock.Now;
        db.MigrationCheckpoints.Add(new MigrationCheckpoint { Id = Guid.NewGuid(), BatchId = batchId, Step = "Migración de prueba", OccurredAt = clock.Now, Detail = $"{created} registros nuevos. Reintento idempotente." });
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.LoteMigrado, "GeDeSS", "MigrationBatch", batchId, new { created }, ct);
        await audit.WriteAsync(user, "GeDeSS", "Migración de prueba", "MigrationBatch", batchId, $"{created} registros", ct: ct);
    }

    public async Task ReconcileAsync(CurrentUser user, Guid batchId, CancellationToken ct)
    {
        Access.Require(user, "GD.CONCILIAR");
        var batch = await db.MigrationBatches.FirstAsync(b => b.Id == batchId, ct);
        var maps = await db.LegacyMappings.Where(m => m.BatchId == batchId && m.Status == "Migrado").ToListAsync(ct);
        foreach (var m in maps) m.Status = "Conciliado";
        batch.ReconciledCount = maps.Count;
        batch.Status = MigrationStates.Conciliado;
        db.MigrationCheckpoints.Add(new MigrationCheckpoint { Id = Guid.NewGuid(), BatchId = batchId, Step = "Conciliación", OccurredAt = clock.Now, Detail = $"{maps.Count} conciliados. El histórico heredado se conserva." });
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "GeDeSS", "Conciliar", "MigrationBatch", batchId, "Conciliación", ct: ct);
    }

    public async Task TransferAsync(CurrentUser user, TransferRequest req, CancellationToken ct)
    {
        Access.Require(user, "GD.TRASLADAR");
        var cap = await db.Capabilities.FirstAsync(c => c.Capability == req.Capability, ct);
        cap.Owner = "EDI";
        cap.MigrationState = MigrationStates.TrasladoAutorizado;
        cap.UpdatedAt = clock.Now;
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.CapacidadTransferida, "GeDeSS", "CapabilityOwnership", cap.Id, new { req.Capability }, ct);
        await audit.WriteAsync(user, "GeDeSS", "Traslado", "Capability", cap.Id, req.Capability, ct: ct);
    }

    public async Task RevertAsync(CurrentUser user, string capability, CancellationToken ct)
    {
        Access.Require(user, "GD.TRASLADAR");
        var cap = await db.Capabilities.FirstAsync(c => c.Capability == capability, ct);
        if (capability is "Radicacion" or "Recepcion")
        {
            var newActs = await db.Records.AnyAsync(r => r.DataOrigin == "EDI Documental" && r.FiledAt > cap.UpdatedAt, ct);
            if (newActs)
                throw new InvalidOperationException("No es seguro revertir: ya hay actuaciones nuevas en EDI. Se requiere revisión.");
        }
        cap.Owner = "GeDeSS";
        cap.MigrationState = MigrationStates.Paralelo;
        cap.UpdatedAt = clock.Now;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "GeDeSS", "Revertir capacidad", "Capability", cap.Id, capability, ct: ct);
    }

    public async Task<CompareDto> CompareAsync(CurrentUser user, Guid legacyId, CancellationToken ct)
    {
        Access.Require(user, "GD.MIGRAR");
        var legacyRec = await legacy.Records.FirstAsync(r => r.Id == legacyId, ct);
        var map = await db.LegacyMappings.FirstOrDefaultAsync(m => m.LegacyRecordId == legacyId, ct);
        FilingRecord? edi = null;
        if (map?.NewRecordId is Guid nid)
            edi = await db.Records.Include(r => r.Assignee).FirstOrDefaultAsync(r => r.Id == nid, ct);
        var diffs = new List<string>();
        if (edi is null) diffs.Add("Aún no hay equivalente en EDI.");
        else
        {
            if (legacyRec.Subject != edi.Subject) diffs.Add("El asunto difiere.");
            if (legacyRec.HasCase != edi.CaseId is not null) diffs.Add("La vinculación al expediente no coincide.");
            if (string.IsNullOrWhiteSpace(legacyRec.AssigneeName) && edi.AssigneeUserId is not null) diffs.Add("EDI ya tiene responsable; el heredado no.");
        }
        return new CompareDto(legacyRec, edi, diffs);
    }

    public async Task<List<object>> LegacyListAsync(CurrentUser user, CancellationToken ct)
    {
        Access.Require(user, "GD.MIGRAR");
        return await legacy.Records.AsNoTracking().OrderBy(r => r.LegacyNumber).Take(100)
            .Select(r => (object)new { r.Id, r.LegacyNumber, r.Subject, r.Status, r.HasCase, r.DuplicateSuspect, r.QualityFlags }).ToListAsync(ct);
    }
}

public sealed class ReportService(EdiDbContext db, AuditService audit, EventPublisher events, IBusinessClock clock)
{
    public async Task<List<ReportListDto>> ListAsync(CurrentUser user, CancellationToken ct)
    {
        Access.Require(user, "E7.VER");
        return await db.Reports.AsNoTracking().OrderBy(r => r.CatalogNumber)
            .Select(r => new ReportListDto(r.Id, r.CatalogNumber, r.Type, r.Audience, r.Period, r.Status, r.Version)).ToListAsync(ct);
    }

    public async Task<ReportDetailDto> GetAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "E7.VER");
        var r = await db.Reports.FirstAsync(x => x.Id == id, ct);
        var hist = await db.ReportRevisions.Where(x => x.ReportId == id).OrderByDescending(x => x.CreatedAt)
            .Select(x => new ActivityDto(x.Kind, x.Comment, x.CreatedAt)).ToListAsync(ct);
        return new ReportDetailDto(r.Id, r.CatalogNumber, r.Type, r.Audience, r.Period, r.Status, r.Draft, r.Sources, r.MissingRequired, r.Version, hist, user.Permissions.Contains("E7.APROBAR") && string.IsNullOrWhiteSpace(r.MissingRequired));
    }

    public async Task GenerateAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "E7.BORRADOR");
        var r = await db.Reports.FirstAsync(x => x.Id == id, ct);
        var (draft, missing, sources) = await BuildDraft(r.CatalogNumber, ct);
        r.Draft = draft;
        r.MissingRequired = missing;
        r.Sources = sources;
        r.Status = string.IsNullOrWhiteSpace(missing) ? "Borrador" : "Incompleto";
        r.Version = (int.TryParse(r.Version, out var v) ? v + 1 : 2).ToString();
        db.ReportRevisions.Add(new ReportRevision { Id = Guid.NewGuid(), ReportId = id, Comment = "Borrador del motor local.", AuthorUserId = user.UserId, CreatedAt = clock.Now, Kind = "Borrador" });
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "E7", "Generar borrador", "GeneratedReport", id, LocalAssistanceEngine.Banner, ct: ct);
    }

    public async Task ReviseAsync(CurrentUser user, Guid id, ReportRevisionRequest req, CancellationToken ct)
    {
        Access.Require(user, "E7.BORRADOR");
        var r = await db.Reports.FirstAsync(x => x.Id == id, ct);
        r.Draft = req.Draft;
        db.ReportRevisions.Add(new ReportRevision { Id = Guid.NewGuid(), ReportId = id, Comment = req.Comment, AuthorUserId = user.UserId, CreatedAt = clock.Now, Kind = "Corrección humana" });
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "E7", "Revisar informe", "GeneratedReport", id, req.Comment, ct: ct);
    }

    public async Task ApproveAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "E7.APROBAR");
        var r = await db.Reports.FirstAsync(x => x.Id == id, ct);
        if (!string.IsNullOrWhiteSpace(r.MissingRequired))
            throw new InvalidOperationException("No se aprueba: falta información obligatoria. " + r.MissingRequired);
        r.Status = "Aprobado";
        r.ApprovedAt = clock.Now;
        r.ApprovedById = user.UserId;
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.InformeAprobado, "E7", "GeneratedReport", id, new { r.Type }, ct);
        await audit.WriteAsync(user, "E7", "Aprobar informe", "GeneratedReport", id, "Firma demostrativa.", ct: ct);
    }

    public async Task<byte[]> PdfAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "E7.VER");
        var r = await db.Reports.Include(x => x.Owner).FirstAsync(x => x.Id == id, ct);
        QuestPDF.Settings.License = LicenseType.Community;
        var doc = Document.Create(c =>
        {
            c.Page(p =>
            {
                p.Margin(40);
                p.Header().Text("EDI 360 · Informe institucional (demostración)").FontSize(10).FontColor(Colors.Blue.Darken3);
                p.Content().Column(col =>
                {
                    col.Item().Text(r.Type).FontSize(18).Bold();
                    col.Item().Text($"Destinatario: {r.Audience} · Periodo: {r.Period} · Estado: {r.Status}");
                    col.Item().PaddingVertical(8).Text("Motor local de asistencia — simulación basada en reglas y plantillas").Italic().FontSize(9);
                    col.Item().Text(r.Draft).FontSize(11);
                    col.Item().PaddingTop(12).Text("Fuentes: " + r.Sources).FontSize(9);
                    if (!string.IsNullOrWhiteSpace(r.MissingRequired))
                        col.Item().Text("Faltantes: " + r.MissingRequired).FontColor(Colors.Red.Medium);
                    col.Item().PaddingTop(20).Text(r.Status == "Aprobado" ? "Firma demostrativa del responsable." : "Sin firma: el informe no está aprobado.").Bold();
                });
                p.Footer().AlignRight().Text("Datos ficticios · Uso interno de demostración");
            });
        });
        return doc.GeneratePdf();
    }

    private async Task<(string draft, string missing, string sources)> BuildDraft(int n, CancellationToken ct)
    {
        var cases = await db.Cases.CountAsync(c => c.Status == "En trámite", ct);
        var tutelas = await db.Records.CountAsync(r => r.IsTutela && r.Status != "Cerrado", ct);
        var vencidas = await db.Records.CountAsync(r => r.IsTutela && r.DueAt != null && r.DueAt < clock.Now && r.Status != "Respondido" && r.Status != "Cerrado", ct);
        var alerts = await db.Alerts.CountAsync(a => a.Status == "Abierta", ct);
        var mora = await db.Contributions.CountAsync(c => c.Status == "En mora", ct);
        var agr = await db.Agreements.CountAsync(a => a.Status == "Vigente", ct);
        var sources = "Expedientes, radicados, alertas, cartera y acuerdos del entorno local.";
        string draft = n switch
        {
            1 => $"Informe de tutelas. Hay {tutelas} tutelas abiertas y {vencidas} con plazo vencido o próximo. El motor local propone priorizar las de plazo más corto. Cada contestación debe ser revisada por la Oficina Jurídica.",
            2 => $"PQRSD. El volumen de radicados PQRSD se consulta en EDI Documental. No se completa ninguna cifra que no exista en la base.",
            3 => $"Requerimientos de control. Procesos en trámite: {cases}. Las cifras reservadas no se incluyen.",
            4 => "Supervisión contractual. Los contratos con póliza verificada y los que esperan acta de inicio aparecen en el módulo de contratación. Ningún pago se sugiere sin informe y seguridad social.",
            5 => $"Estado de sociedades. Acuerdos vigentes: {agr}. La situación de cada sociedad se lee del maestro D1, no de copias.",
            6 => "Circulares. El informe identifica sociedades obligadas a reportar y las que no tienen reporte aprobado. La delegatura confirma.",
            7 => $"Cartera. Obligaciones en mora: {mora}. Recaudo consultado en D7.",
            8 => $"Alta dirección. Procesos en trámite: {cases}. Alertas abiertas: {alerts}. Acuerdos vigentes: {agr}.",
            9 => "Insumos CGR y FURAG. Se listan indicadores disponibles en el entorno demo. La rendición la firma el responsable institucional.",
            _ => "Informe no catalogado."
        };
        draft += "\n\nEste texto es un borrador de plantilla. Una persona debe revisarlo y firmarlo. No se inventaron cifras.";
        var missing = n == 9 ? "Falta el consolidado oficial FURAG, que no existe en el entorno demo." : "";
        return (draft, missing, sources);
    }
}

public sealed class DemoService(EdiDbContext db, DemoSeeder seeder, IBusinessClock clock, AuditService audit)
{
    public async Task<List<OfficialDto>> OfficialsAsync(CancellationToken ct) =>
        await db.Users.AsNoTracking().Include(u => u.Department).Where(u => u.Active).OrderBy(u => u.DisplayName)
            .Select(u => new OfficialDto(u.Id, u.DisplayName, u.Position, u.Department.Name, u.ProfileCode, u.HomeExperience, u.UserName))
            .ToListAsync(ct);

    public async Task<DemoStatusDto> StatusAsync(CurrentUser user, CancellationToken ct)
    {
        Access.Require(user, "DEMO.CONTROL");
        var state = await db.DemoStates.FirstAsync(ct);
        var modules = await db.Modules.OrderBy(m => m.Code)
            .Select(m => new ModuleStatusDto(m.Code, m.Name, m.Pillar, m.Status, m.Source, m.Priority)).ToListAsync(ct);
        var officials = await OfficialsAsync(ct);
        var ev = await db.DomainEvents.OrderByDescending(e => e.OccurredAt).Take(12)
            .Select(e => new ActivityDto(e.Type, e.Source + " · " + e.ProcessingStatus, e.OccurredAt)).ToListAsync(ct);
        var au = await db.AuditEvents.OrderByDescending(e => e.OccurredAt).Take(12)
            .Select(e => new ActivityDto(e.Action, e.Detail, e.OccurredAt)).ToListAsync(ct);
        return new DemoStatusDto(state.BusinessClock, state.ActiveScenario, modules, officials, ev, au);
    }

    public async Task PrepareAsync(CurrentUser user, string scenario, CancellationToken ct)
    {
        Access.Require(user, "DEMO.CONTROL");
        var state = await db.DemoStates.FirstAsync(ct);
        state.ActiveScenario = scenario;
        switch (scenario)
        {
            case "radicacion":
                await EnsureInbox(ct);
                break;
            case "tutela":
                await PrepareTutela(ct);
                break;
            case "expediente":
                break;
            case "insolvencia":
                await PrepareInsolvency(ct);
                break;
            case "supervision":
                break;
            case "auxiliar":
                var aux = await db.Auxiliaries.FirstAsync(a => a.ProfessionalCard == "SIN-DATO", ct);
                aux.Status = "Bloqueado";
                break;
            case "gedess":
                break;
            case "informe":
                var rep = await db.Reports.FirstAsync(r => r.CatalogNumber == 8, ct);
                rep.Status = "Pendiente";
                rep.Draft = "";
                break;
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "DEMO", "Preparar escenario", "DemoState", state.Id, scenario, ct: ct);
    }

    public Task ResetAsync(CurrentUser user, CancellationToken ct)
    {
        Access.Require(user, "DEMO.CONTROL");
        return seeder.ResetAsync(ct);
    }

    public async Task FailIntegrationAsync(CurrentUser user, FailIntegrationRequest req, CancellationToken ct)
    {
        Access.Require(user, "DEMO.CONTROL");
        var src = await db.ExternalSources.FirstAsync(s => s.MatrixRow == req.MatrixRow, ct);
        src.Status = req.Mode;
        src.LastCheckedAt = clock.Now;
        db.IntegrationAttempts.Add(new IntegrationAttempt
        {
            Id = Guid.NewGuid(), SourceId = src.Id, AttemptedAt = clock.Now, Mode = req.Mode,
            Result = "Falla simulada desde el centro de demostración.", LatencyMs = req.Mode == "Demora" ? 5000 : 1200
        });
        var state = await db.DemoStates.FirstAsync(ct);
        state.ForcedIntegrationMode = req.Mode;
        state.ForcedSourceId = src.Id;
        await db.SaveChangesAsync(ct);
    }

    public async Task RecoverIntegrationAsync(CurrentUser user, int row, CancellationToken ct)
    {
        Access.Require(user, "DEMO.CONTROL");
        var src = await db.ExternalSources.FirstAsync(s => s.MatrixRow == row, ct);
        src.Status = "Disponible";
        src.LastCheckedAt = clock.Now;
        db.IntegrationAttempts.Add(new IntegrationAttempt
        {
            Id = Guid.NewGuid(), SourceId = src.Id, AttemptedAt = clock.Now, Mode = "Correcto",
            Result = "Integración restablecida.", LatencyMs = 90
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureInbox(CancellationToken ct)
    {
        if (await db.Records.AnyAsync(r => r.Status == "Pendiente de clasificación", ct)) return;
        db.Records.Add(new FilingRecord
        {
            Id = Guid.NewGuid(), Number = $"EDI-{clock.Now.Year}-PEND01", Channel = "Correo", Direction = "Entrada",
            ReceivedAt = clock.Now.AddHours(-2), FiledAt = clock.Now, Subject = "Comunicación lista para clasificar",
            Body = "Mensaje de demostración para radicación inteligente.", Status = "Pendiente de clasificación",
            DepartmentId = DemoIds.Of("dep:GD"), ClassificationCode = "OTROS", ClassificationName = "Por clasificar",
            SuggestionDecision = "Pendiente", Priority = "Media"
        });
    }

    private async Task PrepareTutela(CancellationToken ct)
    {
        db.Records.Add(new FilingRecord
        {
            Id = Guid.NewGuid(), Number = $"EDI-{clock.Now.Year}-TUTURG", Channel = "Correo", Direction = "Entrada",
            ReceivedAt = clock.Now.AddHours(-1), FiledAt = clock.Now,
            Subject = "Acción de tutela urgente contra actuación administrativa",
            Body = "Se solicita amparo urgente. Plazo de horas. Datos ficticios.",
            Status = "Radicado", DepartmentId = DemoIds.Of("dep:OAJ"),
            AssigneeUserId = DemoIds.Of("user:Felipe Ardila Cruz"),
            ClassificationCode = "TUTELA", ClassificationName = "Tutela", IsTutela = true, Priority = "Crítica",
            DueAt = clock.Now.AddDays(1), SuggestionDecision = "Aceptada",
            SuggestedClassification = "Tutela", SuggestionExplanation = "Palabra clave: tutela."
        });
        await Task.CompletedTask;
    }

    private async Task PrepareInsolvency(CancellationToken ct)
    {
        var c = await db.Cases.FirstAsync(x => x.Type == "Insolvencia" && x.Status == "En trámite" && x.Stage != "Ejecución del acuerdo", ct);
        c.Priority = "Alta";
        c.Stage = "Admisión";
        await Task.CompletedTask;
    }
}

public sealed class ContractService(EdiDbContext db, AuditService audit, EventPublisher events, IBusinessClock clock)
{
    public async Task<List<ContractDto>> ListAsync(CurrentUser user, CancellationToken ct)
    {
        Access.Require(user, "E6.VER");
        return await db.Contracts.AsNoTracking().Include(c => c.Contractor)
            .Select(c => new ContractDto(c.Id, c.Number, c.Object, c.Contractor.FullName, c.Value, c.Status, c.PolicyVerified, c.StartActIssued))
            .ToListAsync(ct);
    }

    public async Task VerifyPolicyAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "E6.CONTRATO");
        var p = await db.ContractPolicies.Include(x => x.Contract).FirstAsync(x => x.ContractId == id, ct);
        p.Status = "Verificada";
        p.VerifiedAt = clock.Now;
        p.VerificationResult = "Simulador de aseguradora: póliza vigente.";
        p.Contract.PolicyVerified = true;
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.PolizaVerificada, "E6", "Contract", id, new { p.Number }, ct);
        await audit.WriteAsync(user, "E6", "Verificar póliza", "Contract", id, "Simulador", ct: ct);
    }

    public async Task StartActAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "E6.CONTRATO");
        var c = await db.Contracts.FirstAsync(x => x.Id == id, ct);
        if (!c.PolicyVerified) throw new InvalidOperationException("No hay acta de inicio sin póliza verificada.");
        c.StartActIssued = true;
        c.Status = "En ejecución";
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.ContratoIniciado, "E6", "Contract", id, new { c.Number }, ct);
    }

    public async Task PayAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "E6.PAGO");
        if (user.ProfileCode == Profiles.Abogado)
            throw new UnauthorizedAccessException("Un abogado no puede aprobar un pago.");
        var c = await db.Contracts.FirstAsync(x => x.Id == id, ct);
        if (!c.StartActIssued || !c.SocialSecurityOk)
            throw new InvalidOperationException("Ningún pago sin acta, informe y seguridad social.");
        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(), ContractId = id, Amount = c.Value / 4, PaidAt = clock.Now,
            Reference = "PAGO-DEMO-" + clock.Now.ToUnixTimeSeconds(), Status = "Aprobado", ApprovedById = user.UserId
        });
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "E6", "Pago", "Contract", id, "Pago ficticio", ct: ct);
    }
}

public sealed class IntegrationService(EdiDbContext db, IBusinessClock clock)
{
    public async Task<List<IntegrationDto>> ListAsync(CurrentUser user, CancellationToken ct)
    {
        Access.Require(user, "D11.VER");
        return await db.ExternalSources.AsNoTracking().OrderBy(s => s.MatrixRow)
            .Select(s => new IntegrationDto(s.MatrixRow, s.DataItem, s.Entity, s.Kind, s.Status, s.Freshness, s.LastCheckedAt, s.Channel))
            .ToListAsync(ct);
    }

    public async Task ProbeAsync(CurrentUser user, int row, CancellationToken ct)
    {
        Access.Require(user, "D11.PROBAR");
        var s = await db.ExternalSources.FirstAsync(x => x.MatrixRow == row, ct);
        db.IntegrationAttempts.Add(new IntegrationAttempt
        {
            Id = Guid.NewGuid(), SourceId = s.Id, AttemptedAt = clock.Now, Mode = s.Status == "Disponible" ? "Correcto" : s.Status,
            Result = s.Status == "Disponible" ? "Respuesta simulada correcta." : "La fuente está en modo de falla simulada.",
            LatencyMs = 70 + row
        });
        s.LastCheckedAt = clock.Now;
        await db.SaveChangesAsync(ct);
    }
}
