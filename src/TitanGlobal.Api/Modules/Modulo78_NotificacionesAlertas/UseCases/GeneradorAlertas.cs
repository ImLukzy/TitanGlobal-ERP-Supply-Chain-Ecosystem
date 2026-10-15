using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo78_NotificacionesAlertas.Dtos;
using TitanGlobal.Api.Modules.Modulo03_Cotizaciones.UseCases;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo78_NotificacionesAlertas.UseCases;

internal static class GeneradorAlertas
{
    private static readonly string[] TiposOrdenados =
    {
        "ReclamoVencido",
        "OrdenAbastecimientoAtrasada",
        "ContratoExcedido",
        "AlmacenCasiLleno",
        "ContratoPorVencer",
        "CotizacionVencida",
        "SolicitudPendienteAntigua"
    };

    public static async Task<List<AlertaDto>> GenerarAsync(IUnitOfWork unitOfWork)
    {
        DateTime ahora = FechasUtc.Normalizar(DateTime.UtcNow);
        DateTime hoy = ahora.Date;
        List<AlertaDto> alertas = new List<AlertaDto>();

        IQueryable<AlertaDto> consultaReclamos =
            from reclamo in unitOfWork.Repository<ReclamoPostventa>().AsQueryable()
            join cliente in unitOfWork.Repository<Cliente>().AsQueryable()
                on reclamo.ClienteId equals cliente.Id
            where (reclamo.Estado == "Abierto" || reclamo.Estado == "EnProceso")
                && reclamo.FechaLimite < ahora
            select new AlertaDto
            {
                Clave = "reclamo-vencido:" + reclamo.Id,
                Tipo = "ReclamoVencido",
                Severidad = "Alta",
                Mensaje = "El reclamo " + reclamo.Numero + " de " + cliente.Nombre
                    + " superó su plazo de atención.",
                Modulo = "09",
                RegistroId = reclamo.Id,
                Ruta = "/modulos/09-reclamos-postventa",
                FechaReferencia = reclamo.FechaLimite
            };
        List<AlertaDto> reclamos = await consultaReclamos.ToListAsync();
        alertas.AddRange(reclamos);

        List<OrdenAlerta> ordenesAtrasadas = await unitOfWork.Repository<OrdenAbastecimiento>().AsQueryable()
            .Where(orden => orden.Estado == "Emitida" && orden.FechaEntregaEstimada < hoy)
            .Select(orden => new OrdenAlerta
            {
                Id = orden.Id,
                Numero = orden.Numero,
                ProveedorNombre = orden.Proveedor.RazonSocial,
                FechaEntrega = orden.FechaEntregaEstimada
            })
            .ToListAsync();
        foreach (OrdenAlerta orden in ordenesAtrasadas)
        {
            alertas.Add(new AlertaDto
            {
                Clave = "orden-atrasada:" + orden.Id,
                Tipo = "OrdenAbastecimientoAtrasada",
                Severidad = "Alta",
                Mensaje = "La orden " + orden.Numero + " de " + orden.ProveedorNombre
                    + " está atrasada desde el " + orden.FechaEntrega.ToString("dd/MM/yyyy") + ".",
                Modulo = "15",
                RegistroId = orden.Id,
                Ruta = "/modulos/15-ordenes-abastecimiento",
                FechaReferencia = orden.FechaEntrega
            });
        }

        List<ContratoAlerta> contratos = await unitOfWork.Repository<ContratoComercial>().AsQueryable()
            .Where(contrato => contrato.Estado == "Vigente")
            .Select(contrato => new ContratoAlerta
            {
                Id = contrato.Id,
                Numero = contrato.Numero,
                ClienteNombre = contrato.Cliente.Nombre,
                FechaFin = contrato.FechaFin,
                FechaInicio = contrato.FechaInicio,
                ClienteId = contrato.ClienteId,
                MontoComprometido = contrato.MontoComprometido,
                MontoConsumido = unitOfWork.Repository<OrdenCompra>().AsQueryable()
                    .Where(orden => orden.ClienteId == contrato.ClienteId
                        && orden.Fecha >= contrato.FechaInicio
                        && orden.Fecha < contrato.FechaFin.AddDays(1))
                    .Sum(orden => (decimal?)orden.Total) ?? 0m
            })
            .ToListAsync();
        foreach (ContratoAlerta contrato in contratos)
        {
            if (contrato.MontoConsumido > contrato.MontoComprometido)
            {
                decimal porcentaje = Math.Round(contrato.MontoConsumido / contrato.MontoComprometido * 100m, 2);
                alertas.Add(new AlertaDto
                {
                    Clave = "contrato-excedido:" + contrato.Id,
                    Tipo = "ContratoExcedido",
                    Severidad = "Alta",
                    Mensaje = "El contrato " + contrato.Numero + " de " + contrato.ClienteNombre + " va al "
                        + porcentaje.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                        + " % de su monto comprometido.",
                    Modulo = "05",
                    RegistroId = contrato.Id,
                    Ruta = "/modulos/05-contratos-comerciales",
                    FechaReferencia = contrato.FechaFin
                });
            }

            if (contrato.MontoConsumido <= contrato.MontoComprometido
                && contrato.FechaFin.Date >= hoy
                && contrato.FechaFin.Date <= hoy.AddDays(30))
            {
                alertas.Add(new AlertaDto
                {
                    Clave = "contrato-por-vencer:" + contrato.Id,
                    Tipo = "ContratoPorVencer",
                    Severidad = "Media",
                    Mensaje = "El contrato " + contrato.Numero + " de " + contrato.ClienteNombre
                        + " termina el " + contrato.FechaFin.ToString("dd/MM/yyyy") + ".",
                    Modulo = "05",
                    RegistroId = contrato.Id,
                    Ruta = "/modulos/05-contratos-comerciales",
                    FechaReferencia = contrato.FechaFin
                });
            }
        }

        List<AlmacenAlerta> almacenes = await unitOfWork.Repository<Almacen>().AsQueryable()
            .Where(almacen => almacen.Activo && almacen.CapacidadM3 > 0)
            .Select(almacen => new AlmacenAlerta
            {
                Id = almacen.Id,
                Codigo = almacen.Codigo,
                CapacidadM3 = almacen.CapacidadM3,
                CapacidadAsignadaM3 = unitOfWork.Repository<ZonaAlmacenaje>().AsQueryable()
                    .Where(zona => zona.AlmacenId == almacen.Id && zona.Activa)
                    .Sum(zona => (decimal?)zona.CapacidadM3) ?? 0m
            })
            .ToListAsync();
        foreach (AlmacenAlerta almacen in almacenes)
        {
            decimal porcentaje = decimal.Round(
                almacen.CapacidadAsignadaM3 / almacen.CapacidadM3 * 100m,
                2,
                MidpointRounding.AwayFromZero);
            if (porcentaje >= 90m)
            {
                alertas.Add(new AlertaDto
                {
                    Clave = "almacen-lleno:" + almacen.Id,
                    Tipo = "AlmacenCasiLleno",
                    Severidad = "Media",
                    Mensaje = "El almacén " + almacen.Codigo + " tiene asignado el "
                        + porcentaje.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                        + " % de su capacidad.",
                    Modulo = "24",
                    RegistroId = almacen.Id,
                    Ruta = "/modulos/24-zonas-almacenaje",
                    FechaReferencia = hoy
                });
            }
        }

        List<CotizacionAlerta> cotizaciones = await unitOfWork.Repository<Cotizacion>().AsQueryable()
            .Where(cotizacion => cotizacion.Estado == "Emitida")
            .Select(cotizacion => new CotizacionAlerta
            {
                Id = cotizacion.Id,
                Numero = cotizacion.Numero,
                ClienteNombre = cotizacion.Cliente.Nombre,
                FechaValidez = cotizacion.FechaValidez
            })
            .ToListAsync();
        foreach (CotizacionAlerta cotizacion in cotizaciones)
        {
            // PATRÓN LINQ 6: la condición de vencida reutiliza la fecha UTC por día del módulo 03.
            if (ConsultaCotizacion.EsVencida("Emitida", cotizacion.FechaValidez))
            {
                alertas.Add(new AlertaDto
                {
                    Clave = "cotizacion-vencida:" + cotizacion.Id,
                    Tipo = "CotizacionVencida",
                    Severidad = "Baja",
                    Mensaje = "La cotización " + cotizacion.Numero + " de " + cotizacion.ClienteNombre
                        + " venció sin respuesta.",
                    Modulo = "03",
                    RegistroId = cotizacion.Id,
                    Ruta = "/modulos/03-cotizaciones",
                    FechaReferencia = cotizacion.FechaValidez
                });
            }
        }

        DateTime limiteSolicitud = hoy.AddDays(-7);
        List<SolicitudAlerta> solicitudes = await unitOfWork.Repository<SolicitudCompra>().AsQueryable()
            .Where(solicitud => solicitud.Estado == "Pendiente" && solicitud.Fecha < limiteSolicitud)
            .Select(solicitud => new SolicitudAlerta
            {
                Id = solicitud.Id,
                Numero = solicitud.Numero,
                Fecha = solicitud.Fecha
            })
            .ToListAsync();
        foreach (SolicitudAlerta solicitud in solicitudes)
        {
            int dias = (hoy - solicitud.Fecha.Date).Days;
            alertas.Add(new AlertaDto
            {
                Clave = "solicitud-pendiente:" + solicitud.Id,
                Tipo = "SolicitudPendienteAntigua",
                Severidad = "Baja",
                Mensaje = "La solicitud " + solicitud.Numero + " lleva " + dias + " días pendiente.",
                Modulo = "13",
                RegistroId = solicitud.Id,
                Ruta = "/modulos/13-solicitudes-compra",
                FechaReferencia = solicitud.Fecha
            });
        }

        return alertas;
    }

    public static string[] ObtenerTipos()
    {
        return TiposOrdenados;
    }
}

internal sealed class ContratoAlerta
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public string ClienteNombre { get; set; } = string.Empty;

    public DateTime FechaFin { get; set; }

    public DateTime FechaInicio { get; set; }

    public int ClienteId { get; set; }

    public decimal MontoComprometido { get; set; }

    public decimal MontoConsumido { get; set; }
}

internal sealed class AlmacenAlerta
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public decimal CapacidadM3 { get; set; }

    public decimal CapacidadAsignadaM3 { get; set; }
}

internal sealed class SolicitudAlerta
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }
}

internal sealed class OrdenAlerta
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public string ProveedorNombre { get; set; } = string.Empty;

    public DateTime FechaEntrega { get; set; }
}

internal sealed class CotizacionAlerta
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public string ClienteNombre { get; set; } = string.Empty;

    public DateTime FechaValidez { get; set; }
}
