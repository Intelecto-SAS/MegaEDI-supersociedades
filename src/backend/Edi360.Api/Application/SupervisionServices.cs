using Edi360.Api.Contracts;
using Edi360.Api.Data;
using Edi360.Api.Domain;
using Edi360.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Edi360.Api.Application;

public sealed class SocietyService(EdiDbContext db, AuditService audit, EventPublisher events, IBusinessClock clock)
{
    public async Task<PageResult<SocietyListDto>> ListAsync(CurrentUser user, string? q, int page, int size, CancellationToken ct)
    {
        Access.Require(user, "D1.VER");
        var query = Access.FilterSocieties(db.Societies.AsNoTracking().Include(s => s.Region), user);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.Name.Contains(q) || s.Nit.Contains(q));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(s => s.Name).Skip((page - 1) * size).Take(size)
            .Select(s => new SocietyListDto(s.Id, s.Nit, s.Name, s.Status, s.Situation, s.Size, s.Region != null ? s.Region.Name : null, s.EconomicActivity))
            .ToListAsync(ct);
        return new PageResult<SocietyListDto>(items, total, page, size);
    }

    public async Task<SocietyDetailDto> GetAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "D1.VER");
        var s = await db.Societies.Include(x => x.Region).Include(x => x.Supervisor)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Sociedad no encontrada.");
        if (!Access.CanSeeRegion(user, s.Region?.Code) && s.SupervisorUserId != user.UserId)
            throw new UnauthorizedAccessException("La sociedad está fuera de su ámbito regional.");
        var parties = await db.SocietyParties.Include(p => p.ThirdParty).Where(p => p.SocietyId == id)
            .Select(p => new PartyDto(p.ThirdParty.FullName, p.Role, p.ThirdParty.DocumentType + " " + p.ThirdParty.DocumentNumber)).ToListAsync(ct);
        var fins = await db.FinancialReports.Where(f => f.SocietyId == id).OrderByDescending(f => f.Period)
            .Select(f => new FinancialListDto(f.Id, f.Period, f.Status, f.Assets, f.Liabilities, f.Equity, f.Income, f.Result, f.Source, f.Unit)).ToListAsync(ct);
        var cases = Access.FilterCases(db.Cases.Include(c => c.Assignee).Include(c => c.Region).Where(c => c.SocietyId == id), user);
        var caseList = await cases.Select(c => new CaseListDto(c.Id, c.Number, c.Title, c.Type, c.Stage, c.Status, c.Priority, s.Name, c.Assignee != null ? c.Assignee.DisplayName : null, c.DueAt, c.Classification)).ToListAsync(ct);
        var alerts = await db.Alerts.Where(a => a.SocietyId == id)
            .Select(a => new AlertListDto(a.Id, a.RuleCode, a.Level, a.Status, s.Name, a.RaisedAt, a.TriggerData)).ToListAsync(ct);
        var sans = await db.Sanctions.Where(x => x.SocietyId == id)
            .Select(x => new SanctionDto(x.Id, x.Number, x.Type, x.Status, x.IssuedOn, x.Amount, x.Summary)).ToListAsync(ct);
        var contrib = await db.Contributions.Where(x => x.SocietyId == id)
            .Select(x => new ContributionDto(x.Id, x.Year, x.Amount, x.Paid, x.Status, x.DueDate)).ToListAsync(ct);
        var history = await db.AuditEvents.Where(a => a.EntityId == id).OrderByDescending(a => a.OccurredAt).Take(12)
            .Select(a => new ActivityDto(a.Action, a.Detail, a.OccurredAt)).ToListAsync(ct);
        return new SocietyDetailDto(s.Id, s.Nit, s.Name, s.LegalForm, s.Status, s.Situation, s.EconomicActivity, s.Ciiu, s.Size,
            s.Region?.Name, s.Supervisor?.DisplayName, s.DataOrigin, s.Notes, parties, fins, caseList, alerts, sans, contrib, history);
    }

    public async Task RecalcAlertsAsync(CurrentUser user, Guid societyId, CancellationToken ct)
    {
        Access.Require(user, "D6.REVISAR");
        var reports = await db.FinancialReports.Where(r => r.SocietyId == societyId && r.Status == "Aprobado").OrderBy(r => r.Period).ToListAsync(ct);
        var last = reports.LastOrDefault();
        if (last is { Equity: < 0 } && !await db.Alerts.AnyAsync(a => a.SocietyId == societyId && a.RuleCode == "PATRIMONIO_NEGATIVO" && a.Status == "Abierta", ct))
        {
            db.Alerts.Add(new RiskAlert
            {
                Id = Guid.NewGuid(), RuleCode = "PATRIMONIO_NEGATIVO", RuleVersion = "1.0", SocietyId = societyId,
                TriggerData = $"Patrimonio {last.Equity:N0} en {last.Period}.", RaisedAt = clock.Now, Level = "Alta",
                OwnerUserId = user.UserId, Status = "Abierta"
            });
            await events.PublishAsync(EventTypes.AlertaGenerada, "D6", "Society", societyId, new { last.Period }, ct);
        }
        var contrib = await db.Contributions.FirstOrDefaultAsync(c => c.SocietyId == societyId && c.Status == "En mora", ct);
        if (contrib is not null && !await db.Alerts.AnyAsync(a => a.SocietyId == societyId && a.RuleCode == "MORA_CONTRIBUCION" && a.Status == "Abierta", ct))
        {
            db.Alerts.Add(new RiskAlert
            {
                Id = Guid.NewGuid(), RuleCode = "MORA_CONTRIBUCION", RuleVersion = "1.0", SocietyId = societyId,
                TriggerData = $"Contribución {contrib.Year} pendiente.", RaisedAt = clock.Now, Level = "Media",
                OwnerUserId = user.UserId, Status = "Abierta"
            });
        }
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "D6", "Recalcular alertas", "Society", societyId, "Motor de reglas local.", ct: ct);
    }
}

