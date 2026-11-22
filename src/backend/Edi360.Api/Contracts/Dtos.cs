namespace Edi360.Api.Contracts;

public record PageResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public record SessionDto(
    Guid UserId,
    string UserName,
    string DisplayName,
    string Position,
    string Department,
    string ProfileCode,
    string HomeExperience,
    string? RegionalScope,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    Guid? ImpersonatedBy,
    DateTimeOffset BusinessClock);

public class LoginRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("userName")]
    public string UserName { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("password")]
    public string Password { get; set; } = "";
}
public record ImpersonateRequest(Guid UserId);

public record OfficialDto(Guid Id, string DisplayName, string Position, string Department, string ProfileCode, string HomeExperience, string UserName);

public record WorkItemDto(Guid Id, string Kind, string Title, string Status, string Priority, DateTimeOffset? DueAt, string? Number, Guid? CaseId, Guid? RecordId);
public record NoticeDto(Guid Id, string Title, string Detail, DateTimeOffset At, string Level);

public record HomeDto(
    string Experience,
    string Greeting,
    IReadOnlyList<KpiDto> Kpis,
    IReadOnlyList<WorkItemDto> Pending,
    IReadOnlyList<WorkItemDto> Cases,
    IReadOnlyList<WorkItemDto> Deadlines,
    IReadOnlyList<NoticeDto> Notices,
    IReadOnlyList<ActivityDto> Activity,
    IReadOnlyList<QuickActionDto> Actions,
    IReadOnlyList<ChartSlice> ByType,
    IReadOnlyList<ChartSlice> ByPriority,
    IReadOnlyList<ChartSlice> DeadlineHorizon);

public record KpiDto(string Id, string Label, string Value, string Hint, string? Href, string Tone);
public record ActivityDto(string Title, string Detail, DateTimeOffset At);
public record QuickActionDto(string Label, string Href);

public record RecordListDto(Guid Id, string Number, string Subject, string Channel, string Status, string Priority, string Department, string? Assignee, DateTimeOffset FiledAt, DateTimeOffset? DueAt, bool IsTutela);
public record CreateRecordRequest(string Channel, string Subject, string Body, Guid? SenderThirdPartyId, Guid? SocietyId, DateTimeOffset? ReceivedAt);
public record ClassifyRequest(string Subject, string Body);
public record ClassifyResponse(string Code, string Name, string Department, string Priority, bool Tutela, string Explanation, string Banner);
public record ConfirmClassificationRequest(string ClassificationCode);
public record AssignRequest(Guid DepartmentId, Guid? UserId, string? Justification);
public record ReplyRequest(string Subject, string Body);
public record CancelRequest(string Justification);

public record RecordDetailDto(
    Guid Id, string Number, string Channel, string Direction, DateTimeOffset ReceivedAt, DateTimeOffset FiledAt, DateTimeOffset? IncorporatedAt,
    string Subject, string Body, string Status, string ClassificationCode, string ClassificationName,
    string? SuggestedClassification, string? SuggestionExplanation, string SuggestionDecision,
    string Department, string? Assignee, string Priority, bool IsTutela, Guid? CaseId, string? CaseNumber,
    string? SocietyName, string? SenderName, DateTimeOffset? DueAt, IReadOnlyList<DocumentListDto> Documents,
    IReadOnlyList<ActivityDto> History, string Banner);

public record DocumentListDto(Guid Id, string Title, string Type, int Version, DateTimeOffset CreatedAt, string Classification);
public record CaseListDto(Guid Id, string Number, string Title, string Type, string Stage, string Status, string Priority, string? Society, string? Assignee, DateTimeOffset? DueAt, string Classification);
public record CaseDetailDto(
    Guid Id, string Number, string Title, string Type, string Subtype, string Stage, string Status, string? Outcome,
    string Priority, string? Society, Guid? SocietyId, string Department, string? Assignee, DateTimeOffset OpenedAt,
    DateTimeOffset? DueAt, string Classification, string Summary, string AssistanceSummary, string Banner,
    IReadOnlyList<DocumentListDto> Documents, IReadOnlyList<ActionDto> Actions, IReadOnlyList<WorkItemDto> Tasks,
    IReadOnlyList<ActivityDto> Timeline, IReadOnlyList<AlertListDto> Alerts, IReadOnlyList<string> AllowedActions);

public record ActionDto(Guid Id, string Type, string Title, string Body, DateTimeOffset At, string Actor, string Status);
public record CreateActionRequest(string Type, string Title, string Body);
public record DecisionRequest(string Outcome, string Notes);
public record ConfirmAgreementRequest(decimal TotalAmount, int Installments, string Notes);

