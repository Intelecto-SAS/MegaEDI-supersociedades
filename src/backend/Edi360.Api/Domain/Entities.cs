namespace Edi360.Api.Domain;

public class UserAccount
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string Position { get; set; } = "";
    public string ProfileCode { get; set; } = "";
    public string HomeExperience { get; set; } = HomeExperiences.Operativo;
    public string? RegionalScope { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset? LastAccess { get; set; }
    public ICollection<UserRoleLink> Roles { get; set; } = [];
}

public class RoleDef
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ICollection<RolePermissionLink> Permissions { get; set; } = [];
}

public class UserRoleLink
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public UserAccount User { get; set; } = null!;
    public RoleDef Role { get; set; } = null!;
}

public class PermissionDef
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Module { get; set; } = "";
    public string Action { get; set; } = "";
    public string Scope { get; set; } = Scopes.Assigned;
    public string Classification { get; set; } = "Ordinario";
    public bool RequiresApproval { get; set; }
}

public class RolePermissionLink
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public RoleDef Role { get; set; } = null!;
    public PermissionDef Permission { get; set; } = null!;
}

public class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserAccount User { get; set; } = null!;
    public Guid? ImpersonatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Active { get; set; } = true;
}

public class Department
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public Guid? ParentId { get; set; }
    public Department? Parent { get; set; }
    public string? RegionCode { get; set; }
    public bool Active { get; set; } = true;
}

public class Region
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class Society
{
    public Guid Id { get; set; }
    public string Nit { get; set; } = "";
    public string Name { get; set; } = "";
    public string LegalForm { get; set; } = "S.A.S.";
    public string Status { get; set; } = "Activa";
    public string Situation { get; set; } = "Normal";
    public string EconomicActivity { get; set; } = "";
    public string Ciiu { get; set; } = "";
    public string Size { get; set; } = "Mediana";
    public Guid? RegionId { get; set; }
    public Region? Region { get; set; }
    public Guid? SupervisorUserId { get; set; }
    public UserAccount? Supervisor { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string DataOrigin { get; set; } = "D1 maestro demo";
    public string? Notes { get; set; }
}

public class ThirdParty
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = "CC";
    public string DocumentNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Kind { get; set; } = "Natural";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool PossibleDuplicate { get; set; }
    public Guid? MergedIntoId { get; set; }
    public ThirdParty? MergedInto { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string DataOrigin { get; set; } = "D2 maestro demo";
}

public class SocietyParty
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public Guid ThirdPartyId { get; set; }
    public ThirdParty ThirdParty { get; set; } = null!;
    public string Role { get; set; } = "Representante legal";
    public DateTimeOffset ValidFrom { get; set; }
}

