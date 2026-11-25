using System.Globalization;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Api.Tests.Modulo67_AdelantosPrestamos;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo66_Planillas;

// Datos y utilidades compartidas por las pruebas del módulo 66. Reutiliza los empleados y contratos de DatosPrestamos:
// empleados 1 (9500 con adenda, INTEGRA), 2 (4200, ONP), 3 (contrato formativo vencido, sin afiliación) y 4 (cesado).
internal static class DatosPlanillas
{
    public static void Restablecer(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.DescuentosCuotaPrestamo.RemoveRange(contexto.DescuentosCuotaPrestamo.ToList());
        contexto.PlanillaDetalles.RemoveRange(contexto.PlanillaDetalles.ToList());
        contexto.BonosPlanilla.RemoveRange(contexto.BonosPlanilla.ToList());
        contexto.Planillas.RemoveRange(contexto.Planillas.ToList());
        contexto.AfiliacionesPension.RemoveRange(contexto.AfiliacionesPension.ToList());
        contexto.ParametrosPension.RemoveRange(contexto.ParametrosPension.ToList());
        contexto.ParametrosPlanilla.RemoveRange(contexto.ParametrosPlanilla.ToList());
        contexto.TramosQuintaCategoria.RemoveRange(contexto.TramosQuintaCategoria.ToList());
        contexto.SaveChanges();
        DatosPrestamos.Restablecer(fabrica);
        contexto.ChangeTracker.Clear();
        contexto.ParametrosPlanilla.AddRange(
            Parametro("UIT", 5350m, "Unidad impositiva tributaria"),
            Parametro("TASA_ESSALUD", 0.09m, "Aporte del empleador a EsSalud"),
            Parametro("HORAS_JORNADA", 8m, "Horas de la jornada ordinaria"),
            Parametro("MINUTOS_RECARGO_25", 120m, "Minutos de cada jornada con recargo del 25 %"),
            Parametro("RECARGO_EXTRA_25", 0.25m, "Recargo de los primeros minutos extra"),
            Parametro("RECARGO_EXTRA_35", 0.35m, "Recargo del resto de minutos extra"),
            Parametro("DEDUCCION_UIT_QUINTA", 7m, "UIT deducibles en la quinta categoria"));
        contexto.ParametrosPension.AddRange(
            Pension(1, "ONP", "ONP", 0.13m, 0m, 0m, null),
            Pension(2, "INTEGRA", "AFP", 0.10m, 0.0137m, 0.0155m, 12027.10m),
            Pension(3, "PRIMA", "AFP", 0.10m, 0.0137m, 0.0160m, 12027.10m),
            Pension(4, "PROFUTURO", "AFP", 0.10m, 0.0137m, 0.0169m, 12027.10m),
            Pension(5, "HABITAT", "AFP", 0.10m, 0.0137m, 0.0147m, 12027.10m));
        contexto.TramosQuintaCategoria.AddRange(
            Tramo(1, 5m, 0.08m),
            Tramo(2, 20m, 0.14m),
            Tramo(3, 35m, 0.17m),
            Tramo(4, 45m, 0.20m),
            Tramo(5, null, 0.30m));
        contexto.SaveChanges();
        contexto.AfiliacionesPension.AddRange(
            Afiliacion(1, 1, 2, "CUSPP0000001"),
            Afiliacion(2, 2, 1, null));
        contexto.SaveChanges();
        BorrarAuditoria(contexto);
    }

    public static HttpClient ClienteConRoles(OrdenesCompraApiFactory fabrica, params string[] roles)
    {
        return DatosPrestamos.ClienteConRoles(fabrica, roles);
    }

    public static async Task<HttpResponseMessage> CrearPlanillaAsync(HttpClient cliente, int? anio, int? mes, string? tipo)
    {
        return await cliente.PostAsJsonAsync("/api/v1/planillas", new CrearPlanillaRequestDto { Anio = anio, Mes = mes, Tipo = tipo });
    }

    public static async Task<int> CrearAsync(HttpClient cliente, int anio, int mes, string tipo)
    {
        HttpResponseMessage respuesta = await CrearPlanillaAsync(cliente, anio, mes, tipo);
        Assert.Equal(System.Net.HttpStatusCode.OK, respuesta.StatusCode);
        PlanillaCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<PlanillaCreadaDto>();
        Assert.NotNull(creada);
        return creada.Id;
    }

    public static async Task<HttpResponseMessage> CalcularAsync(HttpClient cliente, int id)
    {
        return await cliente.PostAsync($"/api/v1/planillas/{id}/calculo", null);
    }

    public static async Task<HttpResponseMessage> AprobarAsync(HttpClient cliente, int id)
    {
        return await cliente.PostAsync($"/api/v1/planillas/{id}/aprobacion", null);
    }

    public static async Task<HttpResponseMessage> PagarAsync(HttpClient cliente, int id, DateOnly? fechaPago)
    {
        return await cliente.PostAsJsonAsync($"/api/v1/planillas/{id}/pago", new PagarPlanillaRequestDto { FechaPago = fechaPago });
    }

    public static async Task<PlanillaCalculoDto> CalcularOkAsync(HttpClient cliente, int id)
    {
        HttpResponseMessage respuesta = await CalcularAsync(cliente, id);
        Assert.Equal(System.Net.HttpStatusCode.OK, respuesta.StatusCode);
        PlanillaCalculoDto? resultado = await respuesta.Content.ReadFromJsonAsync<PlanillaCalculoDto>();
        Assert.NotNull(resultado);
        return resultado;
    }

