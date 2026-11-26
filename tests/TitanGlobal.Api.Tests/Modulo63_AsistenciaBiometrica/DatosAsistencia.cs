using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo63_AsistenciaBiometrica;

// Datos y utilidades compartidas por las pruebas del módulo 63.
internal static class DatosAsistencia
{
    public const string MensajeSinSesion = "Debe iniciar sesión para acceder a este recurso.";

    public const string MensajeSinPermiso = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.";

    public static DateTime Utc(int anio, int mes, int dia, int hora, int minuto, int segundo = 0)
    {
        return new DateTime(anio, mes, dia, hora, minuto, segundo, DateTimeKind.Utc);
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

    public static void Restablecer(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.JornadasAsistencia.RemoveRange(contexto.JornadasAsistencia.ToList());
        contexto.Marcaciones.RemoveRange(contexto.Marcaciones.ToList());
        contexto.AsignacionesTurno.RemoveRange(contexto.AsignacionesTurno.ToList());
        contexto.Turnos.RemoveRange(contexto.Turnos.ToList());
        contexto.AdendasContrato.RemoveRange(contexto.AdendasContrato.ToList());
        contexto.ContratosLaborales.RemoveRange(contexto.ContratosLaborales.ToList());
        contexto.Empleados.RemoveRange(contexto.Empleados.ToList());
        contexto.SaveChanges();
        BorrarAuditoria(contexto);
        contexto.Empleados.AddRange(
            CrearEmpleado(1, "Ana", "Torres Vega", new DateOnly(2021, 3, 1), "Activo", null),
            CrearEmpleado(2, "Luis", "Rojas Mendoza", new DateOnly(2019, 8, 16), "Activo", null),
            CrearEmpleado(3, "Maria", "Salazar Quispe", new DateOnly(2024, 1, 8), "Activo", null),
            CrearEmpleado(4, "Carlos", "Diaz Soto", new DateOnly(2018, 5, 2), "Cesado", new DateOnly(2026, 9, 30)));
        contexto.SaveChanges();
        contexto.ContratosLaborales.AddRange(
            CrearContrato(1, 1, new DateOnly(2021, 3, 1), null, "Vigente", null),
            CrearContrato(2, 2, new DateOnly(2019, 8, 16), null, "Vigente", null),
            CrearContrato(3, 4, new DateOnly(2018, 5, 2), new DateOnly(2027, 5, 1), "Cerrado", new DateOnly(2026, 9, 30)));
        contexto.SaveChanges();
        BorrarAuditoria(contexto);
    }

    public static Turno CrearTurnoDia(string codigo)
    {
        return new Turno
        {
            Codigo = codigo,
            Nombre = "Turno " + codigo,
            Tipo = "Fijo",
            HoraEntrada = new TimeOnly(8, 0),
            HoraSalida = new TimeOnly(17, 0),
            ToleranciaMinutos = 10,
            DiasLaborables = "1,2,3,4,5",
            Activo = true
        };
    }

    public static Turno CrearTurnoNoche(string codigo)
    {
        return new Turno
        {
            Codigo = codigo,
            Nombre = "Turno " + codigo,
            Tipo = "Fijo",
            HoraEntrada = new TimeOnly(22, 0),
            HoraSalida = new TimeOnly(6, 0),
            CruzaMedianoche = true,
            ToleranciaMinutos = 5,
            DiasLaborables = "1,2,3,4,5,6",
            Activo = true
        };
    }

    public static Turno CrearTurnoRotativo(string codigo)
    {
        return new Turno
        {
            Codigo = codigo,
            Nombre = "Turno " + codigo,
            Tipo = "Rotativo",
            HoraEntrada = new TimeOnly(7, 0),
            HoraSalida = new TimeOnly(19, 0),
            ToleranciaMinutos = 15,
            DiasTrabajo = 4,
            DiasDescanso = 2,
            FechaInicioCiclo = new DateOnly(2026, 1, 5),
            Activo = true
        };
    }

    public static Marcacion CrearMarcacion(int empleadoId, string tipo, DateTime instante)
    {
        return new Marcacion
        {
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Instante = instante,
            Origen = "Manual",
            Observacion = "Prueba"
        };
    }

    public static async Task<int> InsertarTurnoAsync(OrdenesCompraApiFactory fabrica, Turno turno)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.Turnos.Add(turno);
        await contexto.SaveChangesAsync();
        return turno.Id;
    }

