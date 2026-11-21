using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;

public class ListarEntregasUseCase
{
    private readonly IUnitOfWork _unidad;

    public ListarEntregasUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaEntregasDto> ExecuteAsync(string? busqueda, string? estado, int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionEntrega.Error("El número de página debe ser mayor que cero.");
        }
        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionEntrega.Error("El tamaño de página debe estar entre 1 y 100.");
        }
        IQueryable<EntregaDespacho> consulta = _unidad.Repository<EntregaDespacho>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            string? normalizado = new[] { "Programada", "EnRuta", "Entregada", "Cancelada" }
                .FirstOrDefault(e => e.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));
            if (normalizado is null)
            {
                throw ValidacionEntrega.Error("El estado debe ser Programada, EnRuta, Entregada o Cancelada.");
            }
            consulta = consulta.Where(e => e.Estado == normalizado);
        }
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            string termino = busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(e => e.Codigo.ToLower().Contains(termino) || e.ReferenciaGuia.ToLower().Contains(termino) || e.Destinatario.ToLower().Contains(termino));
        }
        int total = await consulta.CountAsync();
        long salto = (long)(pagina - 1) * tamanoPagina;
        List<EntregaListadoDto> items = new List<EntregaListadoDto>();
        if (salto < total)
        {
            items = await consulta.OrderBy(e => e.Codigo).ThenBy(e => e.Id).Skip((int)salto).Take(tamanoPagina)
                .Select(e => new EntregaListadoDto
                {
                    Id = e.Id,
                    Codigo = e.Codigo,
                    ReferenciaGuia = e.ReferenciaGuia,
                    Destinatario = e.Destinatario,
                    DireccionDestino = e.DireccionDestino,
                    FechaProgramada = e.FechaProgramada,
                    Estado = e.Estado
                }).ToListAsync();
        }
        return new PaginaEntregasDto { Items = items, Total = total, Pagina = pagina, TamanoPagina = tamanoPagina };

    }
}
