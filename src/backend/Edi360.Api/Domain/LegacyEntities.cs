namespace Edi360.Api.Domain;

public class LegacyRecord
{
    public Guid Id { get; set; }
    public string LegacyNumber { get; set; } = "";
    public string? AlternateNumber { get; set; }
    public string Channel { get; set; } = "Correo";
    public DateTimeOffset? ArrivalDate { get; set; }
    public DateTimeOffset? FilingDate { get; set; }
    public DateTimeOffset? CaseDate { get; set; }
    public string Subject { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string? SenderDocument { get; set; }
    public string? SocietyName { get; set; }
    public string? SocietyNit { get; set; }
    public string? CaseNumber { get; set; }
    public string Status { get; set; } = "Radicado";
    public string? AssigneeName { get; set; }
    public string? DepartmentName { get; set; }
    public bool HasCase { get; set; }
    public bool HasDocument { get; set; }
    public string? DocumentName { get; set; }
    public string? DocumentHash { get; set; }
    public string? DocumentPath { get; set; }
    public bool DuplicateSuspect { get; set; }
    public string QualityFlags { get; set; } = "";
    public string RawPayload { get; set; } = "";
}

public class LegacyCase
{
    public Guid Id { get; set; }
    public string Number { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "Abierto";
    public string? Responsible { get; set; }
}
