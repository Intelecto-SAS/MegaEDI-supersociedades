using Edi360.Api.Application;
using Edi360.Api.Contracts;
using Edi360.Api.Data;
using Edi360.Api.Domain;
using Edi360.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Edi360.Api.Controllers;

[ApiController]
public abstract class EdiController(ICurrentUserAccessor current) : ControllerBase
{
    protected CurrentUser Me => current.Require();
}

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth, ICurrentUserAccessor current, EdiDbContext db) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<SessionDto>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var session = await auth.SignInAsync(req.UserName, req.Password, ct);
        Response.Cookies.Append(AuthService.CookieName, session.Id.ToString(), AuthService.SessionCookie(Request));
        var user = await auth.LoadCurrentUserAsync(session.UserId, session.ImpersonatedByUserId, ct);
        return Ok(await ToSession(user, db, ct));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(AuthService.CookieName, out var sid) && Guid.TryParse(sid, out var id))
            await auth.SignOutAsync(id, ct);
        Response.Cookies.Delete(AuthService.CookieName, AuthService.SessionCookie(Request));
        return Ok();
    }

    [HttpGet("me")]
    public async Task<ActionResult<SessionDto>> Me(CancellationToken ct)
    {
        var user = current.User ?? throw new UnauthorizedAccessException();
        return Ok(await ToSession(user, db, ct));
    }

    [HttpGet("oficiales")]
    public async Task<ActionResult<List<OfficialDto>>> Officials([FromServices] DemoService demo, CancellationToken ct) =>
        Ok(await demo.OfficialsAsync(ct));

    internal static async Task<SessionDto> ToSession(CurrentUser user, EdiDbContext db, CancellationToken ct)
    {
        var clock = await db.DemoStates.Select(s => s.BusinessClock).FirstAsync(ct);
        var name = await db.Users.Where(u => u.Id == user.UserId).Select(u => u.UserName).FirstAsync(ct);
        return new SessionDto(user.UserId, name, user.DisplayName, user.Position, user.DepartmentName, user.ProfileCode,
            user.HomeExperience, user.RegionalScope, user.Roles.ToList(), user.Permissions.ToList(), user.ImpersonatedBy, clock);
    }
}

[Route("api/demo")]
public sealed class DemoController(DemoService demo, AuthService auth, ICurrentUserAccessor current, EdiDbContext db, IBusinessClock clock) : EdiController(current)
{
    [HttpGet("estado")]
    public Task<DemoStatusDto> Status(CancellationToken ct) => demo.StatusAsync(Me, ct);

    [HttpPost("impersonar")]
    public async Task<ActionResult<SessionDto>> Impersonate(ImpersonateRequest req, CancellationToken ct)
    {
        var actorId = Me.ImpersonatedBy ?? Me.UserId;
        var session = await auth.ImpersonateAsync(req.UserId, actorId, ct);
        Response.Cookies.Append(AuthService.CookieName, session.Id.ToString(), AuthService.SessionCookie(Request));
        var user = await auth.LoadCurrentUserAsync(session.UserId, session.ImpersonatedByUserId, ct);
        return Ok(await AuthController.ToSession(user, db, ct));
    }

    [HttpPost("escenario")]
    public async Task<IActionResult> Scenario(PrepareScenarioRequest req, CancellationToken ct)
    {
        await demo.PrepareAsync(Me, req.Scenario, ct);
        return Ok();
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        await demo.ResetAsync(Me, ct);
        return Ok();
    }

    [HttpPost("reloj")]
    public async Task<IActionResult> Clock(AdvanceClockRequest req, CancellationToken ct)
    {
        Access.Require(Me, "DEMO.CONTROL");
        await clock.AdvanceDaysAsync(req.Days, ct);
        return Ok(new { clock = clock.Now });
    }

    [HttpPost("falla-integracion")]
    public async Task<IActionResult> Fail(FailIntegrationRequest req, CancellationToken ct)
    {
        await demo.FailIntegrationAsync(Me, req, ct);
        return Ok();
    }

    [HttpPost("restablecer-integracion/{row:int}")]
    public async Task<IActionResult> Recover(int row, CancellationToken ct)
    {
        await demo.RecoverIntegrationAsync(Me, row, ct);
        return Ok();
    }
}

