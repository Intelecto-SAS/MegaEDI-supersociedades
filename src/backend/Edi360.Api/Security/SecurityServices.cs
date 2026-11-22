using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Edi360.Api.Data;
using Edi360.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Edi360.Api.Security;

public sealed class CurrentUser
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = "";
    public string ProfileCode { get; init; } = "";
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = "";
    public string Position { get; init; } = "";
    public string HomeExperience { get; init; } = "";
    public string? RegionalScope { get; init; }
    public IReadOnlySet<string> Roles { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> Permissions { get; init; } = new HashSet<string>();
    public Guid? ImpersonatedBy { get; init; }
    public bool IsDemoAdmin => Roles.Contains(Profiles.DemoAdmin) || ImpersonatedBy is not null && Permissions.Contains("DEMO.CONTROL");
}

public interface ICurrentUserAccessor
{
    CurrentUser? User { get; }
    CurrentUser Require();
}

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    public CurrentUser? User { get; set; }
    public CurrentUser Require() => User ?? throw new UnauthorizedAccessException("Debe iniciar sesión.");
}

public sealed class AuthService(EdiDbContext db)
{
    public const string CookieName = "edi_session";
    public const string DemoPassword = "demo";

    public static string HashPassword(string password) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("edi360-demo|" + password)));

    public async Task<UserSession> SignInAsync(string userName, string password, CancellationToken ct = default)
    {
        var user = await db.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.UserName == userName && x.Active, ct);
        if (user is null)
            throw new InvalidOperationException("Usuario o contraseña incorrectos.");
        var computed = HashPassword(password);
        if (!string.Equals(user.PasswordHash, computed, StringComparison.Ordinal))
            throw new InvalidOperationException("Usuario o contraseña incorrectos.");
        return await CreateSessionAsync(user.Id, null, ct);
    }

    public async Task<UserSession> ImpersonateAsync(Guid targetUserId, Guid demoAdminId, CancellationToken ct = default)
    {
        var admin = await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role)
            .FirstAsync(x => x.Id == demoAdminId, ct);
        if (!admin.Roles.Any(r => r.Role.Code == Profiles.DemoAdmin))
            throw new UnauthorizedAccessException("Solo el administrador de demostración puede cambiar de funcionario.");
        var target = await db.Users.FirstOrDefaultAsync(x => x.Id == targetUserId && x.Active, ct)
            ?? throw new InvalidOperationException("El funcionario no está disponible.");
        return await CreateSessionAsync(target.Id, demoAdminId, ct);
    }

    public async Task<UserSession> CreateSessionAsync(Guid userId, Guid? impersonatedBy, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ImpersonatedByUserId = impersonatedBy,
            CreatedAt = now,
            ExpiresAt = now.AddHours(12),
            Active = true
        };
        db.Sessions.Add(session);
        var user = await db.Users.FirstAsync(x => x.Id == userId, ct);
        user.LastAccess = now;
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task SignOutAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);
        if (session is null) return;
        session.Active = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<CurrentUser?> ResolveAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sessionId && x.Active, ct);
        if (session is null || session.ExpiresAt < DateTimeOffset.UtcNow) return null;
        return await LoadCurrentUserAsync(session.UserId, session.ImpersonatedByUserId, ct);
    }

    public async Task<CurrentUser> LoadCurrentUserAsync(Guid userId, Guid? impersonatedBy, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Roles).ThenInclude(x => x.Role)
            .FirstAsync(x => x.Id == userId, ct);
        var roleIds = user.Roles.Select(r => r.RoleId).ToList();
        var permCodes = await db.RolePermissions.AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => x.Permission.Code)
            .Distinct()
            .ToListAsync(ct);
        return new CurrentUser
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            ProfileCode = user.ProfileCode,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department.Name,
            Position = user.Position,
            HomeExperience = user.HomeExperience,
            RegionalScope = user.RegionalScope,
            Roles = user.Roles.Select(r => r.Role.Code).ToHashSet(),
            Permissions = permCodes.ToHashSet(),
            ImpersonatedBy = impersonatedBy
        };
    }
}

public static class Access
{
    public static void Require(CurrentUser user, string permission)
    {
        if (!user.Permissions.Contains(permission) && !user.Permissions.Contains("DEMO.CONTROL"))
            throw new UnauthorizedAccessException("No tiene autorización para esta acción.");
    }

    public static bool Can(CurrentUser user, string permission) =>
        user.Permissions.Contains(permission) || user.Permissions.Contains("DEMO.CONTROL") && permission.StartsWith("DEMO.");

    public static bool CanSeeReserved(CurrentUser user) =>
        user.Permissions.Contains("J2.RESERVADO") || user.ProfileCode == Profiles.ControlDisciplinario;

    public static bool CanSeeRegion(CurrentUser user, string? regionCode)
    {
        if (string.IsNullOrWhiteSpace(user.RegionalScope)) return true;
        if (string.IsNullOrWhiteSpace(regionCode)) return user.Permissions.Contains("D1.INSTITUCIONAL");
        return user.RegionalScope == regionCode || user.Permissions.Contains("D1.INSTITUCIONAL");
    }

    public static IQueryable<CaseFile> FilterCases(IQueryable<CaseFile> query, CurrentUser user)
    {
        if (!CanSeeReserved(user))
            query = query.Where(c => c.Classification != "Reservado");
        if (!string.IsNullOrWhiteSpace(user.RegionalScope) && !user.Permissions.Contains("J2.INSTITUCIONAL"))
            query = query.Where(c => c.Region == null || c.Region.Code == user.RegionalScope || c.AssigneeUserId == user.UserId);
        return query;
    }

    public static IQueryable<Society> FilterSocieties(IQueryable<Society> query, CurrentUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.RegionalScope) && !user.Permissions.Contains("D1.INSTITUCIONAL"))
            query = query.Where(s => s.Region == null || s.Region.Code == user.RegionalScope || s.SupervisorUserId == user.UserId);
        return query;
    }
}

public static class DemoIds
{
    public static Guid Of(string key)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes("edi360:" + key));
        return new Guid(bytes);
    }
}
