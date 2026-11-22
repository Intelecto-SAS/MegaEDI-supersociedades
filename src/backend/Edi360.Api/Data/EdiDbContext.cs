using Edi360.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Edi360.Api.Data;

public class EdiDbContext(DbContextOptions<EdiDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<RoleDef> Roles => Set<RoleDef>();
    public DbSet<UserRoleLink> UserRoles => Set<UserRoleLink>();
    public DbSet<PermissionDef> Permissions => Set<PermissionDef>();
    public DbSet<RolePermissionLink> RolePermissions => Set<RolePermissionLink>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Society> Societies => Set<Society>();
    public DbSet<ThirdParty> ThirdParties => Set<ThirdParty>();
    public DbSet<SocietyParty> SocietyParties => Set<SocietyParty>();
    public DbSet<ThirdPartyLink> ThirdPartyLinks => Set<ThirdPartyLink>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentRule> AssignmentRules => Set<AssignmentRule>();
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<BusinessParameter> Parameters => Set<BusinessParameter>();
    public DbSet<FilingRecord> Records => Set<FilingRecord>();
    public DbSet<RetentionRule> RetentionRules => Set<RetentionRule>();
    public DbSet<StoredDocument> Documents => Set<StoredDocument>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<CaseFile> Cases => Set<CaseFile>();
    public DbSet<CaseParty> CaseParties => Set<CaseParty>();
    public DbSet<LegalAction> LegalActions => Set<LegalAction>();
    public DbSet<WorkTask> Tasks => Set<WorkTask>();
    public DbSet<ApprovalStep> Approvals => Set<ApprovalStep>();
    public DbSet<Hearing> Hearings => Set<Hearing>();
    public DbSet<CaseNotification> Notifications => Set<CaseNotification>();
    public DbSet<Acknowledgement> Acknowledgements => Set<Acknowledgement>();
    public DbSet<InsolvencyAgreement> Agreements => Set<InsolvencyAgreement>();
    public DbSet<AgreementInstallment> Installments => Set<AgreementInstallment>();
    public DbSet<FinancialReport> FinancialReports => Set<FinancialReport>();
    public DbSet<RiskAlert> Alerts => Set<RiskAlert>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Sanction> Sanctions => Set<Sanction>();
    public DbSet<Auxiliary> Auxiliaries => Set<Auxiliary>();
    public DbSet<AuxiliaryAppointment> Appointments => Set<AuxiliaryAppointment>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractPolicy> ContractPolicies => Set<ContractPolicy>();
    public DbSet<ValidationAttempt> Validations => Set<ValidationAttempt>();
    public DbSet<DataQualityRule> QualityRules => Set<DataQualityRule>();
    public DbSet<DataQualityIssue> QualityIssues => Set<DataQualityIssue>();
    public DbSet<ExternalSource> ExternalSources => Set<ExternalSource>();
    public DbSet<IntegrationAttempt> IntegrationAttempts => Set<IntegrationAttempt>();
    public DbSet<GeneratedReport> Reports => Set<GeneratedReport>();
    public DbSet<ReportRevision> ReportRevisions => Set<ReportRevision>();
    public DbSet<DomainEventLog> DomainEvents => Set<DomainEventLog>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<EventDelivery> EventDeliveries => Set<EventDelivery>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<ProcessWebPage> ProcessPages => Set<ProcessWebPage>();
    public DbSet<SocietyProcedure> Procedures => Set<SocietyProcedure>();
    public DbSet<DemoState> DemoStates => Set<DemoState>();
    public DbSet<AssistanceDecision> AssistanceDecisions => Set<AssistanceDecision>();
    public DbSet<DownloadRequest> DownloadRequests => Set<DownloadRequest>();
    public DbSet<SecurityIncident> Incidents => Set<SecurityIncident>();
    public DbSet<ModuleCatalog> Modules => Set<ModuleCatalog>();
    public DbSet<CapabilityOwnership> Capabilities => Set<CapabilityOwnership>();
    public DbSet<MigrationBatch> MigrationBatches => Set<MigrationBatch>();
    public DbSet<MigrationIssue> MigrationIssues => Set<MigrationIssue>();
    public DbSet<MigrationCheckpoint> MigrationCheckpoints => Set<MigrationCheckpoint>();
    public DbSet<LegacyMapping> LegacyMappings => Set<LegacyMapping>();
    public DbSet<StoredBlob> FileBlobs => Set<StoredBlob>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
            model.HasDefaultSchema("edi");
        model.Entity<StoredBlob>().HasKey(x => x.Path);
        model.Entity<UserRoleLink>().HasKey(x => new { x.UserId, x.RoleId });
        model.Entity<RolePermissionLink>().HasKey(x => new { x.RoleId, x.PermissionId });
        model.Entity<UserAccount>().HasIndex(x => x.UserName).IsUnique();
        model.Entity<FilingRecord>().HasIndex(x => x.Number).IsUnique();
        model.Entity<CaseFile>().HasIndex(x => x.Number).IsUnique();
        model.Entity<Society>().HasIndex(x => x.Nit).IsUnique();
        model.Entity<ThirdParty>().HasIndex(x => new { x.DocumentType, x.DocumentNumber });
        model.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        model.Entity<Region>().HasIndex(x => x.Code).IsUnique();
        model.Entity<PermissionDef>().HasIndex(x => x.Code).IsUnique();
        model.Entity<RoleDef>().HasIndex(x => x.Code).IsUnique();
        model.Entity<FilingRecord>()
            .HasOne(x => x.ParentRecord)
            .WithMany()
            .HasForeignKey(x => x.ParentRecordId)
            .OnDelete(DeleteBehavior.Restrict);
        model.Entity<ThirdParty>()
            .HasOne(x => x.MergedInto)
            .WithMany()
            .HasForeignKey(x => x.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);
        model.Entity<Department>()
            .HasOne(x => x.Parent)
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        model.Entity<FinancialReport>().Property(x => x.Assets).HasPrecision(18, 2);
        model.Entity<FinancialReport>().Property(x => x.Liabilities).HasPrecision(18, 2);
        model.Entity<FinancialReport>().Property(x => x.Equity).HasPrecision(18, 2);
        model.Entity<FinancialReport>().Property(x => x.Income).HasPrecision(18, 2);
        model.Entity<FinancialReport>().Property(x => x.Result).HasPrecision(18, 2);
        model.Entity<InsolvencyAgreement>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        model.Entity<AgreementInstallment>().Property(x => x.Amount).HasPrecision(18, 2);
        model.Entity<Contribution>().Property(x => x.Amount).HasPrecision(18, 2);
        model.Entity<Contribution>().Property(x => x.Paid).HasPrecision(18, 2);
        model.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 2);
        model.Entity<Sanction>().Property(x => x.Amount).HasPrecision(18, 2);
        model.Entity<Contract>().Property(x => x.Value).HasPrecision(18, 2);
        model.Entity<Society>().HasOne(x => x.Supervisor).WithMany().HasForeignKey(x => x.SupervisorUserId);
        model.Entity<FilingRecord>().HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderThirdPartyId);
        model.Entity<FilingRecord>().HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeUserId);
        model.Entity<CaseFile>().HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeUserId);
        model.Entity<LegalAction>().HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorUserId);
        model.Entity<WorkTask>().HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeUserId);
        model.Entity<RiskAlert>().HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerUserId);
        model.Entity<Contract>().HasOne(x => x.Contractor).WithMany().HasForeignKey(x => x.ContractorThirdPartyId);
        model.Entity<GeneratedReport>().HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerUserId);
        model.Entity<StoredDocument>().HasOne(x => x.Record).WithMany().HasForeignKey(x => x.RecordId);
        SqliteDates.Apply(model, Database);
    }
}

internal static class SqliteDates
{
    public static void Apply(ModelBuilder model, DatabaseFacade database)
    {
        if (!database.IsSqlite()) return;
        var converter = new DateTimeOffsetToBinaryConverter();
        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                    property.SetValueConverter(converter);
            }
        }
    }
}

public class LegacyDbContext(DbContextOptions<LegacyDbContext> options) : DbContext(options)
{
    public DbSet<LegacyRecord> Records => Set<LegacyRecord>();
    public DbSet<LegacyCase> Cases => Set<LegacyCase>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
            model.HasDefaultSchema("gedess");
        model.Entity<LegacyRecord>().HasIndex(x => x.LegacyNumber);
        SqliteDates.Apply(model, Database);
    }
}
