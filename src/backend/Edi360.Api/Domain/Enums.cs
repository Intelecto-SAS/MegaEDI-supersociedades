namespace Edi360.Api.Domain;

public static class Profiles
{
    public const string Superintendente = "SUPERINTENDENTE";
    public const string SecretarioGeneral = "SECRETARIO_GENERAL";
    public const string DirectorAdministrativo = "DIRECTOR_ADMINISTRATIVO";
    public const string CoordDocumental = "COORD_DOCUMENTAL";
    public const string Radicador = "RADICADOR";
    public const string JefeJuridica = "JEFE_JURIDICA";
    public const string Abogado = "ABOGADO";
    public const string DelegadoInsolvencia = "DELEGADO_INSOLVENCIA";
    public const string Sustanciador = "SUSTANCIADOR";
    public const string Jurisdiccional = "JURISDICCIONAL";
    public const string Acuerdos = "ACUERDOS";
    public const string Supervisor = "SUPERVISOR";
    public const string Analista = "ANALISTA";
    public const string Especialistas = "ESPECIALISTAS";
    public const string Intendente = "INTENDENTE";
    public const string FinancieraConsulta = "FINANCIERA_CONSULTA";
    public const string FinancieraLiquidacion = "FINANCIERA_LIQUIDACION";
    public const string FinancieraAprobacion = "FINANCIERA_APROBACION";
    public const string ControlInterno = "CONTROL_INTERNO";
    public const string ControlDisciplinario = "CONTROL_DISCIPLINARIO";
    public const string Planeacion = "PLANEACION";
    public const string DireccionTic = "DIRECCION_TIC";
    public const string DemoAdmin = "DEMO_ADMIN";
}

public static class HomeExperiences
{
    public const string Operativo = "operativo";
    public const string Coordinador = "coordinador";
    public const string Delegado = "delegado";
    public const string AltaDireccion = "alta-direccion";
}

public static class Scopes
{
    public const string Own = "OWN";
    public const string Assigned = "ASSIGNED";
    public const string Team = "TEAM";
    public const string Department = "DEPARTMENT";
    public const string Regional = "REGIONAL";
    public const string Institutional = "INSTITUTIONAL";
    public const string SpecificCase = "SPECIFIC_CASE";
}

public static class EventTypes
{
    public const string RadicadoCreado = "RadicadoCreado";
    public const string RadicadoAsignado = "RadicadoAsignado";
    public const string RespuestaAprobada = "RespuestaAprobada";
    public const string ExpedienteActualizado = "ExpedienteActualizado";
    public const string ProvidenciaRegistrada = "ProvidenciaRegistrada";
    public const string AcuerdoConfirmado = "AcuerdoConfirmado";
    public const string AcuerdoIncumplido = "AcuerdoIncumplido";
    public const string ProcesoCerrado = "ProcesoCerrado";
    public const string SociedadActualizada = "SociedadActualizada";
    public const string TerceroFusionado = "TerceroFusionado";
    public const string ReporteFinancieroAprobado = "ReporteFinancieroAprobado";
    public const string AlertaGenerada = "AlertaGenerada";
    public const string ValidacionCompletada = "ValidacionCompletada";
    public const string PolizaVerificada = "PolizaVerificada";
    public const string ContratoIniciado = "ContratoIniciado";
    public const string InformeAprobado = "InformeAprobado";
    public const string LoteMigrado = "LoteMigrado";
    public const string CapacidadTransferida = "CapacidadTransferida";
}

public static class MigrationStates
{
    public const string NoIniciado = "No iniciado";
    public const string Inventariado = "Inventariado";
    public const string Preparado = "Preparado";
    public const string MigracionEnPrueba = "Migración en prueba";
    public const string PendienteConciliacion = "Pendiente de conciliación";
    public const string Conciliado = "Conciliado";
    public const string Paralelo = "Operación en paralelo controlada";
    public const string TrasladoAutorizado = "Traslado de responsabilidad autorizado";
    public const string ConsultaHistorica = "Consulta histórica en heredado";
    public const string Cerrado = "Cerrado para nuevas operaciones";
}

public static class Capabilities
{
    public static readonly string[] All =
    [
        "Recepcion", "Radicacion", "Reparto", "Expedientes", "Documentos",
        "ProduccionDocumental", "Notificaciones", "Consultas", "Historicos"
    ];
}