public class ThirdPartyLink
{
    public Guid Id { get; set; }
    public Guid LeftId { get; set; }
    public Guid RightId { get; set; }
    public string Relation { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public class Assignment
{
    public Guid Id { get; set; }
    public string SubjectType { get; set; } = "";
    public Guid SubjectId { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public Guid? UserId { get; set; }
    public UserAccount? User { get; set; }
    public string Status { get; set; } = "Activa";
    public DateTimeOffset AssignedAt { get; set; }
    public Guid? AssignedById { get; set; }
    public string? Justification { get; set; }
}

public class AssignmentRule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string ClassificationCode { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public string Priority { get; set; } = "Media";
    public bool Active { get; set; } = true;
}

public class CatalogItem
{
    public Guid Id { get; set; }
    public string Catalog { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
}

public class Holiday
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = "";
}

public class BusinessParameter
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
}

public class FilingRecord
{
    public Guid Id { get; set; }
    public string Number { get; set; } = "";
    public string Channel { get; set; } = "Correo";
    public string Direction { get; set; } = "Entrada";
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset FiledAt { get; set; }
    public DateTimeOffset? IncorporatedAt { get; set; }
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string Status { get; set; } = "Radicado";
    public Guid? SenderThirdPartyId { get; set; }
    public ThirdParty? Sender { get; set; }
    public Guid? SocietyId { get; set; }
    public Society? Society { get; set; }
    public string ClassificationCode { get; set; } = "";
    public string ClassificationName { get; set; } = "";
    public string? SuggestedClassification { get; set; }
    public string? SuggestionExplanation { get; set; }
    public string SuggestionDecision { get; set; } = "Pendiente";
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public Guid? AssigneeUserId { get; set; }
    public UserAccount? Assignee { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public string Priority { get; set; } = "Media";
    public bool IsTutela { get; set; }
    public Guid? CaseId { get; set; }
    public CaseFile? Case { get; set; }
    public Guid? ParentRecordId { get; set; }
    public FilingRecord? ParentRecord { get; set; }
    public bool Cancelled { get; set; }
    public string? CancelJustification { get; set; }
    public string DataOrigin { get; set; } = "EDI Documental";
}

public class RetentionRule
{
    public Guid Id { get; set; }
    public string Series { get; set; } = "";
    public string Subseries { get; set; } = "";
    public int Years { get; set; }
    public string Disposition { get; set; } = "Conservación total";
    public bool Active { get; set; } = true;
}

public class StoredDocument
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public Guid? RecordId { get; set; }
    public FilingRecord? Record { get; set; }
    public Guid? CaseId { get; set; }
    public CaseFile? Case { get; set; }
    public string Classification { get; set; } = "Ordinario";
    public Guid? RetentionRuleId { get; set; }
    public RetentionRule? RetentionRule { get; set; }
    public int CurrentVersion { get; set; } = 1;
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class DocumentVersion
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public StoredDocument Document { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/pdf";
    public long Size { get; set; }
    public string StoragePath { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string ChangeNote { get; set; } = "";
}

public class CaseFile
{
    public Guid Id { get; set; }
    public string Number { get; set; } = "";
    public string Type { get; set; } = "";
    public string Subtype { get; set; } = "";
    public Guid? SocietyId { get; set; }
    public Society? Society { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public Guid? AssigneeUserId { get; set; }
    public UserAccount? Assignee { get; set; }
    public string Stage { get; set; } = "Radicación";
    public string Status { get; set; } = "En trámite";
    public string? Outcome { get; set; }
    public string Priority { get; set; } = "Media";
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public Guid? RegionId { get; set; }
    public Region? Region { get; set; }
    public string Classification { get; set; } = "Ordinario";
    public string Summary { get; set; } = "";
    public string Title { get; set; } = "";
}

public class CaseParty
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public CaseFile Case { get; set; } = null!;
    public Guid ThirdPartyId { get; set; }
    public ThirdParty ThirdParty { get; set; } = null!;
    public string Role { get; set; } = "Solicitante";
}

public class LegalAction
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public CaseFile Case { get; set; } = null!;
    public Guid? RecordId { get; set; }
    public Guid? DocumentId { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public Guid ActorUserId { get; set; }
    public UserAccount Actor { get; set; } = null!;
    public string Status { get; set; } = "Registrada";
}

public class WorkTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Kind { get; set; } = "";
    public Guid? CaseId { get; set; }
    public Guid? RecordId { get; set; }
    public Guid? AssigneeUserId { get; set; }
    public UserAccount? Assignee { get; set; }
    public Guid DepartmentId { get; set; }
    public string Status { get; set; } = "Pendiente";
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Priority { get; set; } = "Media";
}

public class ApprovalStep
{
    public Guid Id { get; set; }
    public string SubjectType { get; set; } = "";
    public Guid SubjectId { get; set; }
    public int Step { get; set; }
    public string Name { get; set; } = "";
    public Guid? ApproverUserId { get; set; }
    public string Status { get; set; } = "Pendiente";
    public string? Comment { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
}

public class Hearing
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public CaseFile Case { get; set; } = null!;
    public DateTimeOffset ScheduledAt { get; set; }
    public string Kind { get; set; } = "Audiencia";
    public string Status { get; set; } = "Programada";
    public string Place { get; set; } = "Sala virtual demo";
    public string Notes { get; set; } = "";
}

public class CaseNotification
{
    public Guid Id { get; set; }
    public Guid? RecordId { get; set; }
    public Guid? CaseId { get; set; }
    public Guid? ThirdPartyId { get; set; }
    public string Channel { get; set; } = "Simulado";
    public string Status { get; set; } = "Programada";
    public DateTimeOffset CreatedAt { get; set; }
    public string Subject { get; set; } = "";
}

public class Acknowledgement
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public CaseNotification Notification { get; set; } = null!;
    public string Kind { get; set; } = "Envio";
    public DateTimeOffset OccurredAt { get; set; }
    public string Result { get; set; } = "Registrado";
}

public class InsolvencyAgreement
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public CaseFile Case { get; set; } = null!;
    public DateTimeOffset ConfirmedAt { get; set; }
    public string Status { get; set; } = "Vigente";
    public decimal TotalAmount { get; set; }
    public string Notes { get; set; } = "";
}

public class AgreementInstallment
{
    public Guid Id { get; set; }
    public Guid AgreementId { get; set; }
    public InsolvencyAgreement Agreement { get; set; } = null!;
    public int Number { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pendiente";
    public DateOnly? PaidOn { get; set; }
}

public class FinancialReport
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public string Period { get; set; } = "";
    public string Source { get; set; } = "XBRL demo";
    public string Unit { get; set; } = "Miles de pesos";
    public string Status { get; set; } = "Aprobado";
    public decimal Assets { get; set; }
    public decimal Liabilities { get; set; }
    public decimal Equity { get; set; }
    public decimal Income { get; set; }
    public decimal Result { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public Guid? ApprovedById { get; set; }
}

public class RiskAlert
{
    public Guid Id { get; set; }
    public string RuleCode { get; set; } = "";
    public string RuleVersion { get; set; } = "1.0";
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public string TriggerData { get; set; } = "";
    public DateTimeOffset RaisedAt { get; set; }
    public string Level { get; set; } = "Media";
    public Guid? OwnerUserId { get; set; }
    public UserAccount? Owner { get; set; }
    public string Status { get; set; } = "Abierta";
    public string? ActionTaken { get; set; }
    public string? ReviewResult { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}

public class Contribution
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public string Status { get; set; } = "Al día";
    public DateOnly DueDate { get; set; }
}

public class Payment
{
    public Guid Id { get; set; }
    public Guid? ContributionId { get; set; }
    public Guid? ContractId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset PaidAt { get; set; }
    public string Reference { get; set; } = "";
    public string Status { get; set; } = "Registrado";
    public Guid? ApprovedById { get; set; }
}

public class Sanction
{
    public Guid Id { get; set; }
    public Guid? SocietyId { get; set; }
    public Guid? ThirdPartyId { get; set; }
    public string Number { get; set; } = "";
    public string Type { get; set; } = "Multa";
    public string Status { get; set; } = "Firme";
    public DateOnly IssuedOn { get; set; }
    public DateOnly? FirmOn { get; set; }
    public decimal? Amount { get; set; }
    public string Summary { get; set; } = "";
    public bool RestrictsAppointment { get; set; }
}

public class Auxiliary
{
    public Guid Id { get; set; }
    public Guid ThirdPartyId { get; set; }
    public ThirdParty ThirdParty { get; set; } = null!;
    public string Profession { get; set; } = "Abogado";
    public string ProfessionalCard { get; set; } = "";
    public string PolicyNumber { get; set; } = "";
    public DateOnly? PolicyUntil { get; set; }
    public string Status { get; set; } = "Habilitado";
    public string Formation { get; set; } = "";
    public string Experience { get; set; } = "";
    public bool BackgroundOk { get; set; } = true;
    public string? BlockReason { get; set; }
}

public class AuxiliaryAppointment
{
    public Guid Id { get; set; }
    public Guid AuxiliaryId { get; set; }
    public Auxiliary Auxiliary { get; set; } = null!;
    public Guid CaseId { get; set; }
    public CaseFile Case { get; set; } = null!;
    public DateTimeOffset AppointedAt { get; set; }
    public string Role { get; set; } = "Promotor";
    public string Status { get; set; } = "Vigente";
    public string? DrawCriteria { get; set; }
    public int? Rating { get; set; }
}

public class Contract
{
    public Guid Id { get; set; }
    public string Number { get; set; } = "";
    public string Object { get; set; } = "";
    public Guid ContractorThirdPartyId { get; set; }
    public ThirdParty Contractor { get; set; } = null!;
    public decimal Value { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "En elaboración";
    public Guid SupervisorUserId { get; set; }
    public bool PolicyVerified { get; set; }
    public bool StartActIssued { get; set; }
    public bool SocialSecurityOk { get; set; }
}

public class ContractPolicy
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public string Number { get; set; } = "";
    public string Insurer { get; set; } = "";
    public string Status { get; set; } = "Pendiente";
    public DateTimeOffset? VerifiedAt { get; set; }
    public string? VerificationResult { get; set; }
}

public class ValidationAttempt
{
    public Guid Id { get; set; }
    public string SubjectType { get; set; } = "";
    public Guid SubjectId { get; set; }
    public string SourceCode { get; set; } = "";
    public string Result { get; set; } = "";
    public DateTimeOffset AttemptedAt { get; set; }
    public string Detail { get; set; } = "";
}

public class DataQualityRule
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Owner { get; set; } = "";
    public bool Active { get; set; } = true;
}

public class DataQualityIssue
{
    public Guid Id { get; set; }
    public Guid RuleId { get; set; }
    public DataQualityRule Rule { get; set; } = null!;
    public string SubjectType { get; set; } = "";
    public Guid SubjectId { get; set; }
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Abierta";
    public DateTimeOffset DetectedAt { get; set; }
    public string? Correction { get; set; }
}

public class ExternalSource
{
    public Guid Id { get; set; }
    public int MatrixRow { get; set; }
    public string DataItem { get; set; } = "";
    public string Entity { get; set; } = "";
    public string Kind { get; set; } = "Externa";
    public string Channel { get; set; } = "";
    public string Modules { get; set; } = "";
    public string Status { get; set; } = "Disponible";
    public DateTimeOffset? LastCheckedAt { get; set; }
    public string Freshness { get; set; } = "Al día";
}

public class IntegrationAttempt
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public ExternalSource Source { get; set; } = null!;
    public DateTimeOffset AttemptedAt { get; set; }
    public string Mode { get; set; } = "Correcto";
    public string Result { get; set; } = "";
    public int LatencyMs { get; set; }
}

