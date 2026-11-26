using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo67_AdelantosPrestamos;

// Datos y utilidades compartidas por las pruebas del módulo 67.
internal static class DatosPrestamos
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
        Assert.Equal((System.Net.HttpStatusCode)codigo, respuesta.StatusCode);
        string contenido = await respuesta.Content.ReadAsStringAsync();
        using JsonDocument documento = JsonDocument.Parse(contenido);
        Assert.Equal(codigo, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }

    // Empleados 1 (contrato indeterminado de 8500 con adenda de 9500 desde 2024-01-01), 2 (contrato indeterminado de 4200),
    // 3 (contrato formativo de 1500 vencido el 2024-12-31) y 4 (cesado, con contrato cerrado).
    public static void Restablecer(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.DescuentosCuotaPrestamo.RemoveRange(contexto.DescuentosCuotaPrestamo.ToList());
        contexto.CuotasPrestamo.RemoveRange(contexto.CuotasPrestamo.ToList());
        contexto.Prestamos.RemoveRange(contexto.Prestamos.ToList());
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
        contexto.ContratosLaborales.AddRange(
            CrearContrato(1, 1, "Indeterminado", new DateOnly(2021, 3, 1), null, 8500m, "Vigente", null),
            CrearContrato(2, 2, "Indeterminado", new DateOnly(2021, 8, 16), null, 4200m, "Vigente", null),
            CrearContrato(3, 3, "Formativo", new DateOnly(2024, 1, 8), new DateOnly(2024, 12, 31), 1500m, "Vigente", null),
            CrearContrato(4, 4, "PlazoFijo", new DateOnly(2024, 5, 2), new DateOnly(2027, 5, 1), 2600m, "Cerrado", new DateOnly(2026, 9, 30)));
        contexto.SaveChanges();
        contexto.AdendasContrato.Add(new AdendaContrato
        {
            ContratoLaboralId = 1,
            FechaVigencia = new DateOnly(2024, 1, 1),
            Cargo = "Gerente Comercial",
            SueldoBasico = 9500m
        });
        contexto.SaveChanges();
        BorrarAuditoria(contexto);
    }

    public static async Task InsertarContratoAsync(OrdenesCompraApiFactory fabrica, ContratoLaboral contrato)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.ContratosLaborales.Add(contrato);
        await contexto.SaveChangesAsync();
    }

    public static ContratoLaboral CrearContrato(int id, int empleadoId, string modalidad, DateOnly inicio, DateOnly? fin, decimal sueldo, string estado, DateOnly? cierre)
    {
        return new ContratoLaboral
        {
            Id = id,
            Numero = "CTR-" + id.ToString("D6", CultureInfo.InvariantCulture),
            EmpleadoId = empleadoId,
            Modalidad = modalidad,
            FechaInicio = inicio,
            FechaFin = fin,
            SueldoBasico = sueldo,
            Cargo = "Operario",
            Estado = estado,
            FechaCierre = cierre,
            MotivoCierre = cierre is null ? null : "Cierre de prueba"
        };
    }

    public static async Task<HttpResponseMessage> RegistrarAsync(
        HttpClient cliente,
        int? empleadoId,
        string? tipo,
        decimal? monto,
        int? numeroCuotas,
        DateOnly? otorgamiento,
        int? primerAnio,
        int? primerMes,
        string? motivo)
    {
        RegistrarPrestamoRequestDto solicitud = new RegistrarPrestamoRequestDto
        {
            EmpleadoId = empleadoId,
            Tipo = tipo,
            Monto = monto,
            NumeroCuotas = numeroCuotas,
            FechaOtorgamiento = otorgamiento,
            PrimerAnio = primerAnio,
            PrimerMes = primerMes,
            Motivo = motivo
        };
        return await cliente.PostAsJsonAsync("/api/v1/prestamos", solicitud);
    }

    // Registra un préstamo válido por la API (otorgado el 2030-01-10) y devuelve su id.
    public static async Task<int> CrearPrestamoAsync(HttpClient cliente, int empleadoId, string tipo, decimal monto, int cuotas, int primerAnio, int primerMes)
    {
        HttpResponseMessage respuesta = await RegistrarAsync(cliente, empleadoId, tipo, monto, cuotas, new DateOnly(2030, 1, 10), primerAnio, primerMes, "Motivo de prueba");
        Assert.Equal(System.Net.HttpStatusCode.OK, respuesta.StatusCode);
        PrestamoCreadoDto? creado = await respuesta.Content.ReadFromJsonAsync<PrestamoCreadoDto>();
        Assert.NotNull(creado);
        return creado.Id;
    }

    public static async Task<List<Prestamo>> LeerAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Prestamos.AsNoTracking().OrderBy(prestamo => prestamo.Id).ToListAsync();
    }

    public static async Task<List<CuotaPrestamo>> LeerCuotasAsync(OrdenesCompraApiFactory fabrica, int prestamoId)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.CuotasPrestamo.AsNoTracking()
            .Where(cuota => cuota.PrestamoId == prestamoId)
            .OrderBy(cuota => cuota.NumeroCuota)
            .ToListAsync();
    }

    // Descuentos de cada cuota del préstamo, en orden de cuota, con el formato "planilla:monto" separados por coma.
    public static async Task<string[]> LeerDescuentosPorCuotaAsync(OrdenesCompraApiFactory fabrica, int prestamoId)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        List<CuotaPrestamo> cuotas = await contexto.CuotasPrestamo.AsNoTracking()
            .Where(cuota => cuota.PrestamoId == prestamoId)
            .OrderBy(cuota => cuota.NumeroCuota)
            .ToListAsync();
        List<DescuentoCuotaPrestamo> descuentos = await contexto.DescuentosCuotaPrestamo.AsNoTracking().OrderBy(descuento => descuento.Id).ToListAsync();
        return cuotas
            .Select(cuota => string.Join(
                ",",
                descuentos
                    .Where(descuento => descuento.CuotaPrestamoId == cuota.Id)
                    .Select(descuento => descuento.PlanillaId.ToString(CultureInfo.InvariantCulture) + ":" + descuento.Monto.ToString("0.##", CultureInfo.InvariantCulture))))
            .ToArray();
    }

    public static async Task<Prestamo> LeerPrestamoAsync(OrdenesCompraApiFactory fabrica, int id)
    {
        List<Prestamo> prestamos = await LeerAsync(fabrica);
        return prestamos.Single(prestamo => prestamo.Id == id);
    }

    public static async Task ModificarAsync(OrdenesCompraApiFactory fabrica, Action<TitanGlobalDbContext> cambio)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        cambio(contexto);
        await contexto.SaveChangesAsync();
    }

    public static async Task<string[]> LeerFirmasAsync(OrdenesCompraApiFactory fabrica)
    {
        List<Prestamo> prestamos = await LeerAsync(fabrica);
        return prestamos.Select(prestamo =>
            $"{prestamo.Id}|{prestamo.Numero}|{prestamo.EmpleadoId}|{prestamo.Monto.ToString(CultureInfo.InvariantCulture)}|{prestamo.Saldo.ToString(CultureInfo.InvariantCulture)}|{prestamo.Estado}|{prestamo.MotivoAnulacion}")
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
                    || registro.Entidad == "ContratoLaboral"
                    || registro.Entidad == "AdendaContrato"
                    || registro.Entidad == "Prestamo"
                    || registro.Entidad == "CuotaPrestamo"
                    || registro.Entidad == "DescuentoCuotaPrestamo")
                .ToList());
        contexto.SaveChanges();
    }
}