    public static async Task AsignarTurnoAsync(OrdenesCompraApiFactory fabrica, int empleadoId, int turnoId, DateOnly inicio)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.AsignacionesTurno.Add(new AsignacionTurno { EmpleadoId = empleadoId, TurnoId = turnoId, FechaInicio = inicio });
        await contexto.SaveChangesAsync();
    }

    public static async Task<int> InsertarMarcacionAsync(OrdenesCompraApiFactory fabrica, Marcacion marcacion)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.Marcaciones.Add(marcacion);
        await contexto.SaveChangesAsync();
        return marcacion.Id;
    }

    public static async Task<int> InsertarJornadaAsync(OrdenesCompraApiFactory fabrica, JornadaAsistencia jornada)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.JornadasAsistencia.Add(jornada);
        await contexto.SaveChangesAsync();
        return jornada.Id;
    }

    public static JornadaAsistencia CrearJornadaDia(int empleadoId, DateOnly fecha, int minutosExtra, string estadoHorasExtra)
    {
        return new JornadaAsistencia
        {
            EmpleadoId = empleadoId,
            Fecha = fecha,
            HoraEntradaProgramada = new TimeOnly(8, 0),
            HoraSalidaProgramada = new TimeOnly(17, 0),
            ToleranciaMinutos = 10,
            MinutosExtra = minutosExtra,
            Estado = "Asistencia",
            EstadoHorasExtra = estadoHorasExtra
        };
    }

    public static async Task<List<Marcacion>> LeerMarcacionesAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Marcaciones.AsNoTracking().OrderBy(marcacion => marcacion.Id).ToListAsync();
    }

    public static async Task<List<JornadaAsistencia>> LeerJornadasAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.JornadasAsistencia.AsNoTracking().OrderBy(jornada => jornada.Id).ToListAsync();
    }

    public static async Task<string[]> LeerFirmasAsync(OrdenesCompraApiFactory fabrica)
    {
        List<Marcacion> marcaciones = await LeerMarcacionesAsync(fabrica);
        List<JornadaAsistencia> jornadas = await LeerJornadasAsync(fabrica);
        List<string> firmas = marcaciones.Select(marcacion =>
            $"M|{marcacion.Id}|{marcacion.EmpleadoId}|{marcacion.Tipo}|{marcacion.Instante:O}|{marcacion.Origen}|{marcacion.NumeroRegistro}").ToList();
        firmas.AddRange(jornadas.Select(jornada =>
            $"J|{jornada.Id}|{jornada.EmpleadoId}|{jornada.Fecha:yyyy-MM-dd}|{jornada.Estado}|{jornada.MinutosExtra}|{jornada.EstadoHorasExtra}|{jornada.DecididoPor}|{jornada.MotivoRechazo}"));
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

    private static Empleado CrearEmpleado(int id, string nombres, string apellidos, DateOnly ingreso, string estado, DateOnly? cese)
    {
        return new Empleado
        {
            Id = id,
            Legajo = "EMP-" + id.ToString("D6", CultureInfo.InvariantCulture),
            TipoDocumento = "DNI",
            NumeroDocumento = "1000000" + id.ToString(CultureInfo.InvariantCulture),
            Nombres = nombres,
            Apellidos = apellidos,
            FechaNacimiento = new DateOnly(1990, 1, 1),
            FechaIngreso = ingreso,
            Cargo = "Operario",
            Area = "Operaciones",
            Estado = estado,
            FechaCese = cese,
            MotivoCese = cese is null ? null : "Renuncia voluntaria"
        };
    }

    private static ContratoLaboral CrearContrato(int id, int empleadoId, DateOnly inicio, DateOnly? fin, string estado, DateOnly? cierre)
    {
        return new ContratoLaboral
        {
            Id = id,
            Numero = "CTR-" + id.ToString("D6", CultureInfo.InvariantCulture),
            EmpleadoId = empleadoId,
            Modalidad = fin is null ? "Indeterminado" : "PlazoFijo",
            FechaInicio = inicio,
            FechaFin = fin,
            SueldoBasico = 2000m,
            Cargo = "Operario",
            Estado = estado,
            FechaCierre = cierre,
            MotivoCierre = cierre is null ? null : "Cese del empleado"
        };
    }

    private static void BorrarAuditoria(TitanGlobalDbContext contexto)
    {
        contexto.RegistrosAuditoria.RemoveRange(
            contexto.RegistrosAuditoria
                .Where(registro => registro.Entidad == "Empleado"
                    || registro.Entidad == "ContratoLaboral"
                    || registro.Entidad == "Turno"
                    || registro.Entidad == "AsignacionTurno"
                    || registro.Entidad == "Marcacion"
                    || registro.Entidad == "JornadaAsistencia")
                .ToList());
        contexto.SaveChanges();
    }
}