public record SocietyListDto(Guid Id, string Nit, string Name, string Status, string Situation, string Size, string? Region, string Activity);
public record SocietyDetailDto(
    Guid Id, string Nit, string Name, string LegalForm, string Status, string Situation, string Activity, string Ciiu, string Size,
    string? Region, string? Supervisor, string DataOrigin, string? Notes,
    IReadOnlyList<PartyDto> Parties, IReadOnlyList<FinancialListDto> Financials, IReadOnlyList<CaseListDto> Cases,
    IReadOnlyList<AlertListDto> Alerts, IReadOnlyList<SanctionDto> Sanctions, IReadOnlyList<ContributionDto> Contributions,
    IReadOnlyList<ActivityDto> History);

public record PartyDto(string Name, string Role, string Document);
public record FinancialListDto(Guid Id, string Period, string Status, decimal Assets, decimal Liabilities, decimal Equity, decimal Income, decimal Result, string Source, string Unit);
public record AlertListDto(Guid Id, string Rule, string Level, string Status, string Society, DateTimeOffset RaisedAt, string Trigger);
public record ReviewAlertRequest(string ActionTaken, string Result);
public record SanctionDto(Guid Id, string Number, string Type, string Status, DateOnly IssuedOn, decimal? Amount, string Summary);
public record ContributionDto(Guid Id, int Year, decimal Amount, decimal Paid, string Status, DateOnly DueDate);

public record ThirdPartyDto(Guid Id, string DocumentType, string DocumentNumber, string FullName, string Kind, bool PossibleDuplicate, Guid? MergedIntoId);
public record MergeRequest(Guid TargetId, string Justification);
public record CreateThirdPartyRequest(string DocumentType, string DocumentNumber, string FullName, string Kind, string? Email);

public record DashboardDto(IReadOnlyList<KpiDto> Kpis, IReadOnlyList<ChartSlice> ByType, IReadOnlyList<ChartSlice> ByStatus, IReadOnlyList<CaseListDto> Critical, IReadOnlyList<AlertListDto> Alerts);
public record ChartSlice(string Name, int Value);
public record DashboardFilter(string? Department, string? Region, string? Type, string? Status);

public record ReportListDto(Guid Id, int CatalogNumber, string Type, string Audience, string Period, string Status, string Version);
public record ReportDetailDto(Guid Id, int CatalogNumber, string Type, string Audience, string Period, string Status, string Draft, string Sources, string MissingRequired, string Version, IReadOnlyList<ActivityDto> History, bool CanApprove);
public record ReportRevisionRequest(string Comment, string Draft);

public record MigrationOverviewDto(
    int LegacyVolume, int Migrated, int Pending, int Rejected, int Duplicates, int Reconciled, int DocumentsTransferred,
    IReadOnlyList<CapabilityDto> Capabilities, IReadOnlyList<MigrationBatchDto> Batches, IReadOnlyList<ActivityDto> History,
    string Disclaimer);
public record CapabilityDto(string Capability, string Owner, string State);
public record MigrationBatchDto(Guid Id, string Name, string Status, int LegacyCount, int MigratedCount, int PendingCount, int RejectedCount, int DuplicateCount, int ReconciledCount);
public record CompareDto(object Legacy, object? Edi, IReadOnlyList<string> Differences);
public record TransferRequest(string Capability);

public record DemoStatusDto(
    DateTimeOffset BusinessClock, string? Scenario, IReadOnlyList<ModuleStatusDto> Modules,
    IReadOnlyList<OfficialDto> Officials, IReadOnlyList<ActivityDto> RecentEvents, IReadOnlyList<ActivityDto> RecentAudit);
public record ModuleStatusDto(string Code, string Name, string Pillar, string Status, string Source, string Priority);
public record PrepareScenarioRequest(string Scenario);
public record AdvanceClockRequest(int Days);
public record FailIntegrationRequest(int MatrixRow, string Mode);

public record AuxiliaryDto(Guid Id, string Name, string Profession, string Card, string Status, string? BlockReason);
public record AppointRequest(Guid CaseId, string Role, string DrawCriteria);
public record ContractDto(Guid Id, string Number, string Object, string Contractor, decimal Value, string Status, bool PolicyVerified, bool StartActIssued);
public record IntegrationDto(int Row, string Data, string Entity, string Kind, string Status, string Freshness, DateTimeOffset? LastChecked, string Channel);
public record ProcedureDto(Guid Id, string Number, string Type, string Society, string Status, int ApprovalStep);
public record PageReviewDto(Guid Id, string Title, string Status, string Findings, string HtmlExcerpt);
public record XbrlUploadRequest(Guid SocietyId, string Period, decimal Assets, decimal Liabilities, decimal Equity, decimal Income, decimal Result);
