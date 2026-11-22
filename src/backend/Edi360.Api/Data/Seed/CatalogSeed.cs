using Edi360.Api.Domain;
using Edi360.Api.Security;

namespace Edi360.Api.Data.Seed;

public static class CatalogSeed
{
    public static readonly (string code, string name, string pillar, string priority, string source, string owner)[] Modules =
    [
        ("E1", "Portal e identidad", "Ecosistema digital", "B", "Documento", "Secretaría General / TIC"),
        ("E2", "Tableros por líder", "Ecosistema digital", "A", "Documento", "Planeación"),
        ("E3", "Integración y eventos", "Ecosistema digital", "B", "Documento", "Dirección TIC"),
        ("E4", "Seguridad y operación", "Ecosistema digital", "B", "Documento", "Seguridad de la información"),
        ("E5", "Plataforma XBRL propia", "Ecosistema digital", "C", "Documento", "Asuntos económicos"),
        ("E6", "Gestión contractual", "Ecosistema digital", "C", "Propuesta pendiente de validación", "Secretaría General"),
        ("E7", "Informes especializados", "Ecosistema digital", "A", "Propuesta pendiente de validación", "Planeación"),
        ("D1", "Sociedades y supervisión", "Gobierno de datos", "A", "Documento", "Supervisión societaria"),
        ("D2", "Registro único de terceros", "Gobierno de datos", "A", "Documento", "Gobierno de datos"),
        ("D3", "Asignación y responsables", "Gobierno de datos", "A", "Documento", "Todas las delegaturas"),
        ("D4", "Parámetros comunes", "Gobierno de datos", "B", "Documento", "Planeación / TIC"),
        ("D5", "Información financiera", "Gobierno de datos", "A", "Documento", "Asuntos económicos"),
        ("D6", "Alertas de riesgo", "Gobierno de datos", "A", "Documento", "Supervisión societaria"),
        ("D7", "Contribución y cartera", "Gobierno de datos", "B", "Documento", "Dirección Financiera"),
        ("D8", "Sanciones", "Gobierno de datos", "B", "Documento", "Delegaturas"),
        ("D9", "Calidad y metadatos", "Gobierno de datos", "B", "Documento", "Planeación"),
        ("D10", "Analítica y datos abiertos", "Gobierno de datos", "C", "Documento", "Estudios económicos"),
        ("D11", "Interoperabilidad", "Gobierno de datos", "B", "Documento", "Dirección TIC"),
        ("J1", "Radicación y reparto", "Justicia digital", "A", "Documento", "Gestión documental"),
        ("J2", "Expediente y agenda procesal", "Justicia digital", "A", "Documento", "Delegaturas"),
        ("J3", "Insolvencia", "Justicia digital", "A", "Documento", "Delegatura de insolvencia"),
        ("J4", "Auxiliares de la justicia", "Justicia digital", "A", "Documento", "Registro de especialistas"),
        ("J5", "Páginas web de los procesos", "Justicia digital", "A", "Documento", "Delegatura de insolvencia"),
        ("J6", "Trámites societarios", "Justicia digital", "C", "Documento", "Asuntos societarios")
    ];

