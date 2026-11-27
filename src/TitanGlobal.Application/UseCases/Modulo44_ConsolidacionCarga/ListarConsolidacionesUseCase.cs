using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga;

public class ListarConsolidacionesUseCase
{
    private readonly IUnitOfWork _unidad;
    public ListarConsolidacionesUseCase(IUnitOfWork unidad)
    { _unidad = unidad; }
    public async Task<PaginaConsolidacionesDto> ExecuteAsync(string? busqueda, string? estado, int pagina, int tamanoPagina)
    {
        if (pagina < 1) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El número de página debe ser mayor que cero."); }
        if (tamanoPagina < 1 || tamanoPagina > 100) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100."); }
        IQueryable<ConsolidacionCarga> consulta = _unidad.Repository<ConsolidacionCarga>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            string? normalizado = new[] { "Borrador", "Confirmada", "Cancelada" }.FirstOrDefault(e => e.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));
            if (normalizado is null) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El estado debe ser Borrador, Confirmada o Cancelada."); }
            consulta = consulta.Where(c => c.Estado == normalizado);
        }
        if (!string.IsNullOrWhiteSpace(busqueda)) { string termino = busqueda.Trim().ToLowerInvariant(); consulta = consulta.Where(c => c.Codigo.ToLower().Contains(termino) || c.Nombre.ToLower().Contains(termino)); }
        int total = await consulta.CountAsync(); long salto = (long)(pagina - 1) * tamanoPagina;
        List<ConsolidacionListadoDto> items = new List<ConsolidacionListadoDto>();
        if (salto < total)
        {
            List<ConsolidacionCarga> cargas = await consulta.OrderBy(c => c.Codigo).ThenBy(c => c.Id).Skip((int)salto).Take(tamanoPagina).Include(c => c.Lineas).ToListAsync();
            foreach (ConsolidacionCarga carga in cargas) { items.Add(CalculoCarga.Detalle(carga)); }
        }
        return new PaginaConsolidacionesDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }
}
