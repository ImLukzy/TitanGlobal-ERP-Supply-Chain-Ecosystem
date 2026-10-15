using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.UseCases;

internal static class ConsultaCotizacion
{
    public static async Task<CotizacionDetalleDto?> ObtenerDetalleAsync(IUnitOfWork unitOfWork, int id)
    {
        CotizacionConsulta? consulta = await Proyectar(unitOfWork.Repository<Cotizacion>().AsQueryable())
            .SingleOrDefaultAsync(cotizacion => cotizacion.Id == id);
        return consulta is null ? null : ConvertirDetalle(consulta);
    }

    public static async Task<List<CotizacionDetalleDto>> ObtenerTodasAsync(IUnitOfWork unitOfWork)
    {
        List<CotizacionConsulta> consultas = await Proyectar(unitOfWork.Repository<Cotizacion>().AsQueryable())
            .ToListAsync();
        return consultas.Select(ConvertirDetalle).ToList();
    }

    public static CotizacionDetalleDto ConvertirDetalle(CotizacionConsulta consulta)
    {
        List<LineaCotizacionDto> lineas = consulta.Lineas.Select(CalcularLinea).ToList();
        return new CotizacionDetalleDto
        {
            Id = consulta.Id,
            Numero = consulta.Numero,
            Fecha = FechasUtc.Normalizar(consulta.Fecha),
            ClienteId = consulta.ClienteId,
            ClienteNombre = consulta.ClienteNombre,
            FechaValidez = FechasUtc.Normalizar(consulta.FechaValidez),
            Estado = consulta.Estado,
            Vencida = EsVencida(consulta.Estado, consulta.FechaValidez),
            Observaciones = consulta.Observaciones,
            MotivoRechazo = consulta.MotivoRechazo,
            FechaDecision = consulta.FechaDecision.HasValue
                ? FechasUtc.Normalizar(consulta.FechaDecision.Value)
                : null,
            OrdenCompraId = consulta.OrdenCompraId,
            OrdenCompraNumero = consulta.OrdenCompraNumero,
            Subtotal = lineas.Sum(linea => linea.Subtotal),
            Descuento = lineas.Sum(linea => linea.Descuento),
            Total = lineas.Sum(linea => linea.Importe),
            Lineas = lineas
        };
    }

    public static LineaCotizacionDto CalcularLinea(LineaCotizacionConsulta linea)
    {
        decimal subtotal = decimal.Round(linea.Cantidad * linea.PrecioUnitario, 2, MidpointRounding.AwayFromZero);
        decimal descuento = decimal.Round(
            subtotal * linea.DescuentoPorcentaje / 100m,
            2,
            MidpointRounding.AwayFromZero);
        return new LineaCotizacionDto
        {
            ProductoId = linea.ProductoId,
            ProductoNombre = linea.ProductoNombre,
            Cantidad = linea.Cantidad,
            PrecioUnitario = linea.PrecioUnitario,
            DescuentoPorcentaje = linea.DescuentoPorcentaje,
            Subtotal = subtotal,
            Descuento = descuento,
            Importe = subtotal - descuento
        };
    }

    public static bool EsVencida(string estado, DateTime fechaValidez)
    {
        DateTime hoyUtc = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        return estado == "Emitida" && FechasUtc.Normalizar(fechaValidez).Date < hoyUtc;
    }

    internal static IQueryable<CotizacionConsulta> Proyectar(IQueryable<Cotizacion> consulta)
    {
        return consulta.Select(cotizacion => new CotizacionConsulta
        {
            Id = cotizacion.Id,
            Numero = cotizacion.Numero,
            Fecha = cotizacion.Fecha,
            ClienteId = cotizacion.ClienteId,
            ClienteNombre = cotizacion.Cliente.Nombre,
            FechaValidez = cotizacion.FechaValidez,
            Estado = cotizacion.Estado,
            Observaciones = cotizacion.Observaciones,
            MotivoRechazo = cotizacion.MotivoRechazo,
            FechaDecision = cotizacion.FechaDecision,
            OrdenCompraId = cotizacion.OrdenCompraId,
            OrdenCompraNumero = cotizacion.OrdenCompra == null ? null : cotizacion.OrdenCompra.NumeroOrden,
            Lineas = cotizacion.Detalles
                .OrderBy(detalle => detalle.Id)
                .Select(detalle => new LineaCotizacionConsulta
                {
                    ProductoId = detalle.ProductoId,
                    ProductoNombre = detalle.Producto.Nombre,
                    Cantidad = detalle.Cantidad,
                    PrecioUnitario = detalle.PrecioUnitario,
                    DescuentoPorcentaje = detalle.DescuentoPorcentaje
                })
                .ToList()
        });
    }
}

internal sealed class CotizacionConsulta
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public DateTime FechaValidez { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public string? MotivoRechazo { get; set; }
    public DateTime? FechaDecision { get; set; }
    public int? OrdenCompraId { get; set; }
    public string? OrdenCompraNumero { get; set; }
    public List<LineaCotizacionConsulta> Lineas { get; set; } = new List<LineaCotizacionConsulta>();
}

internal sealed class LineaCotizacionConsulta
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
}