public sealed class AlertService(EdiDbContext db, AuditService audit, IBusinessClock clock)
{
    public async Task<PageResult<AlertListDto>> ListAsync(CurrentUser user, string? status, int page, int size, CancellationToken ct)
    {
        Access.Require(user, "D6.VER");
        var query = db.Alerts.AsNoTracking().Include(a => a.Society).ThenInclude(s => s.Region).AsQueryable();
        if (!string.IsNullOrWhiteSpace(user.RegionalScope) && !user.Permissions.Contains("D1.INSTITUCIONAL"))
            query = query.Where(a => a.OwnerUserId == user.UserId || a.Society.Region == null || a.Society.Region.Code == user.RegionalScope);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.RaisedAt).Skip((page - 1) * size).Take(size)
            .Select(a => new AlertListDto(a.Id, a.RuleCode, a.Level, a.Status, a.Society.Name, a.RaisedAt, a.TriggerData)).ToListAsync(ct);
        return new PageResult<AlertListDto>(items, total, page, size);
    }

    public async Task ReviewAsync(CurrentUser user, Guid id, ReviewAlertRequest req, CancellationToken ct)
    {
        Access.Require(user, "D6.REVISAR");
        var a = await db.Alerts.FirstAsync(x => x.Id == id, ct);
        a.Status = "Revisada";
        a.ActionTaken = req.ActionTaken;
        a.ReviewResult = req.Result;
        a.ReviewedAt = clock.Now;
        a.OwnerUserId = user.UserId;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "D6", "Revisar alerta", "RiskAlert", id, req.ActionTaken, ct: ct);
    }
}