    public static readonly (int row, string data, string entity, string kind, string channel, string modules)[] Sources =
    [
        (1, "Sanciones a contadores y revisores", "Junta Central de Contadores", "Externa", "Convenio simulado", "D8, J4, D1"),
        (2, "Cargos inscritos", "Cámaras de comercio (RUES)", "Externa", "Consulta simulada", "D1, D2, D8"),
        (3, "Datos de la sociedad", "Confecámaras (RUES)", "Externa", "Consulta simulada", "D1, D7"),
        (4, "Registro de comerciantes (SAIR)", "Cámaras de comercio", "Externa (retiro)", "Reemplazo por RUES", "D1"),
        (5, "Identidad de personas naturales", "Registraduría", "Externa", "Consulta simulada", "D2, E1, J4"),
        (6, "RUT, NIT y dígito de verificación", "DIAN", "Externa", "Convenio simulado", "D2, D7, D11"),
        (7, "Antecedentes disciplinarios, fiscales y judiciales", "Procuraduría, Contraloría, Policía", "Externa", "Consulta simulada", "J4, D8, E6"),
        (8, "Departamentos y municipios", "DANE (DIVIPOLA)", "Parámetro", "Tabla local", "D4, J1"),
        (9, "Actividad económica (CIIU)", "DANE", "Parámetro", "Tabla local", "D4, D1"),
        (10, "Festivos", "Gobierno nacional", "Parámetro", "Tabla única", "D4, J1, J2, J3"),
        (11, "Tamaño de empresa por UVT", "Gobierno nacional y DIAN", "Parámetro", "Tabla con vigencia", "D4, D1, D7"),
        (12, "Salario mínimo y UVT", "Gobierno nacional y DIAN", "Parámetro", "Tabla local", "D4"),
        (13, "Juzgados y despachos", "Rama Judicial", "Externa", "Directorio simulado", "J2"),
        (14, "Firma digital", "Proveedor de certificación", "Externa (simulada)", "Validador local", "E4, J2"),
        (15, "Catastro", "IGAC", "Externa (no replicar)", "Consulta puntual simulada", "D9"),
        (16, "Nómina y planta", "KACTUS", "Interna", "Adaptador simulado", "D3, E1"),
        (17, "Duración de la sociedad", "Cámaras de comercio (RUES)", "Externa", "Consulta simulada", "D1"),
        (18, "Socios, accionistas y administradores", "RUES y DIAN", "Externa", "Consulta simulada", "D2, D5, D6"),
        (19, "Beneficiario final", "DIAN (RUB)", "Externa", "Convenio simulado", "D2, D6"),
        (20, "Pólizas y garantías", "Aseguradoras / Fasecolda", "Externa", "Verificador simulado", "J4, E6"),
        (21, "Títulos profesionales", "IES y Consejos profesionales", "Externa", "Consulta simulada", "J4, E6"),
        (22, "Tarjeta o matrícula profesional", "COPNIA, JCC, CSJ", "Externa", "Consulta simulada", "J4, E6"),
        (23, "Contratos de la entidad en SECOP II", "Colombia Compra Eficiente", "Externa", "Datos abiertos simulados", "E6")
    ];

    public static readonly (int n, string type, string area, string audience)[] Reports =
    [
        (1, "Tutelas: vencimientos y contestación", "Oficina Jurídica", "El juez y Secretaría General"),
        (2, "PQRSD y acceso a la información", "Control Interno", "Superintendente"),
        (3, "Requerimientos del Congreso y entes de control", "Secretaría General", "El requirente"),
        (4, "Supervisión de contratos", "Contratación", "Para cada pago"),
        (5, "Oficios sobre el estado de una sociedad", "Gestión documental", "Juzgado o entidad"),
        (6, "Supervisión normativa de circulares", "Delegatura emisora", "Alta dirección"),
        (7, "Cartera y contribución", "Dirección Financiera", "Secretaría General"),
        (8, "Gestión para la alta dirección", "Planeación", "Comité institucional"),
        (9, "Insumos CGR (SIRECI) y FURAG", "Control Interno y Planeación", "CGR y Función Pública")
    ];

