using Edi360.Api.Contracts;
using Edi360.Api.Data;
using Edi360.Api.Domain;
using Edi360.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Edi360.Api.Application;

public sealed class WorkService(EdiDbContext db, IBusinessClock clock)
{
    public async Task<HomeDto> HomeAsync(CurrentUser user, CancellationToken ct)
    {
        var pendingQ = db.Tasks.AsNoTracking().Where(t => t.Status != "Cerrada");
        if (!user.Permissions.Contains("D3.ASIGNAR") && user.HomeExperience == HomeExperiences.Operativo)
            pendingQ = pendingQ.Where(t => t.AssigneeUserId == user.UserId);
        else if (user.HomeExperience is HomeExperiences.Coordinador)
            pendingQ = pendingQ.Where(t => t.DepartmentId == user.DepartmentId || t.AssigneeUserId == user.UserId);

        var pending = await pendingQ.OrderBy(t => t.DueAt).Take(8)
            .Select(t => new WorkItemDto(t.Id, t.Kind, t.Title, t.Status, t.Priority, t.DueAt, null, t.CaseId, t.RecordId))
            .ToListAsync(ct);

        var casesQ = Access.FilterCases(db.Cases.AsNoTracking().Include(c => c.Region), user);
        if (user.HomeExperience == HomeExperiences.Operativo)
            casesQ = casesQ.Where(c => c.AssigneeUserId == user.UserId);
        var cases = await casesQ.OrderBy(c => c.DueAt).Take(8)
            .Select(c => new WorkItemDto(c.Id, c.Type, c.Title, c.Status, c.Priority, c.DueAt, c.Number, c.Id, null))
            .ToListAsync(ct);

        var deadlines = pending.Where(p => p.DueAt != null).OrderBy(p => p.DueAt).Take(6).ToList();
        var notices = await db.Alerts.AsNoTracking().Include(a => a.Society).OrderByDescending(a => a.RaisedAt).Take(5)
            .Select(a => new NoticeDto(a.Id, a.RuleCode, a.Society.Name + " · " + a.TriggerData, a.RaisedAt, a.Level))
            .ToListAsync(ct);
        var activity = await db.AuditEvents.AsNoTracking().OrderByDescending(a => a.OccurredAt).Take(6)
            .Select(a => new ActivityDto(a.Action, a.Detail, a.OccurredAt)).ToListAsync(ct);

        var kpis = await Kpis(user, ct);
        var actions = Quick(user);
        var greeting = user.HomeExperience switch
        {
            HomeExperiences.AltaDireccion => $"Vista institucional · {user.DisplayName}",
            HomeExperiences.Delegado => $"Delegatura y equipos · {user.DepartmentName}",
            HomeExperiences.Coordinador => $"Carga del equipo · {user.DepartmentName}",
            _ => $"Bandeja de trabajo · {user.Position}"
        };
        return new HomeDto(user.HomeExperience, greeting, kpis, pending, cases, deadlines, notices, activity, actions);
    }

    private async Task<List<KpiDto>> Kpis(CurrentUser user, CancellationToken ct)
    {
        var now = clock.Now;
        if (user.HomeExperience == HomeExperiences.AltaDireccion)
        {
            var cases = await db.Cases.CountAsync(c => c.Status == "En trámite", ct);
            var alerts = await db.Alerts.CountAsync(a => a.Status == "Abierta" && a.Level == "Alta", ct);
            var insolv = await db.Cases.CountAsync(c => c.Type == "Insolvencia" && c.Status == "En trámite", ct);
            var recs = await db.Records.CountAsync(r => r.Status != "Cerrado" && r.Status != "Anulado", ct);
            return
            [
                new("casos", "Procesos en trámite", cases.ToString("N0"), "Todos los tipos", "/justicia/expedientes", "navy"),
                new("insolv", "Insolvencia activa", insolv.ToString("N0"), "Reorganización y liquidación", "/justicia/insolvencia", "navy"),
                new("alertas", "Alertas altas", alerts.ToString("N0"), "Requieren revisión humana", "/supervision/alertas", "warn"),
                new("rad", "Radicados abiertos", recs.ToString("N0"), "EDI Documental", "/documental/radicados", "navy")
            ];
        }
        if (user.HomeExperience == HomeExperiences.Delegado)
        {
            var mine = await db.Cases.CountAsync(c => c.DepartmentId == user.DepartmentId && c.Status == "En trámite", ct);
            var due = await db.Cases.CountAsync(c => c.DepartmentId == user.DepartmentId && c.DueAt != null && c.DueAt < now.AddDays(7), ct);
            var agr = await db.Agreements.CountAsync(a => a.Status == "Vigente", ct);
            return
            [
                new("eq", "Casos de la dependencia", mine.ToString("N0"), user.DepartmentName, "/justicia/expedientes", "navy"),
                new("ven", "Por vencer en 7 días", due.ToString("N0"), "Agenda procesal", "/trabajo", "warn"),
                new("agr", "Acuerdos vigentes", agr.ToString("N0"), "No implican recuperación automática", "/justicia/insolvencia", "navy")
            ];
        }
        if (user.HomeExperience == HomeExperiences.Coordinador)
        {
            var unassigned = await db.Assignments.CountAsync(a => a.UserId == null && a.Status != "Cerrada", ct);
            var team = await db.Tasks.CountAsync(t => t.DepartmentId == user.DepartmentId && t.Status != "Cerrada", ct);
            return
            [
                new("carga", "Pendientes del equipo", team.ToString("N0"), user.DepartmentName, "/trabajo", "navy"),
                new("sin", "Sin funcionario", unassigned.ToString("N0"), "Requieren reparto", "/datos/asignaciones", "warn")
            ];
        }
        var my = await db.Tasks.CountAsync(t => t.AssigneeUserId == user.UserId && t.Status != "Cerrada", ct);
        var myDue = await db.Tasks.CountAsync(t => t.AssigneeUserId == user.UserId && t.DueAt != null && t.DueAt < now.AddDays(5) && t.Status != "Cerrada", ct);
        return
        [
            new("mis", "Mis pendientes", my.ToString("N0"), "Tareas asignadas", "/trabajo", "navy"),
            new("plazo", "Con plazo próximo", myDue.ToString("N0"), "Menos de cinco días", "/trabajo", "warn")
        ];
    }

