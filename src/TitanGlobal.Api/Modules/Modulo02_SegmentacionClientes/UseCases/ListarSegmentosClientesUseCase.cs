using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.UseCases;

public class ListarSegmentosClientesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarSegmentosClientesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaSegmentosDto> ExecuteAsync(string? busqueda, bool? activo, int pagina, int tamanoPagina)
    {
        ValidacionSegmentoCliente.ValidarPaginacion(pagina, tamanoPagina);
        IQueryable<SegmentoCliente> consulta = _unitOfWork.Repository<SegmentoCliente>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            consulta = consulta.Where(segmento =>
                segmento.Codigo.ToLower().Contains(busquedaNormalizada)
                || segmento.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        if (activo.HasValue)
        {
            consulta = consulta.Where(segmento => segmento.Activo == activo.Value);
        }

        int total = await consulta.CountAsync();
        List<SegmentoCliente> segmentos = await consulta
            .OrderBy(segmento => segmento.Prioridad)
            .ThenBy(segmento => segmento.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();
        List<ClienteClasificadoDto> clientes = await ConsultaSegmentacionClientes.ObtenerClientesAsync(_unitOfWork);
        List<SegmentoListadoDto> items = segmentos.Select(segmento => new SegmentoListadoDto
        {
            Id = segmento.Id,
            Codigo = segmento.Codigo,
            Nombre = segmento.Nombre,
            Prioridad = segmento.Prioridad,
            MontoMinimo = segmento.MontoMinimo,
            OrdenesMinimas = segmento.OrdenesMinimas,
            Activo = segmento.Activo,
            Clientes = segmento.Activo ? clientes.Count(cliente => cliente.SegmentoId == segmento.Id) : 0
        }).ToList();
        return new PaginaSegmentosDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
