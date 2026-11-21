using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;

public class ListarGuiasUseCase
{
    private readonly IUnitOfWork _unidad;

    public ListarGuiasUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaGuiasDto> ExecuteAsync(string? busqueda, string? estado, int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionGuia.Error("El número de página debe ser mayor que cero.");
        }
        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionGuia.Error("El tamaño de página debe estar entre 1 y 100.");
        }
        IQueryable<GuiaRemision> consulta = _unidad.Repository<GuiaRemision>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            string? normalizado = new[] { "Borrador", "Emitida", "Anulada" }
                .FirstOrDefault(e => e.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));
            if (normalizado is null)
            {
                throw ValidacionGuia.Error("El estado debe ser Borrador, Emitida o Anulada.");
            }
            consulta = consulta.Where(g => g.Estado == normalizado);
        }
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(g => g.Serie.ToLower().Contains(termino) || g.DestinatarioNombre.ToLower().Contains(termino) || g.DestinatarioDocumento.ToLower().Contains(termino));
        }
        int total = await consulta.CountAsync();
        long salto = (long)(pagina - 1) * tamanoPagina;
        List<GuiaListadoDto> items = new List<GuiaListadoDto>();
        if (salto < total)
        {
            List<GuiaRemision> guias = await consulta.OrderBy(g => g.Serie).ThenBy(g => g.Numero).ThenBy(g => g.Id)
                .Skip((int)salto).Take(tamanoPagina).Include(g => g.Lineas).ToListAsync();
            foreach (GuiaRemision guia in guias)
            {
                items.Add(PresentacionGuia.Listado(guia));
            }
        }
        return new PaginaGuiasDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }
}