    public static async Task<List<PlanillaEmpleadoDto>> LeerDetallesAsync(HttpClient cliente, int id)
    {
        PaginaPlanillaEmpleadosDto? pagina = await cliente.GetFromJsonAsync<PaginaPlanillaEmpleadosDto>($"/api/v1/planillas/{id}/detalles?tamanoPagina=100");
        Assert.NotNull(pagina);
        return pagina.Items;
    }

    public static async Task<PlanillaDetalleDto> LeerPlanillaAsync(HttpClient cliente, int id)
    {
        PlanillaDetalleDto? planilla = await cliente.GetFromJsonAsync<PlanillaDetalleDto>($"/api/v1/planillas/{id}");
        Assert.NotNull(planilla);
        return planilla;
    }

    public static async Task ModificarAsync(OrdenesCompraApiFactory fabrica, Action<TitanGlobalDbContext> cambio)
    {
        await DatosPrestamos.ModificarAsync(fabrica, cambio);
    }

    public static async Task AfiliarAsync(OrdenesCompraApiFactory fabrica, int empleadoId, int parametroPensionId)
    {
        await ModificarAsync(fabrica, contexto => contexto.AfiliacionesPension.Add(Afiliacion(100 + empleadoId, empleadoId, parametroPensionId, parametroPensionId == 1 ? null : "CUSPP0000099")));
    }

    public static async Task InsertarBonoAsync(OrdenesCompraApiFactory fabrica, int empleadoId, int anio, int mes, string tipo, decimal monto)
    {
        await ModificarAsync(fabrica, contexto => contexto.BonosPlanilla.Add(new BonoPlanilla
        {
            EmpleadoId = empleadoId,
            Anio = anio,
            Mes = mes,
            TipoPlanilla = tipo,
            Concepto = "Bono de prueba",
            Monto = monto
        }));
    }

    // Préstamo activo del empleado con una cuota por mes (anio, mes, monto) y el saldo igual al monto total.
    public static async Task<int> InsertarPrestamoAsync(OrdenesCompraApiFactory fabrica, int id, int empleadoId, params (int Anio, int Mes, decimal Monto)[] cuotas)
    {
        decimal total = cuotas.Sum(cuota => cuota.Monto);
        await ModificarAsync(fabrica, contexto =>
        {
            Prestamo prestamo = new Prestamo
            {
                Id = id,
                Numero = "PRE-" + id.ToString("D6", CultureInfo.InvariantCulture),
                EmpleadoId = empleadoId,
                Tipo = "Prestamo",
                Monto = total,
                NumeroCuotas = cuotas.Length,
                FechaOtorgamiento = new DateOnly(2026, 1, 10),
                PrimerAnio = cuotas[0].Anio,
                PrimerMes = cuotas[0].Mes,
                Motivo = "Prestamo de prueba",
                Estado = "Activo",
                Saldo = total
            };
            int numero = 1;
            foreach ((int Anio, int Mes, decimal Monto) cuota in cuotas)
            {
                prestamo.CuotasPrestamo.Add(new CuotaPrestamo
                {
                    NumeroCuota = numero,
                    Anio = cuota.Anio,
                    Mes = cuota.Mes,
                    Monto = cuota.Monto,
                    MontoDescontado = 0m,
                    Estado = "Pendiente"
                });
                numero++;
            }

            contexto.Prestamos.Add(prestamo);
        });
        return id;
    }

    public static async Task<Prestamo> LeerPrestamoAsync(OrdenesCompraApiFactory fabrica, int id)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Prestamos.AsNoTracking().Include(prestamo => prestamo.CuotasPrestamo).SingleAsync(prestamo => prestamo.Id == id);
    }

    public static async Task<int> ContarDescuentosAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.DescuentosCuotaPrestamo.CountAsync();
    }

    public static async Task<List<BonoPlanilla>> LeerBonosAsync(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.BonosPlanilla.AsNoTracking().OrderBy(bono => bono.Id).ToListAsync();
    }

    public static async Task<List<RegistroAuditoria>> LeerAuditoriaAsync(OrdenesCompraApiFactory fabrica, string entidad, int id)
    {
        return await DatosPrestamos.LeerAuditoriaAsync(fabrica, entidad, id);
    }

    private static ParametroPlanilla Parametro(string clave, decimal valor, string descripcion)
    {
        return new ParametroPlanilla { Clave = clave, Valor = valor, Descripcion = descripcion };
    }

    private static ParametroPension Pension(int id, string codigo, string sistema, decimal aporte, decimal prima, decimal comision, decimal? tope)
    {
        return new ParametroPension
        {
            Id = id,
            Codigo = codigo,
            Sistema = sistema,
            TasaAporte = aporte,
            TasaPrimaSeguro = prima,
            TasaComision = comision,
            RemuneracionMaximaAsegurable = tope
        };
    }

    private static TramoQuintaCategoria Tramo(int orden, decimal? hastaUit, decimal tasa)
    {
        return new TramoQuintaCategoria { Orden = orden, HastaUit = hastaUit, Tasa = tasa };
    }

    private static AfiliacionPension Afiliacion(int id, int empleadoId, int parametroId, string? cuspp)
    {
        return new AfiliacionPension
        {
            Id = id,
            EmpleadoId = empleadoId,
            ParametroPensionId = parametroId,
            Cuspp = cuspp,
            FechaAfiliacion = new DateOnly(2021, 3, 1)
        };
    }

    private static void BorrarAuditoria(TitanGlobalDbContext contexto)
    {
        string[] entidades = { "Planilla", "PlanillaDetalle", "BonoPlanilla", "AfiliacionPension", "ParametroPlanilla", "ParametroPension", "TramoQuintaCategoria" };
        contexto.RegistrosAuditoria.RemoveRange(contexto.RegistrosAuditoria.Where(registro => entidades.Contains(registro.Entidad)).ToList());
        contexto.SaveChanges();
    }
}
