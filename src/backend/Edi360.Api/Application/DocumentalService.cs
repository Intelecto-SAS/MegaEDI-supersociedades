using Edi360.Api.Contracts;
using Edi360.Api.Data;
using Edi360.Api.Domain;
using Edi360.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Edi360.Api.Application;

public sealed class DocumentalService(
    EdiDbContext db,
    IBusinessClock clock,
    CalendarService calendar,
    LocalAssistanceEngine engine,
    EventPublisher events,
    AuditService audit,
    IFileStorage files)
{
    public ClassifyResponse Classify(string subject, string body)
    {
        var r = engine.Classify(subject, body);
        return new ClassifyResponse(r.code, r.name, r.departmentCode, r.priority, r.tutela, r.explanation, LocalAssistanceEngine.Banner);
    }

    public async Task<PageResult<RecordListDto>> ListAsync(CurrentUser user, string? q, string? status, int page, int size, CancellationToken ct)
    {
        Access.Require(user, "J1.VER");
        var query = db.Records.AsNoTracking().Include(x => x.Department).Include(x => x.Assignee).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Number.Contains(q) || x.Subject.Contains(q));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (user.ProfileCode is Profiles.Radicador)
            query = query.Where(x => x.AssigneeUserId == user.UserId || x.DepartmentId == user.DepartmentId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.FiledAt).Skip((page - 1) * size).Take(size)
            .Select(x => new RecordListDto(x.Id, x.Number, x.Subject, x.Channel, x.Status, x.Priority, x.Department.Name, x.Assignee != null ? x.Assignee.DisplayName : null, x.FiledAt, x.DueAt, x.IsTutela))
            .ToListAsync(ct);
        return new PageResult<RecordListDto>(items, total, page, size);
    }

    public async Task<RecordDetailDto> GetAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "J1.VER");
        var x = await db.Records.AsNoTracking()
            .Include(r => r.Department).Include(r => r.Assignee).Include(r => r.Society).Include(r => r.Sender).Include(r => r.Case)
            .FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new KeyNotFoundException("Radicado no encontrado.");
        var docs = await db.Documents.AsNoTracking().Where(d => d.RecordId == id)
            .Select(d => new DocumentListDto(d.Id, d.Title, d.DocumentType, d.CurrentVersion, d.CreatedAt, d.Classification)).ToListAsync(ct);
        var history = await db.AuditEvents.AsNoTracking().Where(a => a.EntityId == id)
            .OrderByDescending(a => a.OccurredAt).Take(20)
            .Select(a => new ActivityDto(a.Action, a.Detail, a.OccurredAt)).ToListAsync(ct);
        return new RecordDetailDto(x.Id, x.Number, x.Channel, x.Direction, x.ReceivedAt, x.FiledAt, x.IncorporatedAt,
            x.Subject, x.Body, x.Status, x.ClassificationCode, x.ClassificationName, x.SuggestedClassification,
            x.SuggestionExplanation, x.SuggestionDecision, x.Department.Name, x.Assignee?.DisplayName, x.Priority, x.IsTutela,
            x.CaseId, x.Case?.Number, x.Society?.Name, x.Sender?.FullName, x.DueAt, docs, history, LocalAssistanceEngine.Banner);
    }

    public async Task<RecordDetailDto> CreateAsync(CurrentUser user, CreateRecordRequest req, CancellationToken ct)
    {
        Access.Require(user, "J1.RADICAR");
        var cap = await db.Capabilities.FirstAsync(c => c.Capability == "Radicacion", ct);
        if (cap.Owner != "EDI")
            throw new InvalidOperationException("La radicación nueva está en el núcleo EDI. El repositorio heredado queda en consulta.");
        var suggestion = engine.Classify(req.Subject, req.Body);
        var dept = await db.Departments.FirstAsync(d => d.Code == suggestion.departmentCode, ct);
        var last = await db.Records.CountAsync(ct) + 1;
        var received = req.ReceivedAt ?? clock.Now;
        var filed = clock.Now;
        var rec = new FilingRecord
        {
            Id = Guid.NewGuid(),
            Number = $"EDI-{filed.Year}-{last:000000}",
            Channel = req.Channel,
            Direction = "Entrada",
            ReceivedAt = received,
            FiledAt = filed,
            Subject = req.Subject,
            Body = req.Body,
            Status = "Radicado",
            SenderThirdPartyId = req.SenderThirdPartyId,
            SocietyId = req.SocietyId,
            ClassificationCode = suggestion.code,
            ClassificationName = suggestion.name,
            SuggestedClassification = suggestion.name,
            SuggestionExplanation = suggestion.explanation,
            SuggestionDecision = "Pendiente",
            DepartmentId = dept.Id,
            Priority = suggestion.priority,
            IsTutela = suggestion.tutela,
            DueAt = calendar.AddBusinessDays(filed, suggestion.tutela ? 10 : 15),
            DataOrigin = "EDI Documental"
        };
        db.Records.Add(rec);
        db.AssistanceDecisions.Add(new AssistanceDecision
        {
            Id = Guid.NewGuid(), Kind = "Clasificación", SubjectId = rec.Id,
            Suggestion = suggestion.name, Explanation = suggestion.explanation, CreatedAt = clock.Now
        });
        db.Assignments.Add(new Assignment
        {
            Id = Guid.NewGuid(), SubjectType = "Radicado", SubjectId = rec.Id,
            DepartmentId = dept.Id, Status = "Pendiente de funcionario", AssignedAt = clock.Now, AssignedById = user.UserId
        });
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.RadicadoCreado, "J1", "FilingRecord", rec.Id, new { rec.Number }, ct);
        await audit.WriteAsync(user, "J1", "Radicar", "FilingRecord", rec.Id, $"Se creó el radicado {rec.Number}.", ct: ct);
        return await GetAsync(user, rec.Id, ct);
    }

    public async Task ConfirmClassificationAsync(CurrentUser user, Guid id, ConfirmClassificationRequest req, CancellationToken ct)
    {
        Access.Require(user, "J1.RADICAR");
        var rec = await db.Records.FirstAsync(x => x.Id == id, ct);
        var cat = await db.CatalogItems.FirstOrDefaultAsync(c => c.Catalog == "tramite" && c.Code == req.ClassificationCode, ct)
            ?? throw new InvalidOperationException("Clasificación no válida.");
        rec.ClassificationCode = cat.Code;
        rec.ClassificationName = cat.Name;
        rec.SuggestionDecision = rec.SuggestedClassification == cat.Name ? "Aceptada" : "Modificada";
        rec.IsTutela = cat.Code == "TUTELA";
        rec.Status = "Clasificado";
        var decision = await db.AssistanceDecisions.OrderByDescending(d => d.CreatedAt).FirstOrDefaultAsync(d => d.SubjectId == id, ct);
        if (decision is not null)
        {
            decision.Decision = rec.SuggestionDecision;
            decision.DecidedById = user.UserId;
            decision.DecidedAt = clock.Now;
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "J1", "Clasificar", "FilingRecord", id, rec.SuggestionDecision, ct: ct);
    }

    public async Task AssignAsync(CurrentUser user, Guid id, AssignRequest req, CancellationToken ct)
    {
        Access.Require(user, "J1.REPARTIR");
        var rec = await db.Records.FirstAsync(x => x.Id == id, ct);
        rec.DepartmentId = req.DepartmentId;
        rec.AssigneeUserId = req.UserId;
        rec.Status = req.UserId is null ? "En reparto" : "Asignado";
        db.Assignments.Add(new Assignment
        {
            Id = Guid.NewGuid(), SubjectType = "Radicado", SubjectId = rec.Id,
            DepartmentId = req.DepartmentId, UserId = req.UserId, Status = "Activa",
            AssignedAt = clock.Now, AssignedById = user.UserId, Justification = req.Justification
        });
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.RadicadoAsignado, "J1", "FilingRecord", rec.Id, new { rec.Number, req.UserId }, ct);
        await audit.WriteAsync(user, "J1", "Reparto", "FilingRecord", id, req.Justification ?? "Asignación", ct: ct);
    }

    public async Task<RecordDetailDto> ReplyAsync(CurrentUser user, Guid id, ReplyRequest req, CancellationToken ct)
    {
        Access.Require(user, "J1.RESPONDER");
        var parent = await db.Records.FirstAsync(x => x.Id == id, ct);
        var last = await db.Records.CountAsync(ct) + 1;
        var reply = new FilingRecord
        {
            Id = Guid.NewGuid(),
            Number = $"EDI-{clock.Now.Year}-{last:000000}",
            Channel = "Salida institucional",
            Direction = "Salida",
            ReceivedAt = clock.Now,
            FiledAt = clock.Now,
            Subject = req.Subject,
            Body = req.Body,
            Status = "Por aprobar",
            DepartmentId = parent.DepartmentId,
            AssigneeUserId = user.UserId,
            ParentRecordId = parent.Id,
            CaseId = parent.CaseId,
            SocietyId = parent.SocietyId,
            ClassificationCode = parent.ClassificationCode,
            ClassificationName = parent.ClassificationName,
            Priority = parent.Priority,
            IsTutela = parent.IsTutela
        };
        db.Records.Add(reply);
        parent.Status = "En respuesta";
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "J1", "Responder", "FilingRecord", reply.Id, $"Respuesta a {parent.Number}", ct: ct);
        return await GetAsync(user, reply.Id, ct);
    }

    public async Task ApproveReplyAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        if (!user.Permissions.Contains("E7.APROBAR") && !user.Permissions.Contains("J2.DECISION") && user.ProfileCode is not Profiles.JefeJuridica and not Profiles.CoordDocumental and not Profiles.JefeJuridica)
        {
            if (user.ProfileCode is Profiles.Radicador or Profiles.Abogado)
                throw new UnauthorizedAccessException("La respuesta debe aprobarla el funcionario competente.");
        }
        Access.Require(user, "J1.VER");
        if (user.ProfileCode is Profiles.Radicador)
            throw new UnauthorizedAccessException("Un radicador no puede aprobar una respuesta ni una providencia.");
        var rec = await db.Records.FirstAsync(x => x.Id == id, ct);
        rec.Status = "Aprobada";
        if (rec.ParentRecordId is Guid parentId)
        {
            var parent = await db.Records.FirstAsync(x => x.Id == parentId, ct);
            parent.Status = "Respondido";
        }
        var notif = new CaseNotification
        {
            Id = Guid.NewGuid(), RecordId = rec.Id, CaseId = rec.CaseId, Channel = "Simulado",
            Status = "Enviada", CreatedAt = clock.Now, Subject = rec.Subject
        };
        db.Notifications.Add(notif);
        db.Acknowledgements.Add(new Acknowledgement { Id = Guid.NewGuid(), NotificationId = notif.Id, Kind = "Envio", OccurredAt = clock.Now, Result = "Simulado" });
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.RespuestaAprobada, "J1", "FilingRecord", rec.Id, new { rec.Number }, ct);
        await audit.WriteAsync(user, "J1", "Aprobar respuesta", "FilingRecord", id, "Salida simulada", ct: ct);
    }

    public async Task CancelAsync(CurrentUser user, Guid id, CancelRequest req, CancellationToken ct)
    {
        Access.Require(user, "J1.ANULAR");
        if (string.IsNullOrWhiteSpace(req.Justification))
            throw new InvalidOperationException("La anulación requiere justificación y queda en auditoría.");
        var rec = await db.Records.FirstAsync(x => x.Id == id, ct);
        rec.Cancelled = true;
        rec.CancelJustification = req.Justification;
        rec.Status = "Anulado";
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "J1", "Anular", "FilingRecord", id, req.Justification, ct: ct);
    }

    public async Task LinkCaseAsync(CurrentUser user, Guid recordId, Guid? caseId, string? newTitle, CancellationToken ct)
    {
        Access.Require(user, "J2.ACTUAR");
        var rec = await db.Records.FirstAsync(x => x.Id == recordId, ct);
        if (caseId is null)
        {
            var n = await db.Cases.CountAsync(ct) + 1;
            var c = new CaseFile
            {
                Id = Guid.NewGuid(),
                Number = $"EXP-{clock.Now.Year}-{n:0000}",
                Type = rec.IsTutela ? "Tutela" : "Administrativo",
                Title = newTitle ?? rec.Subject,
                DepartmentId = rec.DepartmentId,
                AssigneeUserId = rec.AssigneeUserId,
                SocietyId = rec.SocietyId,
                Stage = "Radicación",
                Status = "En trámite",
                OpenedAt = clock.Now,
                DueAt = rec.DueAt,
                Classification = "Ordinario",
                Summary = rec.Body,
                Priority = rec.Priority
            };
            db.Cases.Add(c);
            rec.CaseId = c.Id;
            caseId = c.Id;
        }
        else rec.CaseId = caseId;
        rec.IncorporatedAt = clock.Now;
        rec.Status = "En expediente";
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.ExpedienteActualizado, "J2", "CaseFile", caseId.Value, new { rec.Number }, ct);
        await audit.WriteAsync(user, "J2", "Vincular expediente", "FilingRecord", recordId, "Vinculación", ct: ct);
    }

    public async Task<DocumentListDto> AddVersionAsync(CurrentUser user, Guid documentId, Stream content, string fileName, string note, CancellationToken ct)
    {
        Access.Require(user, "J1.RADICAR");
        var doc = await db.Documents.FirstAsync(d => d.Id == documentId, ct);
        var version = doc.CurrentVersion + 1;
        var saved = await files.SaveAsync(content, Path.Combine("docs", documentId.ToString(), version.ToString()), fileName, ct);
        db.DocumentVersions.Add(new DocumentVersion
        {
            Id = Guid.NewGuid(), DocumentId = documentId, VersionNumber = version, FileName = fileName,
            ContentType = "application/octet-stream", Size = saved.size, StoragePath = saved.path, Sha256 = saved.sha,
            CreatedById = user.UserId, CreatedAt = clock.Now, ChangeNote = note
        });
        doc.CurrentVersion = version;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "J1", "Nueva versión", "StoredDocument", documentId, note, ct: ct);
        return new DocumentListDto(doc.Id, doc.Title, doc.DocumentType, version, clock.Now, doc.Classification);
    }
}