    private static List<QuickActionDto> Quick(CurrentUser user)
    {
        var list = new List<QuickActionDto>();
        if (user.Permissions.Contains("J1.RADICAR")) list.Add(new("Radicar comunicación", "/documental/nuevo"));
        if (user.Permissions.Contains("J1.REPARTIR")) list.Add(new("Reparto", "/documental/radicados"));
        if (user.Permissions.Contains("D1.VER")) list.Add(new("Ficha de sociedad", "/supervision/sociedades"));
        if (user.Permissions.Contains("J3.VER")) list.Add(new("Insolvencia", "/justicia/insolvencia"));
        if (user.Permissions.Contains("E7.VER")) list.Add(new("Informes", "/informes"));
        if (user.Permissions.Contains("GD.MIGRAR")) list.Add(new("Modernización GeDeSS", "/modernizacion"));
        if (user.Permissions.Contains("DEMO.CONTROL")) list.Add(new("Centro de demostración", "/demo"));
        return list;
    }

    public async Task<DashboardDto> DashboardAsync(CurrentUser user, DashboardFilter filter, CancellationToken ct)
    {
        Access.Require(user, "E2.VER");
        var cases = Access.FilterCases(db.Cases.AsNoTracking().Include(c => c.Society).Include(c => c.Assignee).Include(c => c.Region), user);
        if (!string.IsNullOrWhiteSpace(filter.Type)) cases = cases.Where(c => c.Type == filter.Type);
        if (!string.IsNullOrWhiteSpace(filter.Status)) cases = cases.Where(c => c.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Region)) cases = cases.Where(c => c.Region != null && c.Region.Code == filter.Region);
        var list = await cases.ToListAsync(ct);
        var kpis = new List<KpiDto>
        {
            new("t", "Procesos", list.Count.ToString("N0"), "Filtro aplicado", "/justicia/expedientes", "navy"),
            new("c", "Críticos", list.Count(c => c.Priority == "Crítica").ToString("N0"), "Prioridad crítica", "/justicia/expedientes", "warn"),
            new("s", "Sin responsable", list.Count(c => c.AssigneeUserId == null).ToString("N0"), "Meta institucional: cero", "/datos/asignaciones", "warn"),
            new("a", "Alertas abiertas", (await db.Alerts.CountAsync(a => a.Status == "Abierta", ct)).ToString("N0"), "Motor de reglas local", "/supervision/alertas", "navy")
        };
        return new DashboardDto(
            kpis,
            list.GroupBy(c => c.Type).Select(g => new ChartSlice(g.Key, g.Count())).ToList(),
            list.GroupBy(c => c.Status).Select(g => new ChartSlice(g.Key, g.Count())).ToList(),
            list.Where(c => c.Priority == "Crítica").Take(8).Select(ToList).ToList(),
            await db.Alerts.AsNoTracking().Include(a => a.Society).Where(a => a.Status == "Abierta").OrderByDescending(a => a.RaisedAt).Take(8)
                .Select(a => new AlertListDto(a.Id, a.RuleCode, a.Level, a.Status, a.Society.Name, a.RaisedAt, a.TriggerData)).ToListAsync(ct)
        );
    }

    private static CaseListDto ToList(CaseFile c) =>
        new(c.Id, c.Number, c.Title, c.Type, c.Stage, c.Status, c.Priority, c.Society?.Name, c.Assignee?.DisplayName, c.DueAt, c.Classification);
}