public class GeneratedReport
{
    public Guid Id { get; set; }
    public int CatalogNumber { get; set; }
    public string Type { get; set; } = "";
    public string Audience { get; set; } = "";
    public string Period { get; set; } = "";
    public string Status { get; set; } = "Borrador";
    public string Draft { get; set; } = "";
    public string Sources { get; set; } = "";
    public string MissingRequired { get; set; } = "";
    public Guid OwnerUserId { get; set; }
    public UserAccount Owner { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public string Version { get; set; } = "1";
}

public class ReportRevision
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public GeneratedReport Report { get; set; } = null!;
    public string Comment { get; set; } = "";
    public Guid AuthorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Kind { get; set; } = "Revisión";
}

public class DomainEventLog
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Version { get; set; } = "1";
    public string Source { get; set; } = "";
    public string RelatedType { get; set; } = "";
    public Guid RelatedId { get; set; }
    public string Payload { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public string ProcessingStatus { get; set; } = "Pendiente";
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Type { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string Status { get; set; } = "Pendiente";
    public string? LastError { get; set; }
}

public class EventDelivery
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Consumer { get; set; } = "";
    public string Status { get; set; } = "Entregado";
    public DateTimeOffset AttemptedAt { get; set; }
    public string? Error { get; set; }
}

public class AuditEvent
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Module { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string Detail { get; set; } = "";
    public string Result { get; set; } = "Ok";
}