    public static IEnumerable<PermissionDef> Permissions()
    {
        string[] codes =
        [
            "E1.LOGIN","E1.PERFIL","E2.VER","E2.FILTRAR","E3.VER","E4.VER","E4.AUDITAR","E4.INCIDENTES",
            "E5.CARGAR","E5.APROBAR","E6.VER","E6.CONTRATO","E6.PAGO","E7.VER","E7.BORRADOR","E7.APROBAR",
            "D1.VER","D1.INSTITUCIONAL","D1.EDITAR","D2.VER","D2.CREAR","D2.FUSIONAR","D3.VER","D3.ASIGNAR",
            "D4.VER","D4.EDITAR","D5.VER","D5.VALIDAR","D5.EDITAR","D6.VER","D6.REVISAR","D7.VER","D7.LIQUIDAR","D7.APROBAR_PAGO",
            "D8.VER","D8.REGISTRAR","D8.CAMBIAR_FIRME","D9.VER","D9.CORREGIR","D10.VER","D11.VER","D11.PROBAR",
            "J1.VER","J1.RADICAR","J1.REPARTIR","J1.RESPONDER","J1.ANULAR",
            "J2.VER","J2.ACTUAR","J2.PROVIDENCIA","J2.RESERVADO","J2.INSTITUCIONAL","J2.DECISION",
            "J3.VER","J3.TRAMITAR","J3.ACUERDO","J3.CERRAR",
            "J4.VER","J4.GESTIONAR","J4.DESIGNAR",
            "J5.VER","J5.REVISAR",
            "J6.VER","J6.TRAMITAR","J6.APROBAR",
            "GD.MIGRAR","GD.CONCILIAR","GD.TRASLADAR",
            "DEMO.CONTROL"
        ];
        return codes.Select(c =>
        {
            var parts = c.Split('.');
            return new PermissionDef
            {
                Id = DemoIds.Of("perm:" + c),
                Code = c,
                Module = parts[0],
                Action = parts[1],
                Scope = c.Contains("INSTITUCIONAL") ? Scopes.Institutional : Scopes.Assigned,
                Classification = c.Contains("RESERVADO") ? "Reservado" : "Ordinario",
                RequiresApproval = c is "J2.DECISION" or "J2.PROVIDENCIA" or "E7.APROBAR" or "E6.PAGO"
            };
        });
    }

