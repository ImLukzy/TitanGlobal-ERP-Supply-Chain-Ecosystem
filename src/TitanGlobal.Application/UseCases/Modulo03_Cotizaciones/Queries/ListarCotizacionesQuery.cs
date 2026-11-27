using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones.Queries;

public class ListarCotizacionesQuery : IRequest<PaginaCotizacionesDto>
{
    public string? Busqueda { get; set; }

    public string? Estado { get; set; }

    public int? ClienteId { get; set; }

    public bool? Vencidas { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarCotizacionesQueryHandler : IRequestHandler<ListarCotizacionesQuery, PaginaCotizacionesDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarCotizacionesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaCotizacionesDto> Handle(ListarCotizacionesQuery request, CancellationToken cancellationToken)
    {
        ValidacionCotizacion.ValidarPaginacion(request.Pagina, request.TamanoPagina);
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

        IQueryable<Cotizacion> consulta = _unitOfWork.Repository<Cotizacion>().AsQueryable();
        string busquedaNormalizada = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            consulta = consulta.Where(cotizacion =>
                cotizacion.Numero.ToLower().Contains(busquedaNormalizada)
                || cotizacion.Cliente.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        if (!string.IsNullOrEmpty(estadoNormalizado))
        {
            consulta = consulta.Where(cotizacion => cotizacion.Estado.ToLower() == estadoNormalizado);
        }

        if (request.ClienteId.HasValue)
        {
            consulta = consulta.Where(cotizacion => cotizacion.ClienteId == request.ClienteId.Value);
        }

        if (request.Vencidas == true)
        {
            DateTime inicioHoyUtc = FechasUtc.Normalizar(DateTime.UtcNow).Date;
            consulta = consulta.Where(cotizacion =>
                cotizacion.Estado == "Emitida" && cotizacion.FechaValidez < inicioHoyUtc);
        }

        if (fechaDesdeUtc.HasValue)
        {
            DateTime limiteInferior = fechaDesdeUtc.Value;
            consulta = consulta.Where(cotizacion => cotizacion.Fecha >= limiteInferior);
        }

        if (fechaHastaUtc.HasValue)
        {
            DateTime limiteSuperior = fechaHastaUtc.Value;
            consulta = consulta.Where(cotizacion => cotizacion.Fecha <= limiteSuperior);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<CotizacionConsulta> consultas = await ConsultaCotizacion.Proyectar(consulta
                .OrderByDescending(cotizacion => cotizacion.Fecha)
                .ThenByDescending(cotizacion => cotizacion.Id)
                .Skip(saltar)
                .Take(request.TamanoPagina))
            .ToListAsync();
        List<CotizacionDetalleDto> detalles = consultas
            .Select(ConsultaCotizacion.ConvertirDetalle)
            .ToList();
        List<CotizacionListadoDto> items = detalles.Select(detalle => new CotizacionListadoDto
        {
            Id = detalle.Id,
            Numero = detalle.Numero,
            Fecha = detalle.Fecha,
            ClienteId = detalle.ClienteId,
            ClienteNombre = detalle.ClienteNombre,
            FechaValidez = detalle.FechaValidez,
            Estado = detalle.Estado,
            Vencida = detalle.Vencida,
            Total = detalle.Total,
            OrdenCompraNumero = detalle.OrdenCompraNumero
        }).ToList();

        return new PaginaCotizacionesDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static readonly HashSet<string> EstadosValidos = new HashSet<string>
    {
        "emitida",
        "aceptada",
        "rechazada"
    };

    private static DateTime ObtenerFinDelDiaUtc(DateTime fecha)
    {
        DateTime fechaNormalizada = FechasUtc.Normalizar(fecha);
        if (fechaNormalizada.Date == DateTime.MaxValue.Date)
        {
            return FechasUtc.Normalizar(DateTime.MaxValue);
        }

        return FechasUtc.Normalizar(fechaNormalizada.Date.AddDays(1).AddTicks(-1));
    }
}