public class ProcessWebPage
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public CaseFile Case { get; set; } = null!;
    public string Title { get; set; } = "";
    public string StoragePath { get; set; } = "";
    public string Status { get; set; } = "Completa";
    public DateTimeOffset? LastReviewedAt { get; set; }
    public string ChecklistJson { get; set; } = "{}";
    public string Findings { get; set; } = "";
}

public class SocietyProcedure
{
    public Guid Id { get; set; }
    public string Number { get; set; } = "";
    public string Type { get; set; } = "Reforma estatutaria";
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public string Status { get; set; } = "En revisión";
    public int ApprovalStep { get; set; }
    public Guid? AssigneeUserId { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
}

public class DemoState
{
    public Guid Id { get; set; }
    public DateTimeOffset BusinessClock { get; set; }
    public string? ActiveScenario { get; set; }
    public string? ForcedIntegrationMode { get; set; }
    public Guid? ForcedSourceId { get; set; }
}

public class AssistanceDecision
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = "";
    public Guid SubjectId { get; set; }
    public string Suggestion { get; set; } = "";
    public string Explanation { get; set; } = "";
    public Guid? DecidedById { get; set; }
    public string Decision { get; set; } = "Pendiente";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
}

public class DownloadRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pendiente";
    public DateTimeOffset RequestedAt { get; set; }
}

public class SecurityIncident
{
    public Guid Id { get; set; }
    public DateTimeOffset DetectedAt { get; set; }
    public string Title { get; set; } = "";
    public string Severity { get; set; } = "Baja";
    public string Status { get; set; } = "Abierto";
    public string Detail { get; set; } = "";
}

public class ModuleCatalog
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Pillar { get; set; } = "";
    public string Priority { get; set; } = "A";
    public string Status { get; set; } = "Operativo en demo";
    public string Source { get; set; } = "Documento";
    public string Owner { get; set; } = "";
}

public class CapabilityOwnership
{
    public Guid Id { get; set; }
    public string Capability { get; set; } = "";
    public string Owner { get; set; } = "GeDeSS";
    public string MigrationState { get; set; } = MigrationStates.NoIniciado;
    public DateTimeOffset UpdatedAt { get; set; }
}

public class MigrationBatch
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = MigrationStates.NoIniciado;
    public int LegacyCount { get; set; }
    public int MigratedCount { get; set; }
    public int PendingCount { get; set; }
    public int RejectedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ReconciledCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
}

public class MigrationIssue
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public MigrationBatch Batch { get; set; } = null!;
    public Guid? LegacyRecordId { get; set; }
    public string Severity { get; set; } = "Media";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Abierta";
    public string? Resolution { get; set; }
}

public class MigrationCheckpoint
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public string Step { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public string Detail { get; set; } = "";
}

public class LegacyMapping
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid LegacyRecordId { get; set; }
    public Guid? NewRecordId { get; set; }
    public Guid? NewCaseId { get; set; }
    public string LegacyNumber { get; set; } = "";
    public string? NewNumber { get; set; }
    public string FileHash { get; set; } = "";
    public string Status { get; set; } = "Pendiente";
}
