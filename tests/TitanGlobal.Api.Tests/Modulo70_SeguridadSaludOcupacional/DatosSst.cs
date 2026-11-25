using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo70_SeguridadSaludOcupacional;

// Datos y utilidades compartidas por las pruebas del módulo 70. Las fechas salen de hoy para no depender del calendario.
internal static class DatosSst
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

    // Empleados 1, 2 y 3 activos; 4 cesado hace 40 días y 5 cesado hace 10 días.
    public static void Restablecer(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.AccionesCorrectivasSst.RemoveRange(contexto.AccionesCorrectivasSst.ToList());
        contexto.IncidentesSst.RemoveRange(contexto.IncidentesSst.ToList());
        contexto.ExamenesMedicos.RemoveRange(contexto.ExamenesMedicos.ToList());
        contexto.EntregasEpp.RemoveRange(contexto.EntregasEpp.ToList());
        contexto.EquiposProteccion.RemoveRange(contexto.EquiposProteccion.ToList());
        contexto.Empleados.RemoveRange(contexto.Empleados.ToList());
        contexto.SaveChanges();
        contexto.Empleados.AddRange(
            CrearEmpleado(1, "Ana", "Torres Vega", "Activo", null),
            CrearEmpleado(2, "Luis", "Rojas Mendoza", "Activo", null),
            CrearEmpleado(3, "Maria", "Salazar Quispe", "Activo", null),
            CrearEmpleado(4, "Carlos", "Diaz Soto", "Cesado", Hoy.AddDays(-40)),
            CrearEmpleado(5, "Rosa", "Quispe Lima", "Cesado", Hoy.AddDays(-10)));
        contexto.SaveChanges();
        BorrarAuditoria(contexto);
    }

    public static async Task<int> InsertarEquipoAsync(OrdenesCompraApiFactory fabrica, string codigo, int vidaUtilDias, bool activo = true)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        EquipoProteccion equipo = new EquipoProteccion
        {
            Codigo = codigo,
            Nombre = "Equipo " + codigo,
            VidaUtilDias = vidaUtilDias,
            Activo = activo
        };
        contexto.EquiposProteccion.Add(equipo);
        await contexto.SaveChangesAsync();
        return equipo.Id;
    }

    public static async Task<int> InsertarEntregaAsync(
        OrdenesCompraApiFactory fabrica,
        int equipoId,
        int empleadoId,
        DateOnly fechaEntrega,
        DateOnly vencimiento)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        EntregaEpp entrega = new EntregaEpp
        {
            EquipoProteccionId = equipoId,
            EmpleadoId = empleadoId,
            FechaEntrega = fechaEntrega,
            Cantidad = 1,
            FechaVencimiento = vencimiento
        };
        contexto.EntregasEpp.Add(entrega);
        await contexto.SaveChangesAsync();
        return entrega.Id;
    }

    public static async Task<int> InsertarExamenAsync(
        OrdenesCompraApiFactory fabrica,
        int empleadoId,
        string tipo,
        DateOnly fecha,
        string resultado,
        DateOnly? vencimiento)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        ExamenMedico examen = new ExamenMedico
        {
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Fecha = fecha,
            Resultado = resultado,
            Restricciones = resultado == "AptoConRestricciones" ? "Sin trabajo en altura" : null,
            FechaVencimiento = vencimiento
        };
        contexto.ExamenesMedicos.Add(examen);
        await contexto.SaveChangesAsync();
        return examen.Id;
    }

    public static async Task<int> InsertarIncidenteAsync(
        OrdenesCompraApiFactory fabrica,
        int empleadoId,
        string tipo,
        DateOnly fecha,
        string estado = "Abierto",
        int diasBaja = 0)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        IncidenteSst incidente = new IncidenteSst
        {
            Numero = "TMP-" + Guid.NewGuid().ToString("N").Substring(0, 16),
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Severidad = "Leve",
            Fecha = fecha,
            Lugar = "Planta",
            Descripcion = "Incidente de prueba",
            DiasBaja = diasBaja,
            Estado = estado,
            FechaCierre = estado == "Cerrado" ? fecha : null
        };
        contexto.IncidentesSst.Add(incidente);
        await contexto.SaveChangesAsync();
        incidente.Numero = "INC-" + incidente.Id.ToString("D6", CultureInfo.InvariantCulture);
        await contexto.SaveChangesAsync();
        return incidente.Id;
    }

    public static async Task<int> InsertarAccionAsync(OrdenesCompraApiFactory fabrica, int incidenteId, bool completada)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        AccionCorrectivaSst accion = new AccionCorrectivaSst
        {
            IncidenteSstId = incidenteId,
            Descripcion = "Señalizar la zona",
            Responsable = "Jefe de planta",
            FechaCompromiso = Hoy.AddDays(5),
            Completada = completada,
            FechaCompletada = completada ? Hoy : null
        };
        contexto.AccionesCorrectivasSst.Add(accion);
        await contexto.SaveChangesAsync();
        return accion.Id;
    }

    public static async Task<List<EquipoProteccion>> LeerEquiposAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.EquiposProteccion.AsNoTracking().OrderBy(equipo => equipo.Id).ToListAsync();
    }

    public static async Task<List<EntregaEpp>> LeerEntregasAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.EntregasEpp.AsNoTracking().OrderBy(entrega => entrega.Id).ToListAsync();
    }

    public static async Task<List<ExamenMedico>> LeerExamenesAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.ExamenesMedicos.AsNoTracking().OrderBy(examen => examen.Id).ToListAsync();
    }

    public static async Task<List<IncidenteSst>> LeerIncidentesAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.IncidentesSst.AsNoTracking().OrderBy(incidente => incidente.Id).ToListAsync();
    }

    public static async Task<List<AccionCorrectivaSst>> LeerAccionesAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.AccionesCorrectivasSst.AsNoTracking().OrderBy(accion => accion.Id).ToListAsync();
    }

    public static async Task<string[]> LeerFirmasAsync(OrdenesCompraApiFactory fabrica)
    {
        List<string> firmas = (await LeerEquiposAsync(fabrica))
            .Select(equipo => $"Q|{equipo.Id}|{equipo.Codigo}|{equipo.Nombre}|{equipo.VidaUtilDias}|{equipo.Activo}")
            .ToList();
        firmas.AddRange((await LeerEntregasAsync(fabrica))
            .Select(entrega => $"E|{entrega.Id}|{entrega.EquipoProteccionId}|{entrega.EmpleadoId}|{entrega.FechaEntrega:yyyy-MM-dd}|{entrega.Cantidad}|{entrega.FechaVencimiento:yyyy-MM-dd}"));
        firmas.AddRange((await LeerExamenesAsync(fabrica))
            .Select(examen => $"X|{examen.Id}|{examen.EmpleadoId}|{examen.Tipo}|{examen.Fecha:yyyy-MM-dd}|{examen.Resultado}|{examen.Restricciones}|{examen.FechaVencimiento:yyyy-MM-dd}"));
        firmas.AddRange((await LeerIncidentesAsync(fabrica))
            .Select(incidente => $"I|{incidente.Id}|{incidente.Numero}|{incidente.EmpleadoId}|{incidente.Tipo}|{incidente.Severidad}|{incidente.Fecha:yyyy-MM-dd}|{incidente.DiasBaja}|{incidente.Estado}|{incidente.FechaCierre:yyyy-MM-dd}"));
        firmas.AddRange((await LeerAccionesAsync(fabrica))
            .Select(accion => $"A|{accion.Id}|{accion.IncidenteSstId}|{accion.Completada}|{accion.FechaCompletada:yyyy-MM-dd}"));
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

    private static Empleado CrearEmpleado(int id, string nombres, string apellidos, string estado, DateOnly? fechaCese)
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
            FechaCese = fechaCese,
            MotivoCese = estado == "Cesado" ? "Renuncia voluntaria" : null
        };
    }

    private static void BorrarAuditoria(TitanGlobalDbContext contexto)
    {
        contexto.RegistrosAuditoria.RemoveRange(
            contexto.RegistrosAuditoria
                .Where(registro => registro.Entidad == "Empleado"
                    || registro.Entidad == "EquipoProteccion"
                    || registro.Entidad == "EntregaEpp"
                    || registro.Entidad == "ExamenMedico"
                    || registro.Entidad == "IncidenteSst"
                    || registro.Entidad == "AccionCorrectivaSst")
                .ToList());
        contexto.SaveChanges();
    }
}
