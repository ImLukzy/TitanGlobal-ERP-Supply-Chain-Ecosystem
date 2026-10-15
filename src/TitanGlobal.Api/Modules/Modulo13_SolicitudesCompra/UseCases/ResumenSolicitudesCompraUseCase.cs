using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.UseCases;

public class ResumenSolicitudesCompraUseCase
{
    private const string EstadoPendiente = "Pendiente";

    private const string EstadoAprobada = "Aprobada";

    private const string EstadoRechazada = "Rechazada";

    private readonly IUnitOfWork _unitOfWork;

    public ResumenSolicitudesCompraUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenSolicitudesDto> ExecuteAsync()
    {
        IQueryable<SolicitudCompra> solicitudes = _unitOfWork.Repository<SolicitudCompra>().AsQueryable();
        int total = await solicitudes.CountAsync();
        int pendientes = await solicitudes.CountAsync(solicitud => solicitud.Estado == EstadoPendiente);
        int aprobadas = await solicitudes.CountAsync(solicitud => solicitud.Estado == EstadoAprobada);
        int rechazadas = await solicitudes.CountAsync(solicitud => solicitud.Estado == EstadoRechazada);
        decimal montoPendiente = await solicitudes
            .Where(solicitud => solicitud.Estado == EstadoPendiente)
            .SelectMany(solicitud => solicitud.Detalles)
            .SumAsync(detalle => (decimal?)(detalle.Cantidad * detalle.PrecioEstimado)) ?? 0m;
        decimal montoAprobado = await solicitudes
            .Where(solicitud => solicitud.Estado == EstadoAprobada)
            .SelectMany(solicitud => solicitud.Detalles)
            .SumAsync(detalle => (decimal?)(detalle.Cantidad * detalle.PrecioEstimado)) ?? 0m;
        // PATRÓN LINQ 11: aplanado de líneas asociadas a estados.

        List<ProductoSolicitadoDto> topProductosSolicitados = await solicitudes
            .Where(solicitud => solicitud.Estado != EstadoRechazada)
            .SelectMany(solicitud => solicitud.Detalles)
            .GroupBy(detalle => new { detalle.ProductoId, detalle.Producto.Nombre })
            .Select(grupo => new ProductoSolicitadoDto
            {
                ProductoId = grupo.Key.ProductoId,
                Nombre = grupo.Key.Nombre,
                Unidades = grupo.Sum(detalle => detalle.Cantidad)
            })
            .OrderByDescending(producto => producto.Unidades)
            .ThenBy(producto => producto.ProductoId)
            .Take(5)
            .ToListAsync();
        // PATRÓN LINQ 9: agrupación y orden por conteo.

        return new ResumenSolicitudesDto
        {
            Total = total,
            Pendientes = pendientes,
            Aprobadas = aprobadas,
            Rechazadas = rechazadas,
            MontoPendiente = montoPendiente,
            MontoAprobado = montoAprobado,
            TopProductosSolicitados = topProductosSolicitados
        };
    }
}