public sealed class ThirdPartyService(EdiDbContext db, AuditService audit, EventPublisher events, IBusinessClock clock)
{
    public async Task<PageResult<ThirdPartyDto>> ListAsync(CurrentUser user, string? q, int page, int size, CancellationToken ct)
    {
        Access.Require(user, "D2.VER");
        var query = db.ThirdParties.AsNoTracking().Where(t => t.MergedIntoId == null);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(t => t.FullName.Contains(q) || t.DocumentNumber.Contains(q));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.FullName).Skip((page - 1) * size).Take(size)
            .Select(t => new ThirdPartyDto(t.Id, t.DocumentType, t.DocumentNumber, t.FullName, t.Kind, t.PossibleDuplicate, t.MergedIntoId))
            .ToListAsync(ct);
        return new PageResult<ThirdPartyDto>(items, total, page, size);
    }

    public async Task<ThirdPartyDto> CreateAsync(CurrentUser user, CreateThirdPartyRequest req, CancellationToken ct)
    {
        Access.Require(user, "D2.CREAR");
        var dup = await db.ThirdParties.AnyAsync(t => t.DocumentType == req.DocumentType && t.DocumentNumber == req.DocumentNumber && t.MergedIntoId == null, ct);
        var t = new ThirdParty
        {
            Id = Guid.NewGuid(), DocumentType = req.DocumentType, DocumentNumber = req.DocumentNumber,
            FullName = req.FullName, Kind = req.Kind, Email = req.Email, PossibleDuplicate = dup, CreatedAt = clock.Now
        };
        db.ThirdParties.Add(t);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "D2", "Crear tercero", "ThirdParty", t.Id, dup ? "Posible duplicado, sin fusión automática." : "Creado.", ct: ct);
        return new ThirdPartyDto(t.Id, t.DocumentType, t.DocumentNumber, t.FullName, t.Kind, t.PossibleDuplicate, t.MergedIntoId);
    }

    public async Task MergeAsync(CurrentUser user, Guid sourceId, MergeRequest req, CancellationToken ct)
    {
        Access.Require(user, "D2.FUSIONAR");
        if (sourceId == req.TargetId) throw new InvalidOperationException("Seleccione dos terceros distintos.");
        var source = await db.ThirdParties.FirstAsync(t => t.Id == sourceId, ct);
        var target = await db.ThirdParties.FirstAsync(t => t.Id == req.TargetId, ct);
        source.MergedIntoId = target.Id;
        source.PossibleDuplicate = false;
        target.PossibleDuplicate = false;
        foreach (var p in await db.SocietyParties.Where(x => x.ThirdPartyId == sourceId).ToListAsync(ct))
            p.ThirdPartyId = target.Id;
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.TerceroFusionado, "D2", "ThirdParty", target.Id, new { sourceId, req.TargetId }, ct);
        await audit.WriteAsync(user, "D2", "Fusionar", "ThirdParty", target.Id, req.Justification, ct: ct);
    }
}

public sealed class AuxiliaryService(EdiDbContext db, AuditService audit, EventPublisher events, IBusinessClock clock)
{
    public async Task<List<AuxiliaryDto>> ListAsync(CurrentUser user, CancellationToken ct)
    {
        Access.Require(user, "J4.VER");
        return await db.Auxiliaries.AsNoTracking().Include(a => a.ThirdParty)
            .Select(a => new AuxiliaryDto(a.Id, a.ThirdParty.FullName, a.Profession, a.ProfessionalCard, a.Status, a.BlockReason))
            .ToListAsync(ct);
    }