public sealed class CaseService(
    EdiDbContext db,
    IBusinessClock clock,
    LocalAssistanceEngine engine,
    EventPublisher events,
    AuditService audit)
{
    public async Task<PageResult<CaseListDto>> ListAsync(CurrentUser user, string? q, string? type, int page, int size, CancellationToken ct)
    {
        Access.Require(user, "J2.VER");
        var query = Access.FilterCases(db.Cases.AsNoTracking().Include(c => c.Society).Include(c => c.Assignee).Include(c => c.Region), user);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(c => c.Number.Contains(q) || c.Title.Contains(q));
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(c => c.Type == type);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(c => c.OpenedAt).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PageResult<CaseListDto>(items.Select(c => new CaseListDto(c.Id, c.Number, c.Title, c.Type, c.Stage, c.Status, c.Priority, c.Society?.Name, c.Assignee?.DisplayName, c.DueAt, c.Classification)).ToList(), total, page, size);
    }

    public async Task<CaseDetailDto> GetAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "J2.VER");
        var c = await db.Cases.Include(x => x.Society).Include(x => x.Assignee).Include(x => x.Department).Include(x => x.Region)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Expediente no encontrado.");
        if (c.Classification == "Reservado" && !Access.CanSeeReserved(user))
            throw new UnauthorizedAccessException("Este expediente es reservado.");
        if (!Access.CanSeeRegion(user, c.Region?.Code) && c.AssigneeUserId != user.UserId)
            throw new UnauthorizedAccessException("El expediente está fuera de su ámbito regional.");
        var actions = await db.LegalActions.Include(a => a.Actor).Where(a => a.CaseId == id).OrderByDescending(a => a.OccurredAt).ToListAsync(ct);
        var docs = await db.Documents.Where(d => d.CaseId == id).ToListAsync(ct);
        var tasks = await db.Tasks.Where(t => t.CaseId == id).ToListAsync(ct);
        var alerts = c.SocietyId is null ? [] : await db.Alerts.Include(a => a.Society).Where(a => a.SocietyId == c.SocietyId).Take(5)
            .Select(a => new AlertListDto(a.Id, a.RuleCode, a.Level, a.Status, a.Society.Name, a.RaisedAt, a.TriggerData)).ToListAsync(ct);
        var allowed = new List<string> { "consultar" };
        if (user.Permissions.Contains("J2.ACTUAR")) allowed.Add("registrar-actuacion");
        if (user.Permissions.Contains("J2.PROVIDENCIA") || user.Permissions.Contains("J2.DECISION")) allowed.Add("providencia");
        if (user.Permissions.Contains("J3.ACUERDO") && c.Type == "Insolvencia") allowed.Add("confirmar-acuerdo");
        if (user.Permissions.Contains("J3.CERRAR") && c.Type == "Insolvencia") allowed.Add("cerrar");
        var summary = engine.SummarizeCase(c, actions, docs);
        return new CaseDetailDto(c.Id, c.Number, c.Title, c.Type, c.Subtype, c.Stage, c.Status, c.Outcome, c.Priority,
            c.Society?.Name, c.SocietyId, c.Department.Name, c.Assignee?.DisplayName, c.OpenedAt, c.DueAt, c.Classification,
            c.Summary, summary, LocalAssistanceEngine.Banner,
            docs.Select(d => new DocumentListDto(d.Id, d.Title, d.DocumentType, d.CurrentVersion, d.CreatedAt, d.Classification)).ToList(),
            actions.Select(a => new ActionDto(a.Id, a.Type, a.Title, a.Body, a.OccurredAt, a.Actor.DisplayName, a.Status)).ToList(),
            tasks.Select(t => new WorkItemDto(t.Id, t.Kind, t.Title, t.Status, t.Priority, t.DueAt, null, t.CaseId, t.RecordId)).ToList(),
            actions.Select(a => new ActivityDto(a.Title, a.Type, a.OccurredAt)).ToList(),
            alerts, allowed);
    }

    public async Task AddActionAsync(CurrentUser user, Guid id, CreateActionRequest req, CancellationToken ct)
    {
        Access.Require(user, "J2.ACTUAR");
        if (req.Type == "Providencia" && user.ProfileCode is Profiles.Radicador or Profiles.Abogado or Profiles.Sustanciador)
            throw new UnauthorizedAccessException("La providencia la registra el funcionario jurisdiccional autorizado.");
        var c = await db.Cases.FirstAsync(x => x.Id == id, ct);
        db.LegalActions.Add(new LegalAction
        {
            Id = Guid.NewGuid(), CaseId = id, Type = req.Type, Title = req.Title, Body = req.Body,
            OccurredAt = clock.Now, ActorUserId = user.UserId, Status = "Registrada"
        });
        await db.SaveChangesAsync(ct);
        if (req.Type == "Providencia")
            await events.PublishAsync(EventTypes.ProvidenciaRegistrada, "J2", "CaseFile", id, new { c.Number, req.Title }, ct);
        else
            await events.PublishAsync(EventTypes.ExpedienteActualizado, "J2", "CaseFile", id, new { c.Number }, ct);
        await audit.WriteAsync(user, "J2", req.Type, "CaseFile", id, req.Title, ct: ct);
    }

    public async Task ConfirmAgreementAsync(CurrentUser user, Guid id, ConfirmAgreementRequest req, CancellationToken ct)
    {
        Access.Require(user, "J3.ACUERDO");
        var c = await db.Cases.Include(x => x.Society).FirstAsync(x => x.Id == id, ct);
        if (c.Type != "Insolvencia") throw new InvalidOperationException("Solo aplica a insolvencia.");
        if (await db.Agreements.AnyAsync(a => a.CaseId == id && a.Status == "Vigente", ct))
            throw new InvalidOperationException("Ya existe un acuerdo vigente.");
        var agr = new InsolvencyAgreement
        {
            Id = Guid.NewGuid(), CaseId = id, ConfirmedAt = clock.Now, Status = "Vigente",
            TotalAmount = req.TotalAmount, Notes = req.Notes + " La confirmación no declara recuperación automática."
        };
        db.Agreements.Add(agr);
        for (var i = 1; i <= Math.Clamp(req.Installments, 2, 12); i++)
        {
            db.Installments.Add(new AgreementInstallment
            {
                Id = Guid.NewGuid(), AgreementId = agr.Id, Number = i,
                DueDate = DateOnly.FromDateTime(clock.Now.Date.AddMonths(i)),
                Amount = req.TotalAmount / req.Installments, Status = "Pendiente"
            });
        }
        c.Stage = "Ejecución del acuerdo";
        db.LegalActions.Add(new LegalAction
        {
            Id = Guid.NewGuid(), CaseId = id, Type = "Acuerdo", Title = "Acuerdo confirmado",
            Body = req.Notes, OccurredAt = clock.Now, ActorUserId = user.UserId, Status = "Registrada"
        });
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.AcuerdoConfirmado, "J3", "CaseFile", id, new { c.Number, agr.TotalAmount }, ct);
        await audit.WriteAsync(user, "J3", "Confirmar acuerdo", "CaseFile", id, "Acuerdo vigente. Sociedad pasa a seguimiento.", ct: ct);
    }

    public async Task CloseAsync(CurrentUser user, Guid id, DecisionRequest req, CancellationToken ct)
    {
        Access.Require(user, "J3.CERRAR");
        if (user.ProfileCode is Profiles.Sustanciador or Profiles.Radicador)
            throw new UnauthorizedAccessException("El cierre con desenlace lo registra el funcionario competente.");
        if (string.IsNullOrWhiteSpace(req.Outcome))
            throw new InvalidOperationException("Ningún proceso puede cerrarse sin desenlace.");
        var c = await db.Cases.FirstAsync(x => x.Id == id, ct);
        c.Status = "Cerrado";
        c.Outcome = req.Outcome;
        c.ClosedAt = clock.Now;
        c.Stage = "Cierre";
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.ProcesoCerrado, "J3", "CaseFile", id, new { c.Number, req.Outcome }, ct);
        await audit.WriteAsync(user, "J3", "Cerrar proceso", "CaseFile", id, req.Outcome, ct: ct);
    }

    public async Task DetectBreachAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "J3.TRAMITAR");
        var agr = await db.Agreements.Include(a => a.Case).FirstOrDefaultAsync(a => a.CaseId == id && a.Status == "Vigente", ct)
            ?? throw new InvalidOperationException("No hay acuerdo vigente.");
        var overdue = await db.Installments.Where(i => i.AgreementId == agr.Id && i.Status != "Pagada" && i.DueDate < DateOnly.FromDateTime(clock.Now.Date)).ToListAsync(ct);
        if (overdue.Count == 0) throw new InvalidOperationException("No hay cuotas vencidas.");
        agr.Status = "Incumplido";
        agr.Case.Stage = "Incumplimiento";
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.AcuerdoIncumplido, "J3", "CaseFile", id, new { agr.Case.Number }, ct);
        await audit.WriteAsync(user, "J3", "Incumplimiento", "CaseFile", id, $"{overdue.Count} cuotas vencidas", ct: ct);
    }
}
