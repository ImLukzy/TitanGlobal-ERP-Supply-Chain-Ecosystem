using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

public class ListarFletesTercerosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarFletesTercerosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaFletesTercerosDto> ExecuteAsync(int? transportistaId, string? busqueda, string? estado, int pagina, int tamanoPagina)
    {
        ValidacionLogisticaExterna.Pagina(pagina, tamanoPagina);
        IQueryable<FleteTercero> consulta = _unitOfWork.Repository<FleteTercero>().AsQueryable();
        if (transportistaId.HasValue)
        {
            if (transportistaId.Value <= 0) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El transportista es obligatorio."); }
            consulta = consulta.Where(f => f.TransportistaTerceroId == transportistaId.Value);
        }
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(f => f.Referencia.ToLower().Contains(termino) || f.Origen.ToLower().Contains(termino) || f.Destino.ToLower().Contains(termino));
        }
        if (!string.IsNullOrWhiteSpace(estado))
        {
            string estadoCanonico = estado.Trim().Equals("Registrado", StringComparison.OrdinalIgnoreCase) ? "Registrado"
                : estado.Trim().Equals("Cancelado", StringComparison.OrdinalIgnoreCase) ? "Cancelado" : string.Empty;
            if (estadoCanonico == string.Empty) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El estado debe ser Registrado o Cancelado."); }
            consulta = consulta.Where(f => f.Estado == estadoCanonico);
        }
        int total = await consulta.CountAsync();
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        List<FleteTerceroListadoDto> items = new List<FleteTerceroListadoDto>();
        if (desplazamiento < total)
        {
            items = await consulta.OrderByDescending(f => f.Fecha).ThenByDescending(f => f.Id).Skip((int)desplazamiento).Take(tamanoPagina)
                .Select(f => new FleteTerceroListadoDto
                {
                    Id = f.Id,
                    TransportistaId = f.TransportistaTerceroId,
                    TransportistaRazonSocial = f.TransportistaTercero.RazonSocial,
                    Referencia = f.Referencia,
                    Fecha = f.Fecha,
                    Origen = f.Origen,
                    Destino = f.Destino,
                    Importe = f.Importe.ToString("0.00", CultureInfo.InvariantCulture),
                    Moneda = f.Moneda,
                    Estado = f.Estado
                }).ToListAsync();
        }
        return new PaginaFletesTercerosDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }
}
