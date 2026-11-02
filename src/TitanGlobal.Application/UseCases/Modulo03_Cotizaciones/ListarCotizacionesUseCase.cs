using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones;

public class ListarCotizacionesUseCase
{
    private static readonly HashSet<string> EstadosValidos = new HashSet<string>
    {
        "emitida",
        "aceptada",
        "rechazada"
    };

    private readonly IUnitOfWork _unitOfWork;

    public ListarCotizacionesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaCotizacionesDto> ExecuteAsync(
        string? busqueda,
        string? estado,
        int? clienteId,
        bool? vencidas,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pagina,
        int tamanoPagina)
    {
        ValidacionCotizacion.ValidarPaginacion(pagina, tamanoPagina);
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

        IQueryable<Cotizacion> consulta = _unitOfWork.Repository<Cotizacion>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
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

        if (clienteId.HasValue)
        {
            consulta = consulta.Where(cotizacion => cotizacion.ClienteId == clienteId.Value);
        }

        if (vencidas == true)
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
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<CotizacionConsulta> consultas = await ConsultaCotizacion.Proyectar(consulta
                .OrderByDescending(cotizacion => cotizacion.Fecha)
                .ThenByDescending(cotizacion => cotizacion.Id)
                .Skip(saltar)
                .Take(tamanoPagina))
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

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
