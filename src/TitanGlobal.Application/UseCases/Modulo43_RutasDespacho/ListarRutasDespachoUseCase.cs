using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho;

public class ListarRutasDespachoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarRutasDespachoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaRutasDespachoDto> ExecuteAsync(string? busqueda, string? zona, bool? activo, int pagina, int tamanoPagina)
    {
        ValidacionRutaDespacho.Pagina(pagina, tamanoPagina);
        IQueryable<RutaDespacho> consulta = _unitOfWork.Repository<RutaDespacho>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(r => r.Codigo.ToLower().Contains(termino) || r.Nombre.ToLower().Contains(termino) || r.Origen.ToLower().Contains(termino) || r.Destino.ToLower().Contains(termino));
        }
        if (!string.IsNullOrWhiteSpace(zona))
        {
            string zonaNormalizada = zona.Trim().ToLowerInvariant();
            consulta = consulta.Where(r => r.Zona.Trim().ToLower() == zonaNormalizada);
        }
        if (activo.HasValue) { consulta = consulta.Where(r => r.Activo == activo.Value); }
        int total = await consulta.CountAsync();
        long desplazamiento = (long)(pagina - 1) * tamanoPagina;
        List<RutaDespachoListadoDto> items = new List<RutaDespachoListadoDto>();
        if (desplazamiento < total)
        {
            items = await consulta.OrderBy(r => r.Codigo).ThenBy(r => r.Id).Skip((int)desplazamiento).Take(tamanoPagina)
                .Select(r => new RutaDespachoListadoDto
                {
                    Id = r.Id,
                    Codigo = r.Codigo,
                    Nombre = r.Nombre,
                    Zona = r.Zona,
                    Origen = r.Origen,
                    Destino = r.Destino,
                    Activo = r.Activo,
                    DistanciaKm = r.DistanciaKm.ToString("0.00", CultureInfo.InvariantCulture),
                    PeajeEstimado = r.PeajeEstimado.ToString("0.00", CultureInfo.InvariantCulture),
                    Moneda = r.Moneda
                }).ToListAsync();
        }
        return new PaginaRutasDespachoDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }
}
