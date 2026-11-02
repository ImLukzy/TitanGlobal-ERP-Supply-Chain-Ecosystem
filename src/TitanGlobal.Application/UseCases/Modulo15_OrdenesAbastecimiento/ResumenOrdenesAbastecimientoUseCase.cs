using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento;

public class ResumenOrdenesAbastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenOrdenesAbastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenOrdenesAbastecimientoDto> ExecuteAsync()
    {
        IQueryable<OrdenAbastecimiento> ordenes = _unitOfWork.Repository<OrdenAbastecimiento>().AsQueryable();
        DateTime hoyUtc = DateTime.UtcNow.Date;
        ResumenOrdenesAbastecimientoDto resumen = new ResumenOrdenesAbastecimientoDto
        {
            Total = await ordenes.CountAsync(),
            Emitidas = await ordenes.CountAsync(orden => orden.Estado == "Emitida"),
            Recibidas = await ordenes.CountAsync(orden => orden.Estado == "Recibida"),
            Anuladas = await ordenes.CountAsync(orden => orden.Estado == "Anulada"),
            Atrasadas = await ordenes.CountAsync(orden =>
                orden.Estado == "Emitida" && orden.FechaEntregaEstimada < hoyUtc),
            MontoPorRecibir = await ordenes
                .Where(orden => orden.Estado == "Emitida")
                .SelectMany(orden => orden.Detalles)
                .SumAsync(detalle => (decimal?)(detalle.Cantidad * detalle.PrecioUnitario)) ?? 0m,
            MontoRecibido = await ordenes
                .Where(orden => orden.Estado == "Recibida")
                .SelectMany(orden => orden.Detalles)
                .SumAsync(detalle => (decimal?)(detalle.Cantidad * detalle.PrecioUnitario)) ?? 0m
        };

        // PATRÓN LINQ 9: agrupación por proveedor y orden por monto.
        resumen.TopProveedores = await ordenes
            .Where(orden => orden.Estado != "Anulada")
            .GroupBy(orden => new { orden.ProveedorId, orden.Proveedor.RazonSocial })
            .Select(grupo => new ProveedorMontoDto
            {
                ProveedorId = grupo.Key.ProveedorId,
                RazonSocial = grupo.Key.RazonSocial,
                Ordenes = grupo.Count(),
                Monto = grupo.Sum(orden => orden.Detalles.Sum(detalle => detalle.Cantidad * detalle.PrecioUnitario))
            })
            .OrderByDescending(proveedor => proveedor.Monto)
            .ThenBy(proveedor => proveedor.ProveedorId)
            .Take(5)
            .ToListAsync();

        return resumen;
    }
}