    public static readonly Dictionary<string, string[]> RolePerms = new()
    {
        [Profiles.Superintendente] = ["E1.LOGIN","E1.PERFIL","E2.VER","E2.FILTRAR","E7.VER","D1.VER","D1.INSTITUCIONAL","D6.VER","D7.VER","D8.VER","D10.VER","J2.VER","J2.INSTITUCIONAL","J3.VER","E3.VER","E4.VER"],
        [Profiles.SecretarioGeneral] = ["E1.LOGIN","E1.PERFIL","E2.VER","E6.VER","E6.CONTRATO","E7.VER","E7.APROBAR","J1.VER","D7.VER","D4.VER","E4.VER"],
        [Profiles.DirectorAdministrativo] = ["E1.LOGIN","E1.PERFIL","E2.VER","J1.VER","J1.REPARTIR","E6.VER","E6.CONTRATO","D3.VER","D3.ASIGNAR"],
        [Profiles.CoordDocumental] = ["E1.LOGIN","E1.PERFIL","J1.VER","J1.RADICAR","J1.REPARTIR","J1.ANULAR","D2.VER","D3.VER","D3.ASIGNAR","D4.VER","J2.VER"],
        [Profiles.Radicador] = ["E1.LOGIN","E1.PERFIL","J1.VER","J1.RADICAR","D2.VER"],
        [Profiles.JefeJuridica] = ["E1.LOGIN","E1.PERFIL","E2.VER","J1.VER","J1.REPARTIR","J2.VER","J2.ACTUAR","E7.VER","E7.BORRADOR","E7.APROBAR","D3.ASIGNAR"],
        [Profiles.Abogado] = ["E1.LOGIN","E1.PERFIL","J1.VER","J1.RESPONDER","J2.VER","J2.ACTUAR","E7.BORRADOR"],
        [Profiles.DelegadoInsolvencia] = ["E1.LOGIN","E1.PERFIL","E2.VER","E2.FILTRAR","J2.VER","J2.INSTITUCIONAL","J3.VER","J3.TRAMITAR","J3.ACUERDO","J3.CERRAR","J4.VER","J5.VER","D1.VER","D6.VER"],
        [Profiles.Sustanciador] = ["E1.LOGIN","E1.PERFIL","J2.VER","J2.ACTUAR","J3.VER","J3.TRAMITAR"],
        [Profiles.Jurisdiccional] = ["E1.LOGIN","E1.PERFIL","J2.VER","J2.ACTUAR","J2.PROVIDENCIA","J2.DECISION","J3.VER","J3.TRAMITAR","J3.ACUERDO","J3.CERRAR"],
        [Profiles.Acuerdos] = ["E1.LOGIN","E1.PERFIL","J3.VER","J3.TRAMITAR","D6.VER","D1.VER"],
        [Profiles.Supervisor] = ["E1.LOGIN","E1.PERFIL","D1.VER","D5.VER","D6.VER","D6.REVISAR","D8.VER","J2.VER","E2.VER"],
        [Profiles.Analista] = ["E1.LOGIN","E1.PERFIL","D5.VER","D5.VALIDAR","D10.VER","E5.CARGAR","D6.VER","D1.VER"],
        [Profiles.Especialistas] = ["E1.LOGIN","E1.PERFIL","J4.VER","J4.GESTIONAR","J4.DESIGNAR","D2.VER","D8.VER","D11.VER"],
        [Profiles.Intendente] = ["E1.LOGIN","E1.PERFIL","E2.VER","D1.VER","D6.VER","J1.VER","J2.VER"],
        [Profiles.FinancieraConsulta] = ["E1.LOGIN","E1.PERFIL","D7.VER","D1.VER"],
        [Profiles.FinancieraLiquidacion] = ["E1.LOGIN","E1.PERFIL","D7.VER","D7.LIQUIDAR","D1.VER"],
        [Profiles.FinancieraAprobacion] = ["E1.LOGIN","E1.PERFIL","D7.VER","D7.APROBAR_PAGO","E6.PAGO"],
        [Profiles.ControlInterno] = ["E1.LOGIN","E1.PERFIL","E4.VER","E4.AUDITAR","D9.VER","E7.VER","E6.VER","J1.VER","J2.VER"],
        [Profiles.ControlDisciplinario] = ["E1.LOGIN","E1.PERFIL","J2.VER","J2.ACTUAR","J2.RESERVADO","E4.AUDITAR"],
        [Profiles.Planeacion] = ["E1.LOGIN","E1.PERFIL","E2.VER","E2.FILTRAR","E7.VER","E7.BORRADOR","D4.VER","D4.EDITAR","D9.VER","D10.VER"],
        [Profiles.DireccionTic] = ["E1.LOGIN","E1.PERFIL","E3.VER","E4.VER","E4.INCIDENTES","D11.VER","D11.PROBAR","D9.VER","GD.MIGRAR"],
        [Profiles.DemoAdmin] =
        [
            "E1.LOGIN","E1.PERFIL","E2.VER","E2.FILTRAR","E3.VER","E4.VER","E4.AUDITAR","E4.INCIDENTES",
            "E5.CARGAR","E5.APROBAR","E6.VER","E6.CONTRATO","E7.VER","E7.BORRADOR",
            "D1.VER","D1.INSTITUCIONAL","D2.VER","D3.VER","D3.ASIGNAR","D4.VER","D5.VER","D6.VER","D7.VER","D8.VER","D9.VER","D10.VER","D11.VER","D11.PROBAR",
            "J1.VER","J1.RADICAR","J1.REPARTIR","J2.VER","J2.INSTITUCIONAL","J3.VER","J3.ACUERDO","J4.VER","J4.GESTIONAR","J4.DESIGNAR","J5.VER","J6.VER","J6.APROBAR",
            "GD.MIGRAR","GD.CONCILIAR","GD.TRASLADAR","DEMO.CONTROL"
        ]
    };
}
