using System.Security.Cryptography;
using System.Text;
using Edi360.Api.Application;
using Edi360.Api.Domain;
using Edi360.Api.Infrastructure;
using Edi360.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Edi360.Api.Data.Seed;

public sealed class DemoSeeder(
    EdiDbContext db,
    LegacyDbContext legacy,
    IFileStorage files)
{
    private readonly Random _rng = new(2026);

    public async Task EnsureAsync(CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct)) return;
        await SeedAllAsync(ct);
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        if (db.Database.IsNpgsql())
        {
            await db.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS edi CASCADE; CREATE SCHEMA edi;", ct);
            await legacy.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS gedess CASCADE; CREATE SCHEMA gedess;", ct);
        }
        else
        {
            await db.Database.EnsureDeletedAsync(ct);
            await legacy.Database.EnsureDeletedAsync(ct);
        }
        await db.Database.EnsureCreatedAsync(ct);
        await legacy.Database.EnsureCreatedAsync(ct);
        await files.ResetAsync(ct);
        await SeedAllAsync(ct);
    }

    private async Task SeedAllAsync(CancellationToken ct)
    {
        SeedOrg();
        SeedSecurity();
        SeedCatalogs();
        SeedPeople();
        SeedSocietiesAndParties();
        SeedFinancials();
        await SeedCasesAndRecords(ct);
        SeedAuxiliariesContracts();
        SeedQualityIntegrations();
        SeedReportsAndDemo();
        await db.SaveChangesAsync(ct);
        await SeedLegacy(ct);
        await legacy.SaveChangesAsync(ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> StoreAsync(string dir, string name, byte[] bytes, CancellationToken ct)
    {
        await using var ms = new MemoryStream(bytes);
        var saved = await files.SaveAsync(ms, dir, name, ct);
        return saved.path;
    }

    private void SeedOrg()
    {
        string[] regions =
        [
            "Norte", "Santanderes y Arauca", "Sur", "Caribe y Archipiélago", "Eje Cafetero", "Occidental y Costa Pacífica"
        ];
        foreach (var r in regions)
            db.Regions.Add(new Region { Id = DemoIds.Of("reg:" + r), Code = Slug(r), Name = r });

        AddDept("DES", "Despacho del Superintendente", "Despacho", null);
        AddDept("SG", "Secretaría General", "Despacho", "DES");
        AddDept("OAJ", "Oficina Asesora Jurídica", "Oficina", "DES");
        AddDept("OAP", "Oficina Asesora de Planeación", "Oficina", "DES");
        AddDept("OCI", "Oficina de Control Interno", "Oficina", "DES");
        AddDept("OCDI", "Oficina de Control Disciplinario Interno", "Oficina", "DES");
        AddDept("TIC", "Dirección TIC", "DireccionTIC", "DES");
        AddDept("IDA", "Innovación, Desarrollo y Arquitectura de Aplicaciones", "DireccionTIC", "TIC");
        AddDept("SAT", "Sistemas y Arquitectura de Tecnología", "DireccionTIC", "TIC");
        AddDept("SEG", "Seguridad", "DireccionTIC", "TIC");
        AddDept("ARD", "Arquitectura de Datos", "DireccionTIC", "TIC");
        AddDept("PRT", "Proyectos de Tecnología", "DireccionTIC", "TIC");
        AddDept("DA", "Dirección Administrativa", "Administrativa", "SG");
        AddDept("GD", "Grupo de Gestión Documental", "Administrativa", "DA");
        AddDept("GCO", "Grupo de Contratos", "Administrativa", "DA");
        AddDept("GNA", "Grupo de Notificaciones Administrativas", "Administrativa", "DA");
        AddDept("REC", "Grupo de Relación Estado-Ciudadano", "Administrativa", "DA");
        AddDept("DF", "Dirección Financiera", "Administrativa", "SG");
        AddDept("CAR", "Grupo de Cartera", "Administrativa", "DF");
        AddDept("COB", "Grupo de Cobro Coactivo y Judicial", "Administrativa", "DF");
        AddDept("TES", "Grupo de Tesorería", "Administrativa", "DF");
        AddDept("DTH", "Dirección de Talento Humano", "Administrativa", "SG");
        AddDept("GRE", "Grupo de Registro de Especialistas", "Administrativa", "DTH");
        AddDept("DAES", "Delegatura de Asuntos Económicos y Societarios", "Delegatura", "DES");
        AddDept("DSS", "Delegatura de Supervisión Societaria", "Delegatura", "DES");
        AddDept("DPI", "Delegatura de Procedimientos de Insolvencia", "Delegatura", "DES");
        AddDept("DPM", "Delegatura de Procedimientos Mercantiles", "Delegatura", "DES");
        AddDept("DIAFE", "Delegatura de Intervención y Asuntos Financieros Especiales", "Delegatura", "DES");
        AddDept("IEE", "Información Empresarial y Estudios Económicos y Contables", "Equipo", "DAES");
        AddDept("SE", "Supervisión Empresarial", "Equipo", "DSS");
        AddDept("SAE", "Supervisión de Asuntos Especiales", "Equipo", "DSS");
        AddDept("SCC", "Supervisión de Cámaras de Comercio y Registros Públicos", "Equipo", "DSS");
        AddDept("REO1", "Reorganización I", "Equipo", "DPI");
        AddDept("REO2", "Reorganización II", "Equipo", "DPI");
        AddDept("AIE", "Acuerdos de Insolvencia en Ejecución", "Equipo", "DPI");
        AddDept("LIQ1", "Liquidación I", "Equipo", "DPI");
        AddDept("LIQ2", "Liquidación II", "Equipo", "DPI");
        AddDept("JS", "Jurisdicción Societaria", "Equipo", "DPM");
        AddDept("PE", "Procesos Especiales", "Equipo", "DPM");
        AddDept("IJ", "Intervención Judicial", "Equipo", "DIAFE");
        AddDept("INT-NOR", "Intendencia Norte", "Intendencia", "DES", "norte");
        AddDept("INT-SAR", "Intendencia Santanderes y Arauca", "Intendencia", "DES", "santanderes-y-arauca");
        AddDept("INT-SUR", "Intendencia Sur", "Intendencia", "DES", "sur");
        AddDept("INT-CAR", "Intendencia Caribe y Archipiélago", "Intendencia", "DES", "caribe-y-archipielago");
        AddDept("INT-EJE", "Intendencia Eje Cafetero", "Intendencia", "DES", "eje-cafetero");
        AddDept("INT-OCC", "Intendencia Occidental y Costa Pacífica", "Intendencia", "DES", "occidental-y-costa-pacifica");
        AddDept("DEMO", "Coordinación de demostración", "Administrativa", "TIC");
    }

    private void AddDept(string code, string name, string kind, string? parent, string? region = null)
    {
        db.Departments.Add(new Department
        {
            Id = DemoIds.Of("dep:" + code),
            Code = code,
            Name = name,
            Kind = kind,
            ParentId = parent is null ? null : DemoIds.Of("dep:" + parent),
            RegionCode = region
        });
    }

    private void SeedSecurity()
    {
        foreach (var p in CatalogSeed.Permissions()) db.Permissions.Add(p);
        foreach (var (code, perms) in CatalogSeed.RolePerms)
        {
            var role = new RoleDef
            {
                Id = DemoIds.Of("role:" + code),
                Code = code,
                Name = code.Replace('_', ' '),
                Description = "Perfil demostrativo " + code
            };
            db.Roles.Add(role);
            foreach (var perm in perms.Distinct())
            {
                db.RolePermissions.Add(new RolePermissionLink
                {
                    RoleId = role.Id,
                    PermissionId = DemoIds.Of("perm:" + perm)
                });
            }
        }
    }

    private void SeedCatalogs()
    {
        foreach (var m in CatalogSeed.Modules)
        {
            db.Modules.Add(new ModuleCatalog
            {
                Id = DemoIds.Of("mod:" + m.code),
                Code = m.code, Name = m.name, Pillar = m.pillar, Priority = m.priority,
                Source = m.source, Owner = m.owner, Status = "Operativo en demo"
            });
        }
        foreach (var s in CatalogSeed.Sources)
        {
            db.ExternalSources.Add(new ExternalSource
            {
                Id = DemoIds.Of("src:" + s.row),
                MatrixRow = s.row, DataItem = s.data, Entity = s.entity, Kind = s.kind,
                Channel = s.channel, Modules = s.modules, Status = "Disponible",
                LastCheckedAt = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.FromHours(-5)),
                Freshness = s.row is 4 or 15 ? "Copia en retiro" : "Al día"
            });
        }
        string[] tramites = ["TUTELA", "INSOLV", "REQJUD", "PQRS", "FINREP", "CONTR", "AUX", "CONT", "SOC", "OTROS"];
        string[] names = ["Tutela", "Insolvencia", "Requerimiento judicial", "PQRSD", "Informe financiero", "Contribución", "Auxiliares", "Contratación", "Trámite societario", "Comunicación general"];
        for (var i = 0; i < tramites.Length; i++)
            db.CatalogItems.Add(new CatalogItem { Id = DemoIds.Of("cat:tramite:" + tramites[i]), Catalog = "tramite", Code = tramites[i], Name = names[i], ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) });

        foreach (var (series, sub, years) in new[] { ("Correspondencia", "Entrada", 5), ("Tutelas", "Contestación", 20), ("Insolvencia", "Reorganización", 20), ("Contratos", "OPS", 10) })
            db.RetentionRules.Add(new RetentionRule { Id = DemoIds.Of("trd:" + series + sub), Series = series, Subseries = sub, Years = years });

        db.AssignmentRules.AddRange(
            new AssignmentRule { Id = DemoIds.Of("ar:tut"), Name = "Tutelas a Jurídica", ClassificationCode = "TUTELA", DepartmentId = DemoIds.Of("dep:OAJ"), Priority = "Crítica" },
            new AssignmentRule { Id = DemoIds.Of("ar:ins"), Name = "Insolvencia a la delegatura", ClassificationCode = "INSOLV", DepartmentId = DemoIds.Of("dep:DPI"), Priority = "Alta" },
            new AssignmentRule { Id = DemoIds.Of("ar:pq"), Name = "PQRSD a atención ciudadana", ClassificationCode = "PQRS", DepartmentId = DemoIds.Of("dep:REC"), Priority = "Media" }
        );

        foreach (var d in Holidays2026)
            db.Holidays.Add(new Holiday { Id = DemoIds.Of("hol:" + d.date), Date = DateOnly.Parse(d.date), Name = d.name });

        db.Parameters.AddRange(
            new BusinessParameter { Id = DemoIds.Of("p:uvt"), Key = "UVT_2026", Value = "49800", Description = "UVT demostrativa 2026", ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new BusinessParameter { Id = DemoIds.Of("p:smlv"), Key = "SMLV_2026", Value = "1423500", Description = "Salario mínimo demostrativo", ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new BusinessParameter { Id = DemoIds.Of("p:tut"), Key = "TERMINO_TUTELA_DIAS", Value = "10", Description = "Días hábiles de respuesta de tutela (demo)", ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) }
        );

        db.QualityRules.AddRange(
            new DataQualityRule { Id = DemoIds.Of("qr:dup"), Code = "DUP_TERCERO", Name = "Posibles terceros duplicados", Owner = "D2" },
            new DataQualityRule { Id = DemoIds.Of("qr:exp"), Code = "RAD_SIN_EXP", Name = "Radicado sin expediente", Owner = "J1" },
            new DataQualityRule { Id = DemoIds.Of("qr:aco"), Code = "COHERENCIA_ACUERDO", Name = "Coherencia tras acuerdo", Owner = "D9" },
            new DataQualityRule { Id = DemoIds.Of("qr:pat"), Code = "ECUACION_PATRIMONIAL", Name = "Activo = pasivo + patrimonio", Owner = "D5" }
        );

        var capMap = new Dictionary<string, (string owner, string state)>
        {
            ["Recepcion"] = ("EDI", MigrationStates.TrasladoAutorizado),
            ["Radicacion"] = ("EDI", MigrationStates.TrasladoAutorizado),
            ["Reparto"] = ("EDI", MigrationStates.Paralelo),
            ["ProduccionDocumental"] = ("EDI", MigrationStates.Paralelo),
            ["Expedientes"] = ("GeDeSS", MigrationStates.Inventariado),
            ["Documentos"] = ("GeDeSS", MigrationStates.Inventariado),
            ["Notificaciones"] = ("GeDeSS", MigrationStates.NoIniciado),
            ["Consultas"] = ("GeDeSS", MigrationStates.ConsultaHistorica),
            ["Historicos"] = ("GeDeSS", MigrationStates.ConsultaHistorica)
        };
        foreach (var cap in Capabilities.All)
        {
            var meta = capMap.GetValueOrDefault(cap, ("GeDeSS", MigrationStates.NoIniciado));
            db.Capabilities.Add(new CapabilityOwnership
            {
                Id = DemoIds.Of("cap:" + cap),
                Capability = cap,
                Owner = meta.Item1,
                MigrationState = meta.Item2,
                UpdatedAt = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.FromHours(-5))
            });
        }
    }

    private void SeedPeople()
    {
        var named = NamedUsers();
        foreach (var u in named) AddUser(u);
        var extras = new (string name, string dept, string profile, string home, string pos, string? region)[]
        {
            ("Sofía Delgado Rueda", "GD", Profiles.Radicador, HomeExperiences.Operativo, "Radicadora", null),
            ("Camilo Reyes Pardo", "GD", Profiles.Radicador, HomeExperiences.Operativo, "Radicador", null),
            ("Elena Duarte Silva", "OAJ", Profiles.Abogado, HomeExperiences.Operativo, "Abogada", null),
            ("Tomás Gil Navarro", "OAJ", Profiles.Abogado, HomeExperiences.Operativo, "Abogado", null),
            ("María Isabel Fonseca", "REO1", Profiles.Sustanciador, HomeExperiences.Operativo, "Sustanciadora", null),
            ("Pedro Arias Cano", "REO2", Profiles.Sustanciador, HomeExperiences.Operativo, "Sustanciador", null),
            ("Luisa Fernanda Mora", "LIQ1", Profiles.Sustanciador, HomeExperiences.Operativo, "Sustanciadora", null),
            ("Héctor Núñez Salas", "AIE", Profiles.Acuerdos, HomeExperiences.Operativo, "Profesional de acuerdos", null),
            ("Angela Pineda Cruz", "SE", Profiles.Supervisor, HomeExperiences.Operativo, "Supervisora", null),
            ("Nicolás Barreto Díaz", "SE", Profiles.Supervisor, HomeExperiences.Operativo, "Supervisor", null),
            ("Carolina Vega Londoño", "IEE", Profiles.Analista, HomeExperiences.Operativo, "Analista contable", null),
            ("Esteban Rueda Naranjo", "IEE", Profiles.Analista, HomeExperiences.Operativo, "Analista económico", null),
            ("Pilar Castaño Mejía", "GRE", Profiles.Especialistas, HomeExperiences.Operativo, "Profesional de registro", null),
            ("Raúl Mendoza Ortiz", "INT-SAR", Profiles.Intendente, HomeExperiences.Delegado, "Intendente", "santanderes-y-arauca"),
            ("Gloria Pacífico Ruiz", "INT-SUR", Profiles.Intendente, HomeExperiences.Delegado, "Intendente", "sur"),
            ("Ivonne Castro Marín", "INT-CAR", Profiles.Intendente, HomeExperiences.Delegado, "Intendente", "caribe-y-archipielago"),
            ("Fabián Orozco Londoño", "INT-EJE", Profiles.Intendente, HomeExperiences.Delegado, "Intendente", "eje-cafetero"),
            ("Yamile Palacios Córdoba", "INT-OCC", Profiles.Intendente, HomeExperiences.Delegado, "Intendente", "occidental-y-costa-pacifica"),
            ("Adriana López Cifuentes", "CAR", Profiles.FinancieraConsulta, HomeExperiences.Operativo, "Analista de cartera", null),
            ("Sergio Tamayo Prieto", "CAR", Profiles.FinancieraLiquidacion, HomeExperiences.Operativo, "Liquidador de contribución", null),
            ("Mónica Henao Rubio", "TES", Profiles.FinancieraAprobacion, HomeExperiences.Coordinador, "Coordinadora de tesorería", null),
            ("Alberto Cuéllar Díaz", "OCI", Profiles.ControlInterno, HomeExperiences.Coordinador, "Auditor", null),
            ("Lucía Ramírez Soto", "OCDI", Profiles.ControlDisciplinario, HomeExperiences.Operativo, "Profesional disciplinaria", null),
            ("Gabriel Torres Quintero", "OAP", Profiles.Planeacion, HomeExperiences.Coordinador, "Profesional de planeación", null),
            ("Daniela Ortiz Buitrago", "TIC", Profiles.DireccionTic, HomeExperiences.Coordinador, "Arquitecta de aplicaciones", null),
            ("Juan Pablo Cárdenas", "SAT", Profiles.DireccionTic, HomeExperiences.Operativo, "Ingeniero de operación", null),
            ("Kelly Andrea Muñoz", "GCO", Profiles.SecretarioGeneral, HomeExperiences.Operativo, "Profesional de contratos", null),
            ("Oscar Iván Belalcázar", "GNA", Profiles.DirectorAdministrativo, HomeExperiences.Operativo, "Notificador", null)
        };
        foreach (var e in extras) AddUser(e);
    }

    private static (string name, string dept, string profile, string home, string pos, string? region)[] NamedUsers() =>
    [
        ("Camila Restrepo Duque", "DES", Profiles.Superintendente, HomeExperiences.AltaDireccion, "Superintendente", null),
        ("Andrés Palacio Mejía", "SG", Profiles.SecretarioGeneral, HomeExperiences.AltaDireccion, "Secretario General", null),
        ("Helena Quiroga Vargas", "DA", Profiles.DirectorAdministrativo, HomeExperiences.Delegado, "Directora Administrativa", null),
        ("Mauricio Peña Cárdenas", "GD", Profiles.CoordDocumental, HomeExperiences.Coordinador, "Coordinador de Gestión Documental", null),
        ("Laura Jiménez Soto", "GD", Profiles.Radicador, HomeExperiences.Operativo, "Radicadora", null),
        ("Beatriz Molina Ríos", "OAJ", Profiles.JefeJuridica, HomeExperiences.Coordinador, "Jefa de la Oficina Jurídica", null),
        ("Felipe Ardila Cruz", "OAJ", Profiles.Abogado, HomeExperiences.Operativo, "Abogado de tutelas", null),
        ("Ricardo Sáenz Ocampo", "DPI", Profiles.DelegadoInsolvencia, HomeExperiences.Delegado, "Delegado de Insolvencia", null),
        ("Natalia Gómez Parra", "REO1", Profiles.Sustanciador, HomeExperiences.Operativo, "Sustanciadora", null),
        ("Óscar Villamizar León", "DPI", Profiles.Jurisdiccional, HomeExperiences.Delegado, "Funcionario jurisdiccional", null),
        ("Diana Castro Pinzón", "AIE", Profiles.Acuerdos, HomeExperiences.Coordinador, "Coordinadora de acuerdos en ejecución", null),
        ("Julián Herrera Nieto", "DSS", Profiles.Supervisor, HomeExperiences.Coordinador, "Supervisor societario", null),
        ("Paula Rincón Ávila", "IEE", Profiles.Analista, HomeExperiences.Operativo, "Analista económica y contable", null),
        ("Sandra Ortiz Beltrán", "GRE", Profiles.Especialistas, HomeExperiences.Coordinador, "Coordinadora de registro de especialistas", null),
        ("Iván Cárdenas Luján", "INT-NOR", Profiles.Intendente, HomeExperiences.Delegado, "Intendente regional", "norte"),
        ("Marta Suárez Patiño", "DF", Profiles.FinancieraConsulta, HomeExperiences.Delegado, "Directora Financiera (consulta)", null),
        ("Jorge Medina Rojas", "CAR", Profiles.FinancieraLiquidacion, HomeExperiences.Operativo, "Profesional de liquidación", null),
        ("Elena Vargas Ruiz", "DF", Profiles.FinancieraAprobacion, HomeExperiences.Coordinador, "Coordinadora de pagos", null),
        ("Patricia León Cifuentes", "OCI", Profiles.ControlInterno, HomeExperiences.Delegado, "Jefa de Control Interno", null),
        ("Roberto Aguilar Niño", "OCDI", Profiles.ControlDisciplinario, HomeExperiences.Delegado, "Jefe de Control Disciplinario", null),
        ("Claudia Bernal Ospina", "OAP", Profiles.Planeacion, HomeExperiences.Delegado, "Jefa de Planeación", null),
        ("Diego Torres Quintero", "TIC", Profiles.DireccionTic, HomeExperiences.Delegado, "Director TIC", null),
        ("Valeria Santamaría López", "DEMO", Profiles.DemoAdmin, HomeExperiences.AltaDireccion, "Administradora de demostración", null)
    ];

    private void AddUser((string name, string dept, string profile, string home, string pos, string? region) u)
    {
        var id = DemoIds.Of("user:" + u.name);
        var userName = UserNameOf(u.name);
        var user = new UserAccount
        {
            Id = id,
            UserName = userName,
            DisplayName = u.name,
            Email = userName + "@demo.edi360.gov.co",
            DepartmentId = DemoIds.Of("dep:" + u.dept),
            Position = u.pos,
            ProfileCode = u.profile,
            HomeExperience = u.home,
            RegionalScope = u.region,
            Active = true
        };
        user.PasswordHash = AuthService.HashPassword(AuthService.DemoPassword);
        db.Users.Add(user);
        db.UserRoles.Add(new UserRoleLink { UserId = id, RoleId = DemoIds.Of("role:" + u.profile) });
    }

    private void SeedSocietiesAndParties()
    {
        var adjectives = new[] { "Andina", "Pacific", "Norte", "Altamira", "Santa Fe", "Aurora", "Delta", "Cumbre", "Llanera", "Café", "Minera", "Portuaria", "Textil", "Agro", "Tech" };
        var nouns = new[] { "Inversiones", "Comercial", "Logística", "Alimentos", "Construcciones", "Energía", "Salud", "Transportes", "Servicios", "Holding", "Industrias", "Agropecuaria", "Financiera", "Editorial", "Tecnología" };
        var forms = new[] { "S.A.S.", "S.A.", "Ltda." };
        var ciius = new[] { ("0111", "Café"), ("4100", "Construcción"), ("4711", "Comercio"), ("4921", "Transporte"), ("6201", "Software"), ("3511", "Energía"), ("8610", "Salud"), ("6412", "Financiera") };
        var sizes = new[] { "Micro", "Pequeña", "Mediana", "Grande" };
        var regions = db.Regions.Local.ToList();
        var supervisor = DemoIds.Of("user:Julián Herrera Nieto");

        for (var i = 1; i <= 200; i++)
        {
            var ciiu = ciius[i % ciius.Length];
            var status = i <= 80 ? (i % 3 == 0 ? "En liquidación" : "En reorganización") : "Activa";
            var situation = status switch
            {
                "En liquidación" => "Liquidación en curso",
                "En reorganización" when i % 5 == 0 => "Acuerdo en ejecución",
                "En reorganización" => "En trámite de reorganización",
                _ => "Normal"
            };
            db.Societies.Add(new Society
            {
                Id = DemoIds.Of("soc:" + i),
                Nit = $"900{i:000000}-{(i % 9) + 1}",
                Name = $"{adjectives[i % adjectives.Length]} {nouns[i % nouns.Length]} {forms[i % forms.Length]}",
                LegalForm = forms[i % forms.Length],
                Status = status,
                Situation = situation,
                EconomicActivity = ciiu.Item2,
                Ciiu = ciiu.Item1,
                Size = sizes[i % sizes.Length],
                RegionId = regions[i % regions.Count].Id,
                SupervisorUserId = supervisor,
                CreatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i),
                DataOrigin = "D1 maestro demo / RUES simulado"
            });
        }

        var first = new[] { "Ana", "Luis", "Marta", "Carlos", "Diana", "Jorge", "Elena", "Pablo", "Rosa", "Hugo", "Nuria", "Iván", "Teresa", "Raúl", "Olga" };
        var last = new[] { "Gutiérrez", "Pardo", "Castaño", "Rincón", "Arias", "Beltrán", "Nieto", "Ocampo", "Serrano", "Vallejo", "Cifuentes", "Montoya", "Quiroga", "Sarmiento", "Peláez" };
        for (var i = 1; i <= 400; i++)
        {
            var name = $"{first[i % first.Length]} {last[(i * 3) % last.Length]} {last[(i * 7) % last.Length]}";
            var dup = i is 12 or 13 or 88 or 89;
            db.ThirdParties.Add(new ThirdParty
            {
                Id = DemoIds.Of("tp:" + i),
                DocumentType = i % 17 == 0 ? "NIT" : "CC",
                DocumentNumber = dup ? $"10{(i < 50 ? 12 : 88):0000000}" : $"1{i:00000000}",
                FullName = dup && i % 2 == 1 ? name + " (posible duplicado)" : name,
                Kind = i % 17 == 0 ? "Jurídica" : "Natural",
                Email = $"tercero.{i}@correo-demo.co",
                Phone = $"310{i:0000000}",
                PossibleDuplicate = dup,
                CreatedAt = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i),
                DataOrigin = "D2 maestro demo"
            });
        }

        for (var i = 1; i <= 200; i++)
        {
            db.SocietyParties.Add(new SocietyParty
            {
                Id = DemoIds.Of("sp:" + i),
                SocietyId = DemoIds.Of("soc:" + i),
                ThirdPartyId = DemoIds.Of("tp:" + i),
                Role = "Representante legal",
                ValidFrom = new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
        }
    }

    private void SeedFinancials()
    {
        for (var i = 1; i <= 200; i++)
        {
            foreach (var year in new[] { 2024, 2025 })
            {
                if (db.FinancialReports.Local.Count >= 250 && year == 2024 && i > 50) continue;
                var assets = 800_000_000m + i * 3_500_000m;
                var liab = i % 11 == 0 ? assets + 40_000_000m : assets * 0.55m;
                var equity = assets - liab;
                var income = 120_000_000m + i * 800_000m;
                var result = i % 11 == 0 ? -18_000_000m - i * 1000 : income * 0.08m;
                db.FinancialReports.Add(new FinancialReport
                {
                    Id = DemoIds.Of($"fin:{i}:{year}"),
                    SocietyId = DemoIds.Of("soc:" + i),
                    Period = year.ToString(),
                    Source = "XBRL demo",
                    Unit = "Pesos",
                    Status = i % 23 == 0 && year == 2025 ? "Borrador" : "Aprobado",
                    Assets = assets, Liabilities = liab, Equity = equity, Income = income, Result = result,
                    SubmittedAt = new DateTimeOffset(year + 1, 4, 15, 0, 0, 0, TimeSpan.Zero),
                    ApprovedById = DemoIds.Of("user:Paula Rincón Ávila")
                });
            }
        }

        for (var i = 1; i <= 100; i++)
        {
            var rule = i switch
            {
                <= 20 => ("PATRIMONIO_NEGATIVO", "Alta", "Patrimonio negativo en el último reporte aprobado."),
                <= 35 => ("PASIVO_Y_PERDIDA", "Alta", "Aumento de pasivos con pérdida neta."),
                <= 45 => ("FALTA_REPORTE", "Media", "Sin reporte financiero del periodo esperado."),
                <= 60 => ("MORA_CONTRIBUCION", "Media", "Contribución en mora."),
                <= 75 => ("PATRIMONIO_RECURRENTE", "Alta", "Patrimonio negativo en varios periodos."),
                <= 88 => ("INCUMPLIMIENTO_ACUERDO", "Alta", "Cuota de acuerdo vencida."),
                _ => ("INCONSISTENCIA", "Baja", "Cifras que no cuadran la ecuación patrimonial.")
            };
            db.Alerts.Add(new RiskAlert
            {
                Id = DemoIds.Of("alert:" + i),
                RuleCode = rule.Item1,
                RuleVersion = "1.0",
                SocietyId = DemoIds.Of("soc:" + i),
                TriggerData = rule.Item3,
                RaisedAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.FromHours(-5)).AddDays(i),
                Level = rule.Item2,
                OwnerUserId = DemoIds.Of("user:Julián Herrera Nieto"),
                Status = i % 7 == 0 ? "En revisión" : "Abierta"
            });
        }

        for (var i = 1; i <= 50; i++)
        {
            var amount = 4_200_000m + i * 15000;
            var paid = i <= 12 ? 0 : amount;
            db.Contributions.Add(new Contribution
            {
                Id = DemoIds.Of("contrib:" + i),
                SocietyId = DemoIds.Of("soc:" + i),
                Year = 2026,
                Amount = amount,
                Paid = paid,
                Status = paid == 0 ? "En mora" : "Al día",
                DueDate = new DateOnly(2026, 5, 31)
            });
            if (paid > 0)
            {
                db.Payments.Add(new Payment
                {
                    Id = DemoIds.Of("pay:" + i),
                    ContributionId = DemoIds.Of("contrib:" + i),
                    Amount = paid,
                    PaidAt = new DateTimeOffset(2026, 4, 20, 0, 0, 0, TimeSpan.Zero).AddDays(i),
                    Reference = $"PSE-DEMO-{i:0000}",
                    Status = "Registrado"
                });
            }
        }

        for (var i = 1; i <= 20; i++)
        {
            db.Sanctions.Add(new Sanction
            {
                Id = DemoIds.Of("san:" + i),
                SocietyId = DemoIds.Of("soc:" + (i + 10)),
                Number = $"SAN-DEMO-2025-{i:000}",
                Type = i % 2 == 0 ? "Multa" : "Amonestación",
                Status = i <= 16 ? "Firme" : "En recurso",
                IssuedOn = new DateOnly(2025, 6, 1).AddDays(i * 3),
                FirmOn = i <= 16 ? new DateOnly(2025, 8, 1).AddDays(i) : null,
                Amount = i % 2 == 0 ? 25_000_000m + i * 100000 : null,
                Summary = "Sanción demostrativa. No corresponde a un caso real.",
                RestrictsAppointment = i % 5 == 0
            });
        }
    }

    private async Task SeedCasesAndRecords(CancellationToken ct)
    {
        var channels = new[] { "Correo", "Ventanilla", "Mensajería", "Formulario" };
        var statuses = new[] { "Radicado", "En reparto", "Asignado", "En gestión", "Respondido", "Cerrado" };
        var engine = new Application.LocalAssistanceEngine();
        var laura = DemoIds.Of("user:Laura Jiménez Soto");
        var felipe = DemoIds.Of("user:Felipe Ardila Cruz");
        var natalia = DemoIds.Of("user:Natalia Gómez Parra");
        var now = new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.FromHours(-5));

        for (var i = 1; i <= 120; i++)
        {
            var insolvency = i <= 80;
            var reserved = i is 119 or 120;
            var region = db.Regions.Local.ElementAt((i - 1) % db.Regions.Local.Count);
            db.Cases.Add(new CaseFile
            {
                Id = DemoIds.Of("case:" + i),
                Number = insolvency ? $"INS-2026-{i:0000}" : reserved ? $"DIS-2026-{i:0000}" : $"EXP-2026-{i:0000}",
                Type = reserved ? "Disciplinario" : insolvency ? "Insolvencia" : i % 5 == 0 ? "Tutela" : "Societario",
                Subtype = reserved ? "Reserva interna" : insolvency ? (i % 3 == 0 ? "Liquidación" : "Reorganización") : "General",
                SocietyId = DemoIds.Of("soc:" + i),
                DepartmentId = reserved ? DemoIds.Of("dep:OCDI") : insolvency ? DemoIds.Of("dep:DPI") : DemoIds.Of("dep:OAJ"),
                AssigneeUserId = reserved ? DemoIds.Of("user:Roberto Aguilar Niño") : insolvency ? natalia : felipe,
                Stage = insolvency ? (i % 5 == 0 ? "Ejecución del acuerdo" : "Admisión") : "Trámite",
                Status = i % 17 == 0 ? "Cerrado" : "En trámite",
                Outcome = i % 17 == 0 ? "Cierre demostrativo" : null,
                Priority = i % 9 == 0 ? "Crítica" : "Media",
                OpenedAt = now.AddDays(-120 + i),
                ClosedAt = i % 17 == 0 ? now.AddDays(-10) : null,
                DueAt = now.AddDays(20 - (i % 15)),
                RegionId = region.Id,
                Classification = reserved ? "Reservado" : "Ordinario",
                Title = insolvency ? "Proceso de insolvencia demostrativo" : reserved ? "Expediente disciplinario reservado" : "Expediente institucional",
                Summary = "Expediente de demostración con datos ficticios."
            });
            db.CaseParties.Add(new CaseParty
            {
                Id = DemoIds.Of("cp:" + i),
                CaseId = DemoIds.Of("case:" + i),
                ThirdPartyId = DemoIds.Of("tp:" + i),
                Role = "Solicitante"
            });
            if (insolvency && i % 5 == 0)
            {
                var agr = new InsolvencyAgreement
                {
                    Id = DemoIds.Of("agr:" + i),
                    CaseId = DemoIds.Of("case:" + i),
                    ConfirmedAt = now.AddDays(-40),
                    Status = "Vigente",
                    TotalAmount = 900_000_000m + i * 1000,
                    Notes = "Acuerdo demostrativo. No implica recuperación automática."
                };
                db.Agreements.Add(agr);
                for (var q = 1; q <= 6; q++)
                {
                    db.Installments.Add(new AgreementInstallment
                    {
                        Id = DemoIds.Of($"ins:{i}:{q}"),
                        AgreementId = agr.Id,
                        Number = q,
                        DueDate = new DateOnly(2026, 6, 1).AddMonths(q),
                        Amount = agr.TotalAmount / 6,
                        Status = q == 1 ? "Pagada" : q == 2 && i % 10 == 0 ? "Vencida" : "Pendiente",
                        PaidOn = q == 1 ? new DateOnly(2026, 7, 2) : null
                    });
                }
            }
            if (i <= 40)
            {
                db.Hearings.Add(new Hearing
                {
                    Id = DemoIds.Of("hear:" + i),
                    CaseId = DemoIds.Of("case:" + i),
                    ScheduledAt = now.AddDays(i % 20),
                    Kind = "Audiencia demostrativa",
                    Status = "Programada"
                });
            }
        }

        for (var i = 1; i <= 500; i++)
        {
            var subject = SubjectFor(i);
            var body = $"Comunicación ficticia número {i}. {subject}. Contenido de demostración, sin datos reales.";
            var suggestion = engine.Classify(subject, body);
            var dept = DemoIds.Of("dep:" + suggestion.departmentCode);
            var tutela = suggestion.tutela || i % 40 == 0;
            var received = now.AddDays(-80).AddHours(i % 24);
            var filed = received.AddHours(2);
            var caseId = i <= 120 ? DemoIds.Of("case:" + i) : (i <= 180 ? DemoIds.Of("case:" + ((i % 120) + 1)) : (Guid?)null);
            db.Records.Add(new FilingRecord
            {
                Id = DemoIds.Of("rec:" + i),
                Number = $"EDI-2026-{i:000000}",
                Channel = channels[i % channels.Length],
                Direction = i % 11 == 0 ? "Salida" : "Entrada",
                ReceivedAt = received,
                FiledAt = filed,
                IncorporatedAt = caseId is null ? null : filed.AddHours(5),
                Subject = subject,
                Body = body,
                Status = statuses[i % statuses.Length],
                SenderThirdPartyId = DemoIds.Of("tp:" + ((i % 400) + 1)),
                SocietyId = DemoIds.Of("soc:" + ((i % 200) + 1)),
                ClassificationCode = suggestion.code,
                ClassificationName = suggestion.name,
                SuggestedClassification = suggestion.name,
                SuggestionExplanation = suggestion.explanation,
                SuggestionDecision = "Aceptada",
                DepartmentId = dept,
                AssigneeUserId = tutela ? felipe : i % 3 == 0 ? natalia : laura,
                DueAt = tutela ? filed.AddDays(10) : filed.AddDays(15),
                Priority = tutela ? "Crítica" : suggestion.priority,
                IsTutela = tutela,
                CaseId = caseId,
                DataOrigin = "EDI Documental"
            });
            db.Assignments.Add(new Assignment
            {
                Id = DemoIds.Of("asg:" + i),
                SubjectType = "Radicado",
                SubjectId = DemoIds.Of("rec:" + i),
                DepartmentId = dept,
                UserId = tutela ? felipe : laura,
                Status = "Activa",
                AssignedAt = filed
            });
            if (i <= 220)
            {
                db.Tasks.Add(new WorkTask
                {
                    Id = DemoIds.Of("task:" + i),
                    Title = tutela ? "Contestar tutela" : "Gestionar radicado " + $"EDI-2026-{i:000000}",
                    Kind = tutela ? "Tutela" : "Radicado",
                    RecordId = DemoIds.Of("rec:" + i),
                    CaseId = caseId,
                    AssigneeUserId = tutela ? felipe : natalia,
                    DepartmentId = dept,
                    Status = i % 6 == 0 ? "Cerrada" : "Pendiente",
                    DueAt = tutela ? filed.AddDays(10) : filed.AddDays(12),
                    CreatedAt = filed,
                    Priority = tutela ? "Crítica" : "Media"
                });
            }
        }

        for (var i = 1; i <= 300; i++)
        {
            var recId = DemoIds.Of("rec:" + i);
            var docId = DemoIds.Of("doc:" + i);
            var bytes = Encoding.UTF8.GetBytes($"Documento demostrativo {i}\n{SubjectFor(i)}\nSin datos reales.");
            var fileName = $"documento-{i:000}.txt";
            var full = await StoreAsync(Path.Combine("docs", i.ToString("000")), fileName, bytes, ct);
            db.Documents.Add(new StoredDocument
            {
                Id = docId,
                Title = "Documento " + i,
                DocumentType = i % 9 == 0 ? "Providencia" : "Anexo",
                RecordId = recId,
                CaseId = i <= 120 ? DemoIds.Of("case:" + i) : null,
                Classification = "Ordinario",
                CurrentVersion = 1,
                CreatedById = laura,
                CreatedAt = now.AddDays(-70 + (i % 60))
            });
            db.DocumentVersions.Add(new DocumentVersion
            {
                Id = DemoIds.Of("dv:" + i),
                DocumentId = docId,
                VersionNumber = 1,
                FileName = fileName,
                ContentType = "text/plain",
                Size = bytes.Length,
                StoragePath = full,
                Sha256 = Convert.ToHexString(SHA256.HashData(bytes)),
                CreatedById = laura,
                CreatedAt = now.AddDays(-70 + (i % 60)),
                ChangeNote = "Versión inicial"
            });
        }

        for (var i = 1; i <= 80; i++)
        {
            db.LegalActions.Add(new LegalAction
            {
                Id = DemoIds.Of("act:" + i),
                CaseId = DemoIds.Of("case:" + i),
                RecordId = DemoIds.Of("rec:" + i),
                Type = i % 4 == 0 ? "Providencia" : "Actuación",
                Title = i % 4 == 0 ? "Auto demostrativo" : "Actuación de trámite",
                Body = "Registro de demostración. No es una decisión real.",
                OccurredAt = now.AddDays(-50 + i),
                ActorUserId = natalia,
                Status = "Registrada"
            });
        }

        for (var i = 1; i <= 40; i++)
        {
            var n = new CaseNotification
            {
                Id = DemoIds.Of("not:" + i),
                RecordId = DemoIds.Of("rec:" + i),
                CaseId = DemoIds.Of("case:" + i),
                ThirdPartyId = DemoIds.Of("tp:" + i),
                Channel = "Simulado",
                Status = "Enviada",
                CreatedAt = now.AddDays(-10),
                Subject = "Notificación demostrativa"
            };
            db.Notifications.Add(n);
            db.Acknowledgements.Add(new Acknowledgement { Id = DemoIds.Of("ack:e:" + i), NotificationId = n.Id, Kind = "Envio", OccurredAt = n.CreatedAt, Result = "Simulado" });
            db.Acknowledgements.Add(new Acknowledgement { Id = DemoIds.Of("ack:d:" + i), NotificationId = n.Id, Kind = "Entrega", OccurredAt = n.CreatedAt.AddHours(6), Result = i % 7 == 0 ? "Fallida" : "Simulado" });
            if (i % 7 != 0)
                db.Acknowledgements.Add(new Acknowledgement { Id = DemoIds.Of("ack:a:" + i), NotificationId = n.Id, Kind = "Apertura", OccurredAt = n.CreatedAt.AddHours(12), Result = "Simulado" });
        }

        for (var i = 1; i <= 12; i++)
        {
            var status = i switch { <= 4 => "Completa", <= 8 => "Incompleta", _ => "Desactualizada" };
            var html = i switch
            {
                <= 4 => ProcessPageHtml(i, complete: true, fresh: true),
                <= 8 => ProcessPageHtml(i, complete: false, fresh: true),
                _ => ProcessPageHtml(i, complete: true, fresh: false)
            };
            var path = await StoreAsync("pages", $"proceso-{i:00}.html", Encoding.UTF8.GetBytes(html), ct);
            db.ProcessPages.Add(new ProcessWebPage
            {
                Id = DemoIds.Of("page:" + i),
                CaseId = DemoIds.Of("case:" + i),
                Title = $"Página del proceso INS-2026-{i:0000}",
                StoragePath = path,
                Status = status,
                LastReviewedAt = now.AddDays(-i),
                ChecklistJson = status == "Completa" ? "{\"auto\":true,\"estado\":true,\"auxiliar\":true}" : "{\"auto\":true,\"estado\":false,\"auxiliar\":false}",
                Findings = status == "Completa" ? "Cumple la lista de chequeo demostrativa." : "Faltan piezas o fechas desactualizadas. No genera sanción automática."
            });
        }

        for (var i = 1; i <= 15; i++)
        {
            db.Procedures.Add(new SocietyProcedure
            {
                Id = DemoIds.Of("proc:" + i),
                Number = $"TRM-2026-{i:000}",
                Type = i % 2 == 0 ? "Reforma estatutaria" : "Autorización de disminución de capital",
                SocietyId = DemoIds.Of("soc:" + (100 + i)),
                Status = i <= 5 ? "En revisión" : i <= 10 ? "En aprobación" : "Cerrado",
                ApprovalStep = Math.Min(i % 4, 3),
                AssigneeUserId = DemoIds.Of("user:Helena Quiroga Vargas"),
                OpenedAt = now.AddDays(-30 + i),
                DueAt = now.AddDays(10)
            });
        }
    }

    private void SeedAuxiliariesContracts()
    {
        for (var i = 1; i <= 40; i++)
        {
            var blocked = i is 7 or 19;
            db.Auxiliaries.Add(new Auxiliary
            {
                Id = DemoIds.Of("aux:" + i),
                ThirdPartyId = DemoIds.Of("tp:" + (200 + i)),
                Profession = i % 2 == 0 ? "Abogado" : "Contador",
                ProfessionalCard = blocked ? "SIN-DATO" : $"TP-DEMO-{i:00000}",
                PolicyNumber = $"POL-DEMO-{i:000}",
                PolicyUntil = new DateOnly(2027, 6, 30),
                Status = blocked ? "Bloqueado" : "Habilitado",
                Formation = "Especialización demostrativa en derecho comercial",
                Experience = $"{8 + i % 10} años en procesos de insolvencia (ficticios)",
                BackgroundOk = !blocked,
                BlockReason = blocked ? "Tarjeta profesional inconsistente. Debe corregirse antes de designar." : null
            });
        }
        for (var i = 1; i <= 18; i++)
        {
            db.Appointments.Add(new AuxiliaryAppointment
            {
                Id = DemoIds.Of("appt:" + i),
                AuxiliaryId = DemoIds.Of("aux:" + i),
                CaseId = DemoIds.Of("case:" + i),
                AppointedAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i),
                Role = i % 2 == 0 ? "Promotor" : "Liquidador",
                Status = "Vigente",
                DrawCriteria = "Sorteo demostrativo por especialidad y región",
                Rating = i % 5 == 0 ? 4 : null
            });
        }
        for (var i = 1; i <= 30; i++)
        {
            var c = new Contract
            {
                Id = DemoIds.Of("ctr:" + i),
                Number = $"CTR-DEMO-2026-{i:000}",
                Object = "Prestación de servicios profesionales de apoyo (demostración)",
                ContractorThirdPartyId = DemoIds.Of("tp:" + (300 + i)),
                Value = 40_000_000m + i * 1_000_000m,
                StartDate = new DateOnly(2026, 2, 1),
                EndDate = new DateOnly(2026, 12, 31),
                Status = i <= 10 ? "En ejecución" : i <= 20 ? "En elaboración" : "Liquidado",
                SupervisorUserId = DemoIds.Of("user:Andrés Palacio Mejía"),
                PolicyVerified = i <= 10,
                StartActIssued = i <= 10,
                SocialSecurityOk = i <= 12
            };
            db.Contracts.Add(c);
            db.ContractPolicies.Add(new ContractPolicy
            {
                Id = DemoIds.Of("cpol:" + i),
                ContractId = c.Id,
                Number = $"GAR-DEMO-{i:000}",
                Insurer = "Aseguradora Demo S.A.",
                Status = i <= 10 ? "Verificada" : "Pendiente",
                VerifiedAt = i <= 10 ? new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero) : null,
                VerificationResult = i <= 10 ? "Póliza vigente en el simulador." : null
            });
        }
    }

    private void SeedQualityIntegrations()
    {
        db.QualityIssues.AddRange(
            new DataQualityIssue { Id = DemoIds.Of("qi:1"), RuleId = DemoIds.Of("qr:dup"), SubjectType = "ThirdParty", SubjectId = DemoIds.Of("tp:12"), Description = "Dos terceros con el mismo documento. Requiere fusión humana.", Status = "Abierta", DetectedAt = DateTimeOffset.Now.AddDays(-12) },
            new DataQualityIssue { Id = DemoIds.Of("qi:2"), RuleId = DemoIds.Of("qr:exp"), SubjectType = "FilingRecord", SubjectId = DemoIds.Of("rec:400"), Description = "Radicado sin expediente asociado (patrón heredado).", Status = "Abierta", DetectedAt = DateTimeOffset.Now.AddDays(-3) }
        );
        foreach (var src in db.ExternalSources.Local.ToList())
        {
            db.IntegrationAttempts.Add(new IntegrationAttempt
            {
                Id = DemoIds.Of("ia:" + src.MatrixRow),
                SourceId = src.Id,
                AttemptedAt = new DateTimeOffset(2026, 11, 21, 7, 30, 0, TimeSpan.FromHours(-5)),
                Mode = src.MatrixRow == 4 ? "Discrepancia" : "Correcto",
                Result = src.MatrixRow == 4 ? "Fuente marcada para retiro." : "Respuesta simulada correcta.",
                LatencyMs = 80 + src.MatrixRow * 3
            });
        }
        db.Incidents.Add(new SecurityIncident
        {
            Id = DemoIds.Of("inc:1"),
            DetectedAt = new DateTimeOffset(2026, 11, 18, 21, 0, 0, TimeSpan.FromHours(-5)),
            Title = "Intento de descarga masiva fuera de horario (demo)",
            Severity = "Media",
            Status = "Cerrado",
            Detail = "El motor local marcó el patrón. No hubo extracción real."
        });
    }

    private void SeedReportsAndDemo()
    {
        var claudia = DemoIds.Of("user:Claudia Bernal Ospina");
        foreach (var r in CatalogSeed.Reports)
        {
            db.Reports.Add(new GeneratedReport
            {
                Id = DemoIds.Of("rep:" + r.n),
                CatalogNumber = r.n,
                Type = r.type,
                Audience = r.audience,
                Period = "Noviembre 2026",
                Status = r.n <= 3 ? "Borrador" : "Pendiente",
                Draft = "",
                Sources = "Tableros y registros del entorno demo",
                OwnerUserId = claudia,
                CreatedAt = new DateTimeOffset(2026, 11, 19, 8, 0, 0, TimeSpan.FromHours(-5)),
                Version = "1"
            });
        }
        db.DemoStates.Add(new DemoState
        {
            Id = DemoIds.Of("demo:state"),
            BusinessClock = new DateTimeOffset(2026, 11, 22, 9, 30, 0, TimeSpan.FromHours(-5))
        });
        db.MigrationBatches.Add(new MigrationBatch
        {
            Id = DemoIds.Of("batch:historico-1"),
            Name = "Lote histórico 2019-2022",
            Status = MigrationStates.NoIniciado,
            LegacyCount = 100,
            PendingCount = 100,
            CreatedAt = new DateTimeOffset(2026, 11, 22, 8, 0, 0, TimeSpan.FromHours(-5))
        });
    }

    private async Task SeedLegacy(CancellationToken ct)
    {
        for (var i = 1; i <= 100; i++)
        {
            var inconsistent = i % 6 == 0;
            var dup = i is 21 or 22;
            var noCase = i % 4 == 0;
            var bytes = Encoding.UTF8.GetBytes($"GEDESS heredado {i}");
            var path = await StoreAsync("legacy", $"gd-{i:000}.txt", bytes, ct);
            legacy.Records.Add(new LegacyRecord
            {
                Id = DemoIds.Of("leg:" + i),
                LegacyNumber = $"GD-2019-{45000 + i}",
                AlternateNumber = inconsistent ? $"GD/19/{i}" : null,
                Channel = i % 3 == 0 ? "Correo" : "Ventanilla",
                ArrivalDate = new DateTimeOffset(2019, 3, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i),
                FilingDate = new DateTimeOffset(2019, 3, 2, 0, 0, 0, TimeSpan.Zero).AddDays(i),
                CaseDate = noCase ? null : new DateTimeOffset(2019, 3, 10, 0, 0, 0, TimeSpan.Zero).AddDays(i),
                Subject = "Radicado histórico ficticio " + i,
                SenderName = i % 5 == 0 ? "Remitente duplicado Demo" : $"Remitente histórico {i}",
                SenderDocument = dup ? "1012000000" : $"9{i:00000000}",
                SocietyName = $"Sociedad heredada {i} S.A.S.",
                SocietyNit = inconsistent ? $"NIT {i}" : $"800{i:000000}-1",
                CaseNumber = noCase ? null : $"EXP-OLD-{i:000}",
                Status = inconsistent ? "SIN ESTADO" : "Radicado",
                AssigneeName = i % 7 == 0 ? null : "Funcionario heredado",
                DepartmentName = i % 8 == 0 ? "" : "Gestión Documental",
                HasCase = !noCase,
                HasDocument = i % 9 != 0,
                DocumentName = i % 9 == 0 ? null : $"gd-{i:000}.txt",
                DocumentHash = Convert.ToHexString(SHA256.HashData(bytes)),
                DocumentPath = path,
                DuplicateSuspect = dup,
                QualityFlags = string.Join(",", new[] { inconsistent ? "fechas_divergentes" : null, noCase ? "sin_expediente" : null, dup ? "duplicado" : null, i % 7 == 0 ? "sin_responsable" : null }.Where(x => x != null)!),
                RawPayload = "{\"origen\":\"gedess-simulado\"}"
            });
        }
        for (var i = 1; i <= 25; i++)
        {
            legacy.Cases.Add(new LegacyCase
            {
                Id = DemoIds.Of("lc:" + i),
                Number = $"EXP-OLD-{i:000}",
                Title = "Expediente heredado " + i,
                Status = "Abierto",
                Responsible = i % 4 == 0 ? null : "Funcionario heredado"
            });
        }
    }

    private static string SubjectFor(int i) => (i % 10) switch
    {
        0 => "Acción de tutela contra actuación administrativa",
        1 => "Solicitud de admisión a reorganización empresarial",
        2 => "Oficio de juzgado sobre estado de la sociedad",
        3 => "Petición de información pública",
        4 => "Cargue de estados financieros",
        5 => "Consulta sobre contribución especial",
        6 => "Hoja de vida de auxiliar de la justicia",
        7 => "Póliza de cumplimiento contractual",
        8 => "Reforma estatutaria para autorización",
        _ => "Comunicación general de demostración"
    };

    private static string ProcessPageHtml(int i, bool complete, bool fresh)
    {
        var fecha = fresh ? "2026-11-10" : "2024-02-01";
        var aux = complete ? "Promotor: auxiliar demo designado" : "Auxiliar: no publicado";
        return $"<html><body><h1>Proceso INS-2026-{i:0000}</h1><p>Estado: en trámite</p><p>Última actuación: {fecha}</p><p>{aux}</p><p>Demostración local. No es una página pública real.</p></body></html>";
    }

    private static string UserNameOf(string name)
    {
        var parts = name.ToLowerInvariant().Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts[0] + "." + parts[1];
    }

    private static string Slug(string s) => s.ToLowerInvariant()
        .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n")
        .Replace(" ", "-");

    private static readonly (string date, string name)[] Holidays2026 =
    [
        ("2026-01-01", "Año nuevo"), ("2026-01-12", "Reyes"), ("2026-03-23", "San José"),
        ("2026-04-02", "Jueves Santo"), ("2026-04-03", "Viernes Santo"), ("2026-05-01", "Día del Trabajo"),
        ("2026-05-18", "Ascensión"), ("2026-06-08", "Corpus Christi"), ("2026-06-15", "Sagrado Corazón"),
        ("2026-06-29", "San Pedro y San Pablo"), ("2026-07-20", "Independencia"), ("2026-08-07", "Batalla de Boyacá"),
        ("2026-08-17", "Asunción"), ("2026-10-12", "Día de la Raza"), ("2026-11-02", "Todos los Santos"),
        ("2026-11-16", "Independencia de Cartagena"), ("2026-12-08", "Inmaculada"), ("2026-12-25", "Navidad"),
        ("2027-01-01", "Año nuevo 2027")
    ];
}
