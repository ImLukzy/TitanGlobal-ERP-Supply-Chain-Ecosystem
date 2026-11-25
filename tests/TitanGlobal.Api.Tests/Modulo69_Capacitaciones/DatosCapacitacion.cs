using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo69_Capacitaciones;

// Datos y utilidades compartidas por las pruebas del módulo 69.
internal static class DatosCapacitacion
{
    public const string MensajeSinSesion = "Debe iniciar sesión para acceder a este recurso.";

    public const string MensajeSinPermiso = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.";

    public static DateOnly Hoy
    {
        get { return DateOnly.FromDateTime(DateTime.UtcNow); }
    }

    public static string Fecha(DateOnly fecha)
    {
        return fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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

    // Empleados 1, 2 y 3 activos y 4 cesado.
    public static void Restablecer(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.CertificacionesCurso.RemoveRange(contexto.CertificacionesCurso.ToList());
        contexto.InscripcionesCurso.RemoveRange(contexto.InscripcionesCurso.ToList());
        contexto.ProgramacionesCurso.RemoveRange(contexto.ProgramacionesCurso.ToList());
        contexto.Cursos.RemoveRange(contexto.Cursos.ToList());
        contexto.Empleados.RemoveRange(contexto.Empleados.ToList());
        contexto.SaveChanges();
        contexto.Empleados.AddRange(
            CrearEmpleado(1, "Ana", "Torres Vega", "Activo"),
            CrearEmpleado(2, "Luis", "Rojas Mendoza", "Activo"),
            CrearEmpleado(3, "Maria", "Salazar Quispe", "Activo"),
            CrearEmpleado(4, "Carlos", "Diaz Soto", "Cesado"));
        contexto.SaveChanges();
        BorrarAuditoria(contexto);
    }

    public static async Task<int> InsertarCursoAsync(OrdenesCompraApiFactory fabrica, string codigo, decimal horas, bool activo = true)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Curso curso = new Curso
        {
            Codigo = codigo,
            Nombre = "Curso " + codigo,
            Horas = horas,
            Tipo = "Interno",
            Activo = activo
        };
        contexto.Cursos.Add(curso);
        await contexto.SaveChangesAsync();
        return curso.Id;
    }

    public static async Task<int> InsertarProgramacionAsync(
        OrdenesCompraApiFactory fabrica,
        int cursoId,
        DateOnly inicio,
        DateOnly fin,
        int cupo,
        string estado)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        ProgramacionCurso programacion = new ProgramacionCurso
        {
            CursoId = cursoId,
            Anio = inicio.Year,
            FechaInicio = inicio,
            FechaFin = fin,
            Cupo = cupo,
            Estado = estado
        };
        contexto.ProgramacionesCurso.Add(programacion);
        await contexto.SaveChangesAsync();
        return programacion.Id;
    }

    public static async Task<int> InsertarInscripcionAsync(
        OrdenesCompraApiFactory fabrica,
        int programacionId,
        int empleadoId,
        string estado,
        decimal? nota = null)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        InscripcionCurso inscripcion = new InscripcionCurso
        {
            ProgramacionCursoId = programacionId,
            EmpleadoId = empleadoId,
            Estado = estado,
            Nota = nota,
            FechaInscripcion = Hoy.AddDays(-30)
        };
        contexto.InscripcionesCurso.Add(inscripcion);
        await contexto.SaveChangesAsync();
        return inscripcion.Id;
    }

    public static async Task<int> InsertarCertificacionAsync(
        OrdenesCompraApiFactory fabrica,
        int inscripcionId,
        string codigo,
        DateOnly emision,
        DateOnly? vencimiento)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        CertificacionCurso certificacion = new CertificacionCurso
        {
            InscripcionCursoId = inscripcionId,
            Codigo = codigo,
            FechaEmision = emision,
            FechaVencimiento = vencimiento
        };
        contexto.CertificacionesCurso.Add(certificacion);
        await contexto.SaveChangesAsync();
        return certificacion.Id;
    }

    public static async Task<List<Curso>> LeerCursosAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Cursos.AsNoTracking().OrderBy(curso => curso.Id).ToListAsync();
    }

    public static async Task<List<ProgramacionCurso>> LeerProgramacionesAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.ProgramacionesCurso.AsNoTracking().OrderBy(programacion => programacion.Id).ToListAsync();
    }

    public static async Task<List<InscripcionCurso>> LeerInscripcionesAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.InscripcionesCurso.AsNoTracking().OrderBy(inscripcion => inscripcion.Id).ToListAsync();
    }

    public static async Task<List<CertificacionCurso>> LeerCertificacionesAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.CertificacionesCurso.AsNoTracking().OrderBy(certificacion => certificacion.Id).ToListAsync();
    }

    public static async Task<string[]> LeerFirmasAsync(OrdenesCompraApiFactory fabrica)
    {
        List<Curso> cursos = await LeerCursosAsync(fabrica);
        List<ProgramacionCurso> programaciones = await LeerProgramacionesAsync(fabrica);
        List<InscripcionCurso> inscripciones = await LeerInscripcionesAsync(fabrica);
        List<CertificacionCurso> certificaciones = await LeerCertificacionesAsync(fabrica);
        List<string> firmas = cursos
            .Select(curso => $"C|{curso.Id}|{curso.Codigo}|{curso.Nombre}|{curso.Horas.ToString(CultureInfo.InvariantCulture)}|{curso.Tipo}|{curso.Activo}")
            .ToList();
        firmas.AddRange(programaciones.Select(programacion => $"P|{programacion.Id}|{programacion.CursoId}|{programacion.FechaInicio:yyyy-MM-dd}|{programacion.FechaFin:yyyy-MM-dd}|{programacion.Cupo}|{programacion.Estado}"));
        firmas.AddRange(inscripciones.Select(inscripcion => $"I|{inscripcion.Id}|{inscripcion.ProgramacionCursoId}|{inscripcion.EmpleadoId}|{inscripcion.Estado}|{inscripcion.Nota}"));
        firmas.AddRange(certificaciones.Select(certificacion => $"X|{certificacion.Id}|{certificacion.InscripcionCursoId}|{certificacion.Codigo}|{certificacion.FechaEmision:yyyy-MM-dd}|{certificacion.FechaVencimiento:yyyy-MM-dd}"));
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

    private static void BorrarAuditoria(TitanGlobalDbContext contexto)
    {
        contexto.RegistrosAuditoria.RemoveRange(
            contexto.RegistrosAuditoria
                .Where(registro => registro.Entidad == "Empleado"
                    || registro.Entidad == "Curso"
                    || registro.Entidad == "ProgramacionCurso"
                    || registro.Entidad == "InscripcionCurso"
                    || registro.Entidad == "CertificacionCurso")
                .ToList());
        contexto.SaveChanges();
    }
}