[Route("api/inicio")]
public sealed class HomeController(WorkService work, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<HomeDto> Get(CancellationToken ct) => work.HomeAsync(Me, ct);
}

[Route("api/tableros")]
public sealed class DashboardController(WorkService work, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<DashboardDto> Get([FromQuery] string? department, [FromQuery] string? region, [FromQuery] string? type, [FromQuery] string? status, CancellationToken ct) =>
        work.DashboardAsync(Me, new DashboardFilter(department, region, type, status), ct);
}

[Route("api/radicados")]
public sealed class RecordsController(DocumentalService svc, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<PageResult<RecordListDto>> List([FromQuery] string? q, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default) =>
        svc.ListAsync(Me, q, status, page, size, ct);

    [HttpGet("{id:guid}")]
    public Task<RecordDetailDto> Get(Guid id, CancellationToken ct) => svc.GetAsync(Me, id, ct);

    [HttpPost]
    public Task<RecordDetailDto> Create(CreateRecordRequest req, CancellationToken ct) => svc.CreateAsync(Me, req, ct);

    [HttpPost("clasificar")]
    public ActionResult<ClassifyResponse> Classify(ClassifyRequest req) => svc.Classify(req.Subject, req.Body);

    [HttpPost("{id:guid}/clasificacion")]
    public async Task<IActionResult> Confirm(Guid id, ConfirmClassificationRequest req, CancellationToken ct)
    {
        await svc.ConfirmClassificationAsync(Me, id, req, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/reparto")]
    public async Task<IActionResult> Assign(Guid id, AssignRequest req, CancellationToken ct)
    {
        await svc.AssignAsync(Me, id, req, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/respuesta")]
    public Task<RecordDetailDto> Reply(Guid id, ReplyRequest req, CancellationToken ct) => svc.ReplyAsync(Me, id, req, ct);

    [HttpPost("{id:guid}/aprobar")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await svc.ApproveReplyAsync(Me, id, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/anular")]
    public async Task<IActionResult> Cancel(Guid id, CancelRequest req, CancellationToken ct)
    {
        await svc.CancelAsync(Me, id, req, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/expediente")]
    public async Task<IActionResult> Link(Guid id, [FromBody] LinkCaseBody body, CancellationToken ct)
    {
        await svc.LinkCaseAsync(Me, id, body.CaseId, body.Title, ct);
        return Ok();
    }
}

public record LinkCaseBody(Guid? CaseId, string? Title);

[Route("api/expedientes")]
public sealed class CasesController(CaseService svc, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<PageResult<CaseListDto>> List([FromQuery] string? q, [FromQuery] string? type, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default) =>
        svc.ListAsync(Me, q, type, page, size, ct);

    [HttpGet("{id:guid}")]
    public Task<CaseDetailDto> Get(Guid id, CancellationToken ct) => svc.GetAsync(Me, id, ct);

    [HttpPost("{id:guid}/actuaciones")]
    public async Task<IActionResult> Act(Guid id, CreateActionRequest req, CancellationToken ct)
    {
        await svc.AddActionAsync(Me, id, req, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/acuerdo")]
    public async Task<IActionResult> Agreement(Guid id, ConfirmAgreementRequest req, CancellationToken ct)
    {
        await svc.ConfirmAgreementAsync(Me, id, req, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/incumplimiento")]
    public async Task<IActionResult> Breach(Guid id, CancellationToken ct)
    {
        await svc.DetectBreachAsync(Me, id, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/cierre")]
    public async Task<IActionResult> Close(Guid id, DecisionRequest req, CancellationToken ct)
    {
        await svc.CloseAsync(Me, id, req, ct);
        return Ok();
    }
}

[Route("api/sociedades")]
public sealed class SocietiesController(SocietyService svc, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<PageResult<SocietyListDto>> List([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default) =>
        svc.ListAsync(Me, q, page, size, ct);

    [HttpGet("{id:guid}")]
    public Task<SocietyDetailDto> Get(Guid id, CancellationToken ct) => svc.GetAsync(Me, id, ct);

    [HttpPost("{id:guid}/alertas")]
    public async Task<IActionResult> Recalc(Guid id, CancellationToken ct)
    {
        await svc.RecalcAlertsAsync(Me, id, ct);
        return Ok();
    }
}

[Route("api/alertas")]
public sealed class AlertsController(AlertService svc, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<PageResult<AlertListDto>> List([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default) =>
        svc.ListAsync(Me, status, page, size, ct);

    [HttpPost("{id:guid}/revision")]
    public async Task<IActionResult> Review(Guid id, ReviewAlertRequest req, CancellationToken ct)
    {
        await svc.ReviewAsync(Me, id, req, ct);
        return Ok();
    }
}

[Route("api/terceros")]
public sealed class ThirdPartiesController(ThirdPartyService svc, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<PageResult<ThirdPartyDto>> List([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default) =>
        svc.ListAsync(Me, q, page, size, ct);

    [HttpPost]
    public Task<ThirdPartyDto> Create(CreateThirdPartyRequest req, CancellationToken ct) => svc.CreateAsync(Me, req, ct);

    [HttpPost("{id:guid}/fusion")]
    public async Task<IActionResult> Merge(Guid id, MergeRequest req, CancellationToken ct)
    {
        await svc.MergeAsync(Me, id, req, ct);
        return Ok();
    }
}

[Route("api/auxiliares")]
public sealed class AuxController(AuxiliaryService svc, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<List<AuxiliaryDto>> List(CancellationToken ct) => svc.ListAsync(Me, ct);

    [HttpPost("{id:guid}/validar")]
    public async Task<IActionResult> Validate(Guid id, CancellationToken ct)
    {
        await svc.ValidateAsync(Me, id, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/tarjeta")]
    public async Task<IActionResult> Card(Guid id, [FromBody] CardBody body, CancellationToken ct)
    {
        await svc.FixCardAsync(Me, id, body.Card, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/designar")]
    public async Task<IActionResult> Appoint(Guid id, AppointRequest req, CancellationToken ct)
    {
        await svc.AppointAsync(Me, id, req, ct);
        return Ok();
    }
}

public record CardBody(string Card);

[Route("api/xbrl")]
public sealed class XbrlController(FinancialService svc, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpPost]
    public Task<FinancialListDto> Upload(XbrlUploadRequest req, CancellationToken ct) => svc.UploadAsync(Me, req, ct);

    [HttpPost("{id:guid}/aprobar")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await svc.ApproveAsync(Me, id, ct);
        return Ok();
    }
}

[Route("api/informes")]
public sealed class ReportsController(ReportService svc, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<List<ReportListDto>> List(CancellationToken ct) => svc.ListAsync(Me, ct);

    [HttpGet("{id:guid}")]
    public Task<ReportDetailDto> Get(Guid id, CancellationToken ct) => svc.GetAsync(Me, id, ct);

    [HttpPost("{id:guid}/borrador")]
    public async Task<IActionResult> Draft(Guid id, CancellationToken ct)
    {
        await svc.GenerateAsync(Me, id, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/revision")]
    public async Task<IActionResult> Revise(Guid id, ReportRevisionRequest req, CancellationToken ct)
    {
        await svc.ReviseAsync(Me, id, req, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/aprobar")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await svc.ApproveAsync(Me, id, ct);
        return Ok();
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var bytes = await svc.PdfAsync(Me, id, ct);
        return File(bytes, "application/pdf", "informe-edi360.pdf");
    }
}

[Route("api/modernizacion")]
public sealed class MigrationController(MigrationService svc, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<MigrationOverviewDto> Overview(CancellationToken ct) => svc.OverviewAsync(Me, ct);

    [HttpGet("heredado")]
    public Task<List<object>> Legacy(CancellationToken ct) => svc.LegacyListAsync(Me, ct);

    [HttpPost("{batchId:guid}/inventario")]
    public async Task<IActionResult> Inventory(Guid batchId, CancellationToken ct)
    {
        await svc.InventoryAsync(Me, batchId, ct);
        return Ok();
    }

    [HttpPost("{batchId:guid}/migrar")]
    public async Task<IActionResult> Migrate(Guid batchId, CancellationToken ct)
    {
        await svc.TestMigrateAsync(Me, batchId, ct);
        return Ok();
    }

    [HttpPost("{batchId:guid}/conciliar")]
    public async Task<IActionResult> Reconcile(Guid batchId, CancellationToken ct)
    {
        await svc.ReconcileAsync(Me, batchId, ct);
        return Ok();
    }

    [HttpPost("traslado")]
    public async Task<IActionResult> Transfer(TransferRequest req, CancellationToken ct)
    {
        await svc.TransferAsync(Me, req, ct);
        return Ok();
    }

    [HttpPost("revertir/{capability}")]
    public async Task<IActionResult> Revert(string capability, CancellationToken ct)
    {
        await svc.RevertAsync(Me, capability, ct);
        return Ok();
    }

    [HttpGet("comparar/{legacyId:guid}")]
    public Task<CompareDto> Compare(Guid legacyId, CancellationToken ct) => svc.CompareAsync(Me, legacyId, ct);
}

[Route("api/contratos")]
public sealed class ContractsController(ContractService svc, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<List<ContractDto>> List(CancellationToken ct) => svc.ListAsync(Me, ct);

    [HttpPost("{id:guid}/poliza")]
    public async Task<IActionResult> Policy(Guid id, CancellationToken ct)
    {
        await svc.VerifyPolicyAsync(Me, id, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/acta")]
    public async Task<IActionResult> Act(Guid id, CancellationToken ct)
    {
        await svc.StartActAsync(Me, id, ct);
        return Ok();
    }

    [HttpPost("{id:guid}/pago")]
    public async Task<IActionResult> Pay(Guid id, CancellationToken ct)
    {
        await svc.PayAsync(Me, id, ct);
        return Ok();
    }
}

[Route("api/integraciones")]
public sealed class IntegrationsController(IntegrationService svc, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet]
    public Task<List<IntegrationDto>> List(CancellationToken ct) => svc.ListAsync(Me, ct);

    [HttpPost("{row:int}/probar")]
    public async Task<IActionResult> Probe(int row, CancellationToken ct)
    {
        await svc.ProbeAsync(Me, row, ct);
        return Ok();
    }
}

[Route("api/catalogos")]
public sealed class CatalogController(EdiDbContext db, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet("dependencias")]
    public async Task<IActionResult> Depts(CancellationToken ct) =>
        Ok(await db.Departments.AsNoTracking().OrderBy(d => d.Name).Select(d => new { d.Id, d.Code, d.Name, d.Kind, d.RegionCode }).ToListAsync(ct));

    [HttpGet("regiones")]
    public async Task<IActionResult> Regions(CancellationToken ct) =>
        Ok(await db.Regions.AsNoTracking().Select(r => new { r.Id, r.Code, r.Name }).ToListAsync(ct));

    [HttpGet("tramites")]
    public async Task<IActionResult> Types(CancellationToken ct) =>
        Ok(await db.CatalogItems.AsNoTracking().Where(c => c.Catalog == "tramite").Select(c => new { c.Code, c.Name }).ToListAsync(ct));

    [HttpGet("parametros")]
    public async Task<IActionResult> Params(CancellationToken ct)
    {
        Access.Require(Me, "D4.VER");
        return Ok(new
        {
            parametros = await db.Parameters.AsNoTracking().ToListAsync(ct),
            festivos = await db.Holidays.AsNoTracking().OrderBy(h => h.Date).ToListAsync(ct),
            retencion = await db.RetentionRules.AsNoTracking().ToListAsync(ct)
        });
    }

    [HttpGet("modulos")]
    public async Task<IActionResult> Modules(CancellationToken ct) =>
        Ok(await db.Modules.AsNoTracking().OrderBy(m => m.Code).ToListAsync(ct));

    [HttpGet("funcionarios")]
    public async Task<IActionResult> Users(CancellationToken ct) =>
        Ok(await db.Users.AsNoTracking().Include(u => u.Department).Where(u => u.Active)
            .Select(u => new { u.Id, u.DisplayName, u.Position, Departamento = u.Department.Name }).ToListAsync(ct));
}

[Route("api/seguridad")]
public sealed class SecurityController(EdiDbContext db, IFileStorage files, ICurrentUserAccessor current) : EdiController(current)
{
    [HttpGet("auditoria")]
    public async Task<IActionResult> Audit([FromQuery] int take = 50, CancellationToken ct = default)
    {
        Access.Require(Me, "E4.AUDITAR");
        return Ok(await db.AuditEvents.AsNoTracking().OrderByDescending(a => a.OccurredAt).Take(take)
            .Select(a => new { a.OccurredAt, a.UserName, a.Module, a.Action, a.Detail, a.Result }).ToListAsync(ct));
    }

    [HttpGet("eventos")]
    public async Task<IActionResult> Events([FromQuery] int take = 50, CancellationToken ct = default)
    {
        Access.Require(Me, "E3.VER");
        return Ok(await db.DomainEvents.AsNoTracking().OrderByDescending(e => e.OccurredAt).Take(take)
            .Select(e => new { e.Id, e.Type, e.Source, e.ProcessingStatus, e.OccurredAt, e.RelatedType }).ToListAsync(ct));
    }

    [HttpGet("incidentes")]
    public async Task<IActionResult> Incidents(CancellationToken ct)
    {
        Access.Require(Me, "E4.VER");
        return Ok(await db.Incidents.AsNoTracking().OrderByDescending(i => i.DetectedAt).ToListAsync(ct));
    }

    [HttpGet("calidad")]
    public async Task<IActionResult> Quality(CancellationToken ct)
    {
        Access.Require(Me, "D9.VER");
        return Ok(new
        {
            reglas = await db.QualityRules.AsNoTracking().ToListAsync(ct),
            incidencias = await db.QualityIssues.AsNoTracking().Include(i => i.Rule).Select(i => new { i.Id, i.Description, i.Status, regla = i.Rule.Name, i.DetectedAt }).ToListAsync(ct)
        });
    }

    [HttpGet("cartera")]
    public async Task<IActionResult> Contrib(CancellationToken ct)
    {
        Access.Require(Me, "D7.VER");
        return Ok(await db.Contributions.AsNoTracking().Include(c => c.Society)
            .Select(c => new { c.Id, sociedad = c.Society.Name, c.Year, c.Amount, c.Paid, c.Status, c.DueDate }).ToListAsync(ct));
    }

    [HttpGet("sanciones")]
    public async Task<IActionResult> Sanctions(CancellationToken ct)
    {
        Access.Require(Me, "D8.VER");
        return Ok(await db.Sanctions.AsNoTracking().ToListAsync(ct));
    }

    [HttpGet("asignaciones")]
    public async Task<IActionResult> Assignments(CancellationToken ct)
    {
        Access.Require(Me, "D3.VER");
        return Ok(await db.Assignments.AsNoTracking().Include(a => a.Department).Include(a => a.User)
            .OrderByDescending(a => a.AssignedAt).Take(100)
            .Select(a => new { a.Id, a.SubjectType, a.Status, departamento = a.Department.Name, funcionario = a.User != null ? a.User.DisplayName : "Sin asignar", a.AssignedAt }).ToListAsync(ct));
    }

    [HttpGet("analitica")]
    public async Task<IActionResult> Analytics(CancellationToken ct)
    {
        Access.Require(Me, "D10.VER");
        var bySector = await db.Societies.AsNoTracking().GroupBy(s => s.EconomicActivity).Select(g => new { sector = g.Key, n = g.Count() }).ToListAsync(ct);
        var bySize = await db.Societies.AsNoTracking().GroupBy(s => s.Size).Select(g => new { tamano = g.Key, n = g.Count() }).ToListAsync(ct);
        return Ok(new { bySector, bySize, nota = "Conjuntos agregados para revisión interna. No se publican en portales externos." });
    }

    [HttpGet("paginas")]
    public async Task<IActionResult> Pages(CancellationToken ct)
    {
        Access.Require(Me, "J5.VER");
        return Ok(await db.ProcessPages.AsNoTracking().Include(p => p.Case)
            .Select(p => new PageReviewDto(p.Id, p.Title, p.Status, p.Findings, "")).ToListAsync(ct));
    }

    [HttpGet("paginas/{id:guid}")]
    public async Task<IActionResult> Page(Guid id, CancellationToken ct)
    {
        Access.Require(Me, "J5.VER");
        var p = await db.ProcessPages.FirstAsync(x => x.Id == id, ct);
        var html = await files.ReadTextAsync(p.StoragePath, ct) ?? "";
        return Ok(new PageReviewDto(p.Id, p.Title, p.Status, p.Findings, html.Length > 800 ? html[..800] : html));
    }

    [HttpPost("paginas/{id:guid}/revisar")]
    public async Task<IActionResult> ReviewPage(Guid id, CancellationToken ct)
    {
        Access.Require(Me, "J5.REVISAR");
        var p = await db.ProcessPages.FirstAsync(x => x.Id == id, ct);
        p.LastReviewedAt = DateTimeOffset.Now;
        p.Findings = p.Status == "Completa" ? "Revisión humana: cumple." : "Revisión humana: persisten faltantes. Sin sanción automática.";
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpGet("tramites")]
    public async Task<IActionResult> Procedures(CancellationToken ct)
    {
        Access.Require(Me, "J6.VER");
        return Ok(await db.Procedures.AsNoTracking().Include(p => p.Society)
            .Select(p => new ProcedureDto(p.Id, p.Number, p.Type, p.Society.Name, p.Status, p.ApprovalStep)).ToListAsync(ct));
    }

    [HttpPost("tramites/{id:guid}/aprobar")]
    public async Task<IActionResult> ApproveProc(Guid id, CancellationToken ct)
    {
        Access.Require(Me, "J6.APROBAR");
        var p = await db.Procedures.FirstAsync(x => x.Id == id, ct);
        p.ApprovalStep = Math.Min(p.ApprovalStep + 1, 3);
        p.Status = p.ApprovalStep >= 3 ? "Cerrado" : "En aprobación";
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpGet("busqueda")]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        q ??= "";
        var recs = await db.Records.AsNoTracking().Where(r => r.Number.Contains(q) || r.Subject.Contains(q)).Take(8)
            .Select(r => new { tipo = "Radicado", r.Id, titulo = r.Number, r.Subject }).ToListAsync(ct);
        var cases = Access.FilterCases(db.Cases.AsNoTracking().Include(c => c.Region), Me)
            .Where(c => c.Number.Contains(q) || c.Title.Contains(q)).Take(8)
            .Select(c => new { tipo = "Expediente", c.Id, titulo = c.Number, Subject = c.Title });
        var soc = Access.FilterSocieties(db.Societies.AsNoTracking().Include(s => s.Region), Me)
            .Where(s => s.Name.Contains(q) || s.Nit.Contains(q)).Take(8)
            .Select(s => new { tipo = "Sociedad", s.Id, titulo = s.Nit, Subject = s.Name });
        return Ok(new { radicados = recs, expedientes = await cases.ToListAsync(ct), sociedades = await soc.ToListAsync(ct) });
    }
}

[Route("api/eventos")]
public sealed class EventsController(OutboxProcessor processor) : ControllerBase
{
    [HttpGet("procesar")]
    [HttpPost("procesar")]
    public async Task<IActionResult> Process(CancellationToken ct)
    {
        var secret = Environment.GetEnvironmentVariable("CRON_SECRET");
        if (!string.IsNullOrWhiteSpace(secret))
        {
            var expected = "Bearer " + secret;
            if (!string.Equals(Request.Headers.Authorization.ToString(), expected, StringComparison.Ordinal))
                return Unauthorized();
        }
        await processor.ProcessOnce(ct);
        return Ok(new { estado = "ok" });
    }
}

[Route("api/documentos")]
public sealed class DocumentsController(EdiDbContext db, IFileStorage files, ICurrentUserAccessor current, AuditService audit) : EdiController(current)
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        Access.Require(Me, "J1.VER");
        var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct) ?? throw new KeyNotFoundException();
        if (doc.Classification == "Reservado" && !Access.CanSeeReserved(Me))
            throw new UnauthorizedAccessException("Documento reservado.");
        var ver = await db.DocumentVersions.Where(v => v.DocumentId == id).OrderByDescending(v => v.VersionNumber).FirstAsync(ct);
        var stream = await files.OpenAsync(ver.StoragePath, ct);
        await audit.WriteAsync(Me, "E4", "Descarga", "StoredDocument", id, ver.FileName, ct: ct);
        return File(stream, ver.ContentType, ver.FileName);
    }
}
