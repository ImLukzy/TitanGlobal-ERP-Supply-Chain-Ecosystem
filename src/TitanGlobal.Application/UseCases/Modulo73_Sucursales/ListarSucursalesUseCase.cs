using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo73_Sucursales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
namespace TitanGlobal.Application.UseCases.Modulo73_Sucursales;

public class ListarSucursalesUseCase
{
    private readonly IUnitOfWork _unidad;
    public ListarSucursalesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<PaginaSucursalesDto> ExecuteAsync(string? busqueda, string? tipo, string? pais, bool? activa, int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionSucursal.Error("El número de página debe ser mayor que cero.");
        }
        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionSucursal.Error("El tamaño de página debe estar entre 1 y 100.");
        }
        IQueryable<Sucursal> consulta = _unidad.Repository<Sucursal>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            string normalizado = ValidacionSucursal.Tipo(tipo);
            consulta = consulta.Where(s => s.Tipo == normalizado);
        }
        if (!string.IsNullOrWhiteSpace(pais))
        {
            string normalizado = pais.Trim().ToLowerInvariant();
            consulta = consulta.Where(s => s.Pais.ToLower() == normalizado);
        }
        if (activa.HasValue)
        {
            consulta = consulta.Where(s => s.Activa == activa.Value);
        }
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(s => s.Codigo.ToLower().Contains(termino) || s.Nombre.ToLower().Contains(termino) || s.Ciudad.ToLower().Contains(termino));
        }
        int total = await consulta.CountAsync();
        long salto = (long)(pagina - 1) * tamanoPagina;
        List<SucursalListadoDto> items = new List<SucursalListadoDto>();
        if (salto < total)
        {
            List<Sucursal> sucursales = await consulta.OrderBy(s => s.Codigo).ThenBy(s => s.Id).Skip((int)salto).Take(tamanoPagina).ToListAsync();
            foreach (Sucursal sucursal in sucursales)
            {
                items.Add(PresentacionSucursal.Listado(sucursal));
            }
        }
        return new PaginaSucursalesDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }
}