    public async Task ValidateAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "J4.GESTIONAR");
        var a = await db.Auxiliaries.FirstAsync(x => x.Id == id, ct);
        var ok = !string.IsNullOrWhiteSpace(a.ProfessionalCard) && a.ProfessionalCard != "SIN-DATO" && a.PolicyUntil > DateOnly.FromDateTime(clock.Now.Date);
        a.BackgroundOk = ok;
        a.Status = ok ? "Habilitado" : "Bloqueado";
        a.BlockReason = ok ? null : "Verificación simulada: tarjeta o póliza inconsistente.";
        db.Validations.Add(new ValidationAttempt
        {
            Id = Guid.NewGuid(), SubjectType = "Auxiliary", SubjectId = id, SourceCode = "D11-22",
            Result = ok ? "Correcto" : "Discrepancia", AttemptedAt = clock.Now,
            Detail = "Consulta simulada a tarjeta profesional."
        });
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.ValidacionCompletada, "J4", "Auxiliary", id, new { a.Status }, ct);
        await audit.WriteAsync(user, "J4", "Validar auxiliar", "Auxiliary", id, a.Status, ct: ct);
    }

    public async Task FixCardAsync(CurrentUser user, Guid id, string card, CancellationToken ct)
    {
        Access.Require(user, "J4.GESTIONAR");
        var a = await db.Auxiliaries.FirstAsync(x => x.Id == id, ct);
        a.ProfessionalCard = card;
        a.BlockReason = null;
        a.Status = "En verificación";
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "J4", "Corregir tarjeta", "Auxiliary", id, card, ct: ct);
    }

    public async Task AppointAsync(CurrentUser user, Guid id, AppointRequest req, CancellationToken ct)
    {
        Access.Require(user, "J4.DESIGNAR");
        var a = await db.Auxiliaries.Include(x => x.ThirdParty).FirstAsync(x => x.Id == id, ct);
        if (a.Status != "Habilitado" || !a.BackgroundOk)
            throw new InvalidOperationException("La inconsistencia bloquea la designación.");
        db.Appointments.Add(new AuxiliaryAppointment
        {
            Id = Guid.NewGuid(), AuxiliaryId = id, CaseId = req.CaseId, AppointedAt = clock.Now,
            Role = req.Role, Status = "Vigente", DrawCriteria = req.DrawCriteria + " (sorteo demostrativo)"
        });
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "J4", "Designar", "Auxiliary", id, $"Caso {req.CaseId}", ct: ct);
    }
}

public sealed class FinancialService(EdiDbContext db, AuditService audit, EventPublisher events, IBusinessClock clock)
{
    public async Task<FinancialListDto> UploadAsync(CurrentUser user, XbrlUploadRequest req, CancellationToken ct)
    {
        Access.Require(user, "E5.CARGAR");
        if (req.Assets != req.Liabilities + req.Equity)
            throw new InvalidOperationException("La ecuación patrimonial no cuadra (activo = pasivo + patrimonio).");
        var r = new FinancialReport
        {
            Id = Guid.NewGuid(), SocietyId = req.SocietyId, Period = req.Period, Source = "XBRL demo",
            Unit = "Pesos", Status = "Borrador", Assets = req.Assets, Liabilities = req.Liabilities,
            Equity = req.Equity, Income = req.Income, Result = req.Result, SubmittedAt = clock.Now
        };
        db.FinancialReports.Add(r);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(user, "E5", "Cargar reporte", "FinancialReport", r.Id, req.Period, ct: ct);
        return new FinancialListDto(r.Id, r.Period, r.Status, r.Assets, r.Liabilities, r.Equity, r.Income, r.Result, r.Source, r.Unit);
    }

    public async Task ApproveAsync(CurrentUser user, Guid id, CancellationToken ct)
    {
        Access.Require(user, "E5.APROBAR");
        var r = await db.FinancialReports.FirstAsync(x => x.Id == id, ct);
        if (r.Assets != r.Liabilities + r.Equity)
            throw new InvalidOperationException("No se aprueba un reporte con ecuación patrimonial rota.");
        r.Status = "Aprobado";
        r.ApprovedById = user.UserId;
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(EventTypes.ReporteFinancieroAprobado, "E5", "FinancialReport", id, new { r.Period, r.SocietyId }, ct);
        await audit.WriteAsync(user, "E5", "Aprobar reporte", "FinancialReport", id, r.Period, ct: ct);
    }
}
