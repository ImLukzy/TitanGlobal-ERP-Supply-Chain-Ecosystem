using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo65_VacacionesPermisos;

// Datos y utilidades compartidas por las pruebas del módulo 65.
internal static class DatosAusencias
{
    public const string MensajeSinSesion = "Debe iniciar sesión para acceder a este recurso.";

    public const string MensajeSinPermiso = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.";

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

    // Empleados 1 (ingreso 2021-03-01), 2 (2019-08-16), 3 (2024-01-08) activos y 4 cesado.
    public static void Restablecer(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.Ausencias.RemoveRange(contexto.Ausencias.ToList());
        contexto.JornadasAsistencia.RemoveRange(contexto.JornadasAsistencia.ToList());
        contexto.Marcaciones.RemoveRange(contexto.Marcaciones.ToList());
        contexto.AsignacionesTurno.RemoveRange(contexto.AsignacionesTurno.ToList());
        contexto.Turnos.RemoveRange(contexto.Turnos.ToList());
        contexto.AdendasContrato.RemoveRange(contexto.AdendasContrato.ToList());
        contexto.ContratosLaborales.RemoveRange(contexto.ContratosLaborales.ToList());
        contexto.Empleados.RemoveRange(contexto.Empleados.ToList());
        contexto.SaveChanges();
        contexto.Empleados.AddRange(
            CrearEmpleado(1, "Ana", "Torres Vega", new DateOnly(2021, 3, 1), "Activo", null),
            CrearEmpleado(2, "Luis", "Rojas Mendoza", new DateOnly(2019, 8, 16), "Activo", null),
            CrearEmpleado(3, "Maria", "Salazar Quispe", new DateOnly(2024, 1, 8), "Activo", null),
            CrearEmpleado(4, "Carlos", "Diaz Soto", new DateOnly(2018, 5, 2), "Cesado", new DateOnly(2026, 9, 30)));
        contexto.SaveChanges();
        BorrarAuditoria(contexto);
    }

    public static Ausencia CrearAusencia(int empleadoId, string tipo, DateOnly inicio, DateOnly fin, string estado)
    {
        return new Ausencia
        {
            Numero = "AUS-" + Guid.NewGuid().ToString("N").Substring(0, 12),
            EmpleadoId = empleadoId,
            Tipo = tipo,
            FechaInicio = inicio,
            FechaFin = fin,
            Dias = fin.DayNumber - inicio.DayNumber + 1,
            Motivo = tipo == "Vacaciones" ? null : "Motivo de prueba",
            Estado = estado,
            FechaSolicitud = new DateOnly(2024, 1, 2)
        };
    }

    public static async Task<int> InsertarAsync(OrdenesCompraApiFactory fabrica, Ausencia ausencia)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.Ausencias.Add(ausencia);
        await contexto.SaveChangesAsync();
        return ausencia.Id;
    }

    public static async Task<List<Ausencia>> LeerAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Ausencias.AsNoTracking().OrderBy(ausencia => ausencia.Id).ToListAsync();
    }

    public static async Task<string[]> LeerFirmasAsync(OrdenesCompraApiFactory fabrica)
    {
        List<Ausencia> ausencias = await LeerAsync(fabrica);
        return ausencias.Select(ausencia =>
            $"{ausencia.Id}|{ausencia.Numero}|{ausencia.EmpleadoId}|{ausencia.Tipo}|{ausencia.FechaInicio:yyyy-MM-dd}|{ausencia.FechaFin:yyyy-MM-dd}|{ausencia.Estado}|{ausencia.DecididoPor}|{ausencia.MotivoRechazo}|{ausencia.FechaCancelacion:yyyy-MM-dd}")
            .ToArray();
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

    private static void BorrarAuditoria(TitanGlobalDbContext contexto)
    {
        contexto.RegistrosAuditoria.RemoveRange(
            contexto.RegistrosAuditoria
                .Where(registro => registro.Entidad == "Empleado"
                    || registro.Entidad == "Ausencia")
                .ToList());
        contexto.SaveChanges();
    }
}
