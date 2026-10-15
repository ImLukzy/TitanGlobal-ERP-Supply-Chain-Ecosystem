using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.UseCases;

public class ListarOrdenesAbastecimientoUseCase
{
    private static readonly string[] EstadosValidos = { "emitida", "recibida", "anulada" };

    private readonly IUnitOfWork _unitOfWork;

    public ListarOrdenesAbastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaOrdenesAbastecimientoDto> ExecuteAsync(
        string? busqueda,
        string? estado,
        int? proveedorId,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pagina,
        int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        string estadoNormalizado = estado?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(estadoNormalizado) && !EstadosValidos.Contains(estadoNormalizado))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El estado no es válido.");
        }

        DateTime? fechaDesdeUtc = fechaDesde.HasValue ? FechasUtc.Normalizar(fechaDesde.Value) : null;
        DateTime? fechaHastaUtc = fechaHasta.HasValue ? FechasUtc.Normalizar(fechaHasta.Value) : null;
        if (fechaDesdeUtc.HasValue && fechaHastaUtc.HasValue && fechaDesdeUtc.Value > fechaHastaUtc.Value)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "La fecha desde no puede ser posterior a la fecha hasta.");
        }

        if (fechaHastaUtc.HasValue)
        {
            fechaHastaUtc = ObtenerFinDelDiaUtc(fechaHastaUtc.Value);
        }

        IQueryable<OrdenAbastecimiento> consulta = _unitOfWork.Repository<OrdenAbastecimiento>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            consulta = consulta.Where(orden =>
                orden.Numero.ToLower().Contains(busquedaNormalizada)
                || orden.Proveedor.RazonSocial.ToLower().Contains(busquedaNormalizada));
        }

        if (!string.IsNullOrEmpty(estadoNormalizado))
        {
            consulta = consulta.Where(orden => orden.Estado.ToLower() == estadoNormalizado);
        }

        if (proveedorId.HasValue)
        {
            consulta = consulta.Where(orden => orden.ProveedorId == proveedorId.Value);
        }

        if (fechaDesdeUtc.HasValue)
        {
            DateTime limiteInferior = fechaDesdeUtc.Value;
            consulta = consulta.Where(orden => orden.Fecha >= limiteInferior);
        }

        if (fechaHastaUtc.HasValue)
        {
            DateTime limiteSuperior = fechaHastaUtc.Value;
            consulta = consulta.Where(orden => orden.Fecha <= limiteSuperior);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        DateTime hoyUtc = DateTime.UtcNow.Date;
        List<OrdenAbastecimientoListadoDto> items = await consulta
            .OrderByDescending(orden => orden.Fecha)
            .ThenByDescending(orden => orden.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(orden => new OrdenAbastecimientoListadoDto
            {
                Id = orden.Id,
                Numero = orden.Numero,
                Fecha = orden.Fecha,
                ProveedorId = orden.ProveedorId,
                ProveedorNombre = orden.Proveedor.RazonSocial,
                SolicitudNumero = orden.SolicitudCompra == null ? null : orden.SolicitudCompra.Numero,
                Estado = orden.Estado,
                FechaEntregaEstimada = orden.FechaEntregaEstimada,
                Atrasada = orden.Estado == "Emitida" && orden.FechaEntregaEstimada < hoyUtc,
                MontoTotal = orden.Detalles.Sum(detalle => (decimal?)(detalle.Cantidad * detalle.PrecioUnitario)) ?? 0m
            })
            .ToListAsync();

        return new PaginaOrdenesAbastecimientoDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    private static DateTime ObtenerFinDelDiaUtc(DateTime fecha)
    {
        DateTime fechaNormalizada = FechasUtc.Normalizar(fecha);
        if (fechaNormalizada.Date == DateTime.MaxValue.Date)
        {
            return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        }

        return DateTime.SpecifyKind(fechaNormalizada.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
    }
}
