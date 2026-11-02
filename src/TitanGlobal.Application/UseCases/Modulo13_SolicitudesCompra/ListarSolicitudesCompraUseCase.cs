using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo13_SolicitudesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo13_SolicitudesCompra;

public class ListarSolicitudesCompraUseCase
{
    private static readonly string[] EstadosValidos = { "pendiente", "aprobada", "rechazada" };

    private readonly IUnitOfWork _unitOfWork;

    public ListarSolicitudesCompraUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaSolicitudesDto> ExecuteAsync(
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
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        string estadoNormalizado = estado?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(estadoNormalizado) && !EstadosValidos.Contains(estadoNormalizado))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El estado no es válido.");
        }

        DateTime? fechaDesdeUtc = fechaDesde.HasValue ? FechasUtc.Normalizar(fechaDesde.Value) : null;
        DateTime? fechaHastaUtc = fechaHasta.HasValue ? FechasUtc.Normalizar(fechaHasta.Value) : null;
        if (fechaDesdeUtc.HasValue && fechaHastaUtc.HasValue && fechaDesdeUtc.Value > fechaHastaUtc.Value)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha desde no puede ser posterior a la fecha hasta.");
        }

        if (fechaHastaUtc.HasValue)
        {
            fechaHastaUtc = ObtenerFinDelDiaUtc(fechaHastaUtc.Value);
        }

        IQueryable<SolicitudCompra> consulta = _unitOfWork.Repository<SolicitudCompra>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            consulta = consulta.Where(solicitud =>
                solicitud.Numero.ToLower().Contains(busquedaNormalizada)
                || solicitud.Solicitante.ToLower().Contains(busquedaNormalizada));
        }

        if (!string.IsNullOrEmpty(estadoNormalizado))
        {
            consulta = consulta.Where(solicitud => solicitud.Estado.ToLower() == estadoNormalizado);
        }

        if (proveedorId.HasValue)
        {
            consulta = consulta.Where(solicitud => solicitud.ProveedorId == proveedorId.Value);
        }

        if (fechaDesdeUtc.HasValue)
        {
            DateTime limiteInferior = fechaDesdeUtc.Value;
            consulta = consulta.Where(solicitud => solicitud.Fecha >= limiteInferior);
        }

        if (fechaHastaUtc.HasValue)
        {
            DateTime limiteSuperior = fechaHastaUtc.Value;
            consulta = consulta.Where(solicitud => solicitud.Fecha <= limiteSuperior);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<SolicitudListadoDto> items = await consulta
            .OrderByDescending(solicitud => solicitud.Fecha)
            .ThenByDescending(solicitud => solicitud.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(solicitud => new SolicitudListadoDto
            {
                Id = solicitud.Id,
                Numero = solicitud.Numero,
                Fecha = solicitud.Fecha,
                Solicitante = solicitud.Solicitante,
                ProveedorId = solicitud.ProveedorId,
                ProveedorNombre = solicitud.Proveedor == null ? null : solicitud.Proveedor.RazonSocial,
                Estado = solicitud.Estado,
                TotalLineas = solicitud.Detalles.Count(),
                MontoEstimado = solicitud.Detalles.Sum(detalle => (decimal?)(detalle.Cantidad * detalle.PrecioEstimado)) ?? 0m
            })
            .ToListAsync();

        return new PaginaSolicitudesDto
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
