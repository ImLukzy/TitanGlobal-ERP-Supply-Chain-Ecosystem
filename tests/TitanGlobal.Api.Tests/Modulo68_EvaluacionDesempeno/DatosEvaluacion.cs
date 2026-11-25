using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo68_EvaluacionDesempeno;

// Datos y utilidades compartidas por las pruebas del módulo 68.
internal static class DatosEvaluacion
{
    public const string MensajeSinSesion = "Debe iniciar sesión para acceder a este recurso.";

    public const string MensajeSinPermiso = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.";

    public static DateOnly Hoy
    {
        get { return DateOnly.FromDateTime(DateTime.UtcNow); }
    }

    public static HttpClient ClienteAnonimo(OrdenesCompraApiFactory fabrica)
    {
        HttpClient cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = null;
        return cliente;
    }

    public static HttpClient ClienteConRoles(OrdenesCompraApiFactory fabrica, params string[] roles)
    {
        HttpClient cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(fabrica.Services, roles));
        return cliente;
    }

    public static async Task AssertErrorAsync(HttpResponseMessage respuesta, int codigo, string mensaje)
    {
        Assert.Equal((HttpStatusCode)codigo, respuesta.StatusCode);
        string contenido = await respuesta.Content.ReadAsStringAsync();
        using JsonDocument documento = JsonDocument.Parse(contenido);
        Assert.Equal(codigo, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }

    // Empleados 1 y 2 activos con contrato indeterminado desde 2020, 3 activo con contrato vencido en 2020,
    // 4 cesado con contrato, 5 activo sin contrato.
    public static void Restablecer(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.CalificacionesEvaluacion.RemoveRange(contexto.CalificacionesEvaluacion.ToList());
        contexto.Evaluaciones.RemoveRange(contexto.Evaluaciones.ToList());
        contexto.CompetenciasEvaluacion.RemoveRange(contexto.CompetenciasEvaluacion.ToList());
        contexto.CiclosEvaluacion.RemoveRange(contexto.CiclosEvaluacion.ToList());
        contexto.AdendasContrato.RemoveRange(contexto.AdendasContrato.ToList());
        contexto.ContratosLaborales.RemoveRange(contexto.ContratosLaborales.ToList());
        contexto.Empleados.RemoveRange(contexto.Empleados.ToList());
        contexto.SaveChanges();
        contexto.Empleados.AddRange(
            CrearEmpleado(1, "Ana", "Torres Vega", "Activo"),
            CrearEmpleado(2, "Luis", "Rojas Mendoza", "Activo"),
            CrearEmpleado(3, "Maria", "Salazar Quispe", "Activo"),
            CrearEmpleado(4, "Carlos", "Diaz Soto", "Cesado"),
            CrearEmpleado(5, "Rosa", "Vera Campos", "Activo"));
        contexto.SaveChanges();
        contexto.ContratosLaborales.AddRange(
            CrearContrato(1, new DateOnly(2020, 1, 1), null, "Indeterminado", "Vigente"),
            CrearContrato(2, new DateOnly(2020, 1, 1), null, "Indeterminado", "Vigente"),
            CrearContrato(3, new DateOnly(2019, 1, 1), new DateOnly(2020, 12, 31), "PlazoFijo", "Vencido"),
            CrearContrato(4, new DateOnly(2020, 1, 1), null, "Indeterminado", "Vigente"));
        contexto.SaveChanges();
        BorrarAuditoria(contexto);
    }

    // Inserta un ciclo semestral cuyo periodo termina 10 días después de hoy, con las competencias de los pesos dados.
    public static async Task<(int CicloId, List<int> CompetenciaIds)> InsertarCicloAsync(
        OrdenesCompraApiFactory fabrica,
        string codigo,
        string estado,
        DateOnly apertura,
        DateOnly cierre,
        params decimal[] pesos)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        DateOnly fin = Hoy.AddDays(10);
        CicloEvaluacion ciclo = new CicloEvaluacion
        {
            Codigo = codigo,
            Nombre = "Ciclo " + codigo,
            Periodo = "Semestral",
            FechaInicioPeriodo = fin.AddDays(-179),
            FechaFinPeriodo = fin,
            FechaApertura = apertura,
            FechaCierre = cierre,
            Estado = estado
        };
        for (int indice = 0; indice < pesos.Length; indice++)
        {
            ciclo.CompetenciasEvaluacion.Add(new CompetenciaEvaluacion
            {
                Nombre = "Competencia " + (indice + 1).ToString(CultureInfo.InvariantCulture),
                Peso = pesos[indice]
            });
        }

        contexto.CiclosEvaluacion.Add(ciclo);
        await contexto.SaveChangesAsync();
        return (ciclo.Id, ciclo.CompetenciasEvaluacion.OrderBy(competencia => competencia.Id).Select(competencia => competencia.Id).ToList());
    }

    public static async Task<(int CicloId, List<int> CompetenciaIds)> InsertarCicloAbiertoAsync(
        OrdenesCompraApiFactory fabrica,
        string codigo,
        params decimal[] pesos)
    {
        return await InsertarCicloAsync(fabrica, codigo, "Abierto", Hoy.AddDays(-5), Hoy.AddDays(20), pesos);
    }

    public static async Task<int> InsertarEvaluacionAsync(
        OrdenesCompraApiFactory fabrica,
        int cicloId,
        int empleadoId,
        int evaluadorId,
        string estado,
        decimal? puntaje)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Evaluacion evaluacion = new Evaluacion
        {
            CicloEvaluacionId = cicloId,
            EmpleadoId = empleadoId,
            EvaluadorId = evaluadorId,
            Estado = estado,
            PuntajeFinal = puntaje,
            FechaEnvio = estado == "Borrador" ? null : DateTime.UtcNow,
            FechaCierre = estado == "Cerrada" ? DateTime.UtcNow : null
        };
        contexto.Evaluaciones.Add(evaluacion);
        await contexto.SaveChangesAsync();
        return evaluacion.Id;
    }

    public static async Task InsertarCalificacionAsync(OrdenesCompraApiFactory fabrica, int evaluacionId, int competenciaId, int calificacion)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.CalificacionesEvaluacion.Add(new CalificacionEvaluacion
        {
            EvaluacionId = evaluacionId,
            CompetenciaEvaluacionId = competenciaId,
            Calificacion = calificacion
        });
        await contexto.SaveChangesAsync();
    }

    public static async Task<List<CicloEvaluacion>> LeerCiclosAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.CiclosEvaluacion.AsNoTracking().OrderBy(ciclo => ciclo.Id).ToListAsync();
    }

    public static async Task<List<Evaluacion>> LeerEvaluacionesAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Evaluaciones.AsNoTracking().OrderBy(evaluacion => evaluacion.Id).ToListAsync();
    }

    public static async Task<List<CompetenciaEvaluacion>> LeerCompetenciasAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.CompetenciasEvaluacion.AsNoTracking().OrderBy(competencia => competencia.Id).ToListAsync();
    }

    public static async Task<List<CalificacionEvaluacion>> LeerCalificacionesAsync(OrdenesCompraApiFactory fabrica, int evaluacionId)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.CalificacionesEvaluacion.AsNoTracking()
            .Where(calificacion => calificacion.EvaluacionId == evaluacionId)
            .OrderBy(calificacion => calificacion.CompetenciaEvaluacionId)
            .ToListAsync();
    }

    public static async Task<string[]> LeerFirmasAsync(OrdenesCompraApiFactory fabrica)
    {
        List<CicloEvaluacion> ciclos = await LeerCiclosAsync(fabrica);
        List<CompetenciaEvaluacion> competencias = await LeerCompetenciasAsync(fabrica);
        List<Evaluacion> evaluaciones = await LeerEvaluacionesAsync(fabrica);
        List<string> firmas = ciclos.Select(ciclo => $"C|{ciclo.Id}|{ciclo.Codigo}|{ciclo.Nombre}|{ciclo.Estado}|{ciclo.FechaApertura:yyyy-MM-dd}").ToList();
        firmas.AddRange(competencias.Select(competencia => $"K|{competencia.Id}|{competencia.CicloEvaluacionId}|{competencia.Nombre}|{competencia.Peso.ToString(CultureInfo.InvariantCulture)}"));
        firmas.AddRange(evaluaciones.Select(evaluacion => $"E|{evaluacion.Id}|{evaluacion.CicloEvaluacionId}|{evaluacion.EmpleadoId}|{evaluacion.Estado}|{evaluacion.PuntajeFinal}|{evaluacion.Comentario}"));
        return firmas.ToArray();
    }

    public static async Task<List<RegistroAuditoria>> LeerAuditoriaAsync(OrdenesCompraApiFactory fabrica, string entidad, int id)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        string idTexto = id.ToString(CultureInfo.InvariantCulture);
        return await contexto.RegistrosAuditoria.AsNoTracking()
            .Where(registro => registro.Entidad == entidad && registro.EntidadId == idTexto)
            .OrderBy(registro => registro.Id)
            .ToListAsync();
    }

    private static Empleado CrearEmpleado(int id, string nombres, string apellidos, string estado)
    {
        return new Empleado
        {
            Id = id,
            Legajo = "EMP-" + id.ToString("D6", CultureInfo.InvariantCulture),
            TipoDocumento = "DNI",
            NumeroDocumento = "2000000" + id.ToString(CultureInfo.InvariantCulture),
            Nombres = nombres,
            Apellidos = apellidos,
            FechaNacimiento = new DateOnly(1990, 1, 1),
            FechaIngreso = new DateOnly(2019, 1, 1),
            Cargo = "Operario",
            Area = "Operaciones",
            Estado = estado,
            FechaCese = estado == "Cesado" ? new DateOnly(2026, 9, 30) : null,
            MotivoCese = estado == "Cesado" ? "Renuncia voluntaria" : null
        };
    }

    private static ContratoLaboral CrearContrato(int empleadoId, DateOnly inicio, DateOnly? fin, string modalidad, string estado)
    {
        return new ContratoLaboral
        {
            Numero = "CTR-" + empleadoId.ToString("D6", CultureInfo.InvariantCulture),
            EmpleadoId = empleadoId,
            Modalidad = modalidad,
            FechaInicio = inicio,
            FechaFin = fin,
            SueldoBasico = 3000m,
            Cargo = "Operario",
            Estado = estado
        };
    }

    private static void BorrarAuditoria(TitanGlobalDbContext contexto)
    {
        contexto.RegistrosAuditoria.RemoveRange(
            contexto.RegistrosAuditoria
                .Where(registro => registro.Entidad == "Empleado"
                    || registro.Entidad == "ContratoLaboral"
                    || registro.Entidad == "CicloEvaluacion"
                    || registro.Entidad == "CompetenciaEvaluacion"
                    || registro.Entidad == "Evaluacion"
                    || registro.Entidad == "CalificacionEvaluacion")
                .ToList());
        contexto.SaveChanges();
    }
}
