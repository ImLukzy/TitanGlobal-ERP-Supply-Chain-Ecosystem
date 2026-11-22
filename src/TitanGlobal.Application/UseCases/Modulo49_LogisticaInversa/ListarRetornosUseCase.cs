using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;

public class ListarRetornosUseCase
{
    private readonly IUnitOfWork _unidad;
    public ListarRetornosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<PaginaRetornosDto> ExecuteAsync(string? busqueda, string? estado, string? tipo, int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionRetorno.Error("El número de página debe ser mayor que cero.");
        }
        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionRetorno.Error("El tamaño de página debe estar entre 1 y 100.");
        }
        IQueryable<RetornoLogistico> consulta = _unidad.Repository<RetornoLogistico>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            string? normalizado = new[] { "Programado", "Recogido", "Recibido", "Cancelado" }.FirstOrDefault(e => e.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));
            if (normalizado is null)
            {
                throw ValidacionRetorno.Error("El estado debe ser Programado, Recogido, Recibido o Cancelado.");
            }
            consulta = consulta.Where(r => r.Estado == normalizado);
        }
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            string normalizado = ValidacionRetorno.Tipo(tipo);
            consulta = consulta.Where(r => r.Tipo == normalizado);
        }
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(r => r.Codigo.ToLower().Contains(termino) || r.ReferenciaOrigen.ToLower().Contains(termino) || r.Contraparte.ToLower().Contains(termino) || r.Descripcion.ToLower().Contains(termino));
        }
        int total = await consulta.CountAsync();
        long salto = (long)(pagina - 1) * tamanoPagina;
        List<RetornoListadoDto> items = new List<RetornoListadoDto>();
        if (salto < total)
        {
            List<RetornoLogistico> retornos = await consulta.OrderBy(r => r.Codigo).ThenBy(r => r.Id).Skip((int)salto).Take(tamanoPagina).ToListAsync();
            foreach (RetornoLogistico retorno in retornos)
            {
                items.Add(PresentacionRetorno.Listado(retorno));
            }
        }
        return new PaginaRetornosDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };
    }
}
