using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento.Queries;

public class ListarOrdenesAbastecimientoQuery : IRequest<PaginaOrdenesAbastecimientoDto>
{
    public string? Busqueda { get; set; }

    public string? Estado { get; set; }

    public int? ProveedorId { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarOrdenesAbastecimientoQueryHandler : IRequestHandler<ListarOrdenesAbastecimientoQuery, PaginaOrdenesAbastecimientoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarOrdenesAbastecimientoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaOrdenesAbastecimientoDto> Handle(ListarOrdenesAbastecimientoQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        string estadoNormalizado = request.Estado?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(estadoNormalizado) && !EstadosValidos.Contains(estadoNormalizado))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El estado no es válido.");
        }

        DateTime? fechaDesdeUtc = request.FechaDesde.HasValue ? FechasUtc.Normalizar(request.FechaDesde.Value) : null;
        DateTime? fechaHastaUtc = request.FechaHasta.HasValue ? FechasUtc.Normalizar(request.FechaHasta.Value) : null;
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

        IQueryable<OrdenAbastecimiento> consulta = _unitOfWork.Repository<OrdenAbastecimiento>().AsQueryable();
        string busquedaNormalizada = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
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

        if (request.ProveedorId.HasValue)
        {
            consulta = consulta.Where(orden => orden.ProveedorId == request.ProveedorId.Value);
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
        long inicio = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        DateTime hoyUtc = DateTime.UtcNow.Date;
        List<OrdenAbastecimientoListadoDto> items = await consulta
            .OrderByDescending(orden => orden.Fecha)
            .ThenByDescending(orden => orden.Id)
            .Skip(saltar)
            .Take(request.TamanoPagina)
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
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static readonly string[] EstadosValidos = { "emitida", "recibida", "anulada" };

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
