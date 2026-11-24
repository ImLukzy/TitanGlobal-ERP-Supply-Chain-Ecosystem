using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

public class ListarTransportistasTercerosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarTransportistasTercerosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaTransportistasTercerosDto> ExecuteAsync(string? busqueda, string? pais, string? tipoServicio, bool? activo, int pagina, int tamanoPagina)
    {
        ValidacionLogisticaExterna.Pagina(pagina, tamanoPagina);
        IQueryable<TransportistaTercero> consulta = _unitOfWork.Repository<TransportistaTercero>().AsQueryable();
        // PATRÓN LINQ 1: filtros compuestos, sin cargar la tabla completa.
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(t => t.RazonSocial.ToLower().Contains(termino) || t.DocumentoFiscal.ToLower().Contains(termino));
        }
        if (!string.IsNullOrWhiteSpace(pais))
        {
            string filtroPais = pais.Trim().ToLowerInvariant();
            consulta = consulta.Where(t => t.Pais.ToLower() == filtroPais);
        }
        if (!string.IsNullOrWhiteSpace(tipoServicio))
        {
            string tipo = ValidacionLogisticaExterna.TipoServicio(tipoServicio);
            consulta = consulta.Where(t => t.TipoServicio == tipo);
        }
        if (activo.HasValue) { consulta = consulta.Where(t => t.Activo == activo.Value); }
        int total = await consulta.CountAsync();
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        List<TransportistaTerceroListadoDto> items = new List<TransportistaTerceroListadoDto>();
        if (desplazamiento < total)
        {
            items = await consulta.OrderBy(t => t.RazonSocial).ThenBy(t => t.Id).Skip((int)desplazamiento).Take(tamanoPagina)
                .Select(t => new TransportistaTerceroListadoDto
                {
                    Id = t.Id,
                    RazonSocial = t.RazonSocial,
                    DocumentoFiscal = t.DocumentoFiscal,
                    Pais = t.Pais,
                    TipoServicio = t.TipoServicio,
                    Activo = t.Activo
                }).ToListAsync();
        }
        return new PaginaTransportistasTercerosDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }
}
