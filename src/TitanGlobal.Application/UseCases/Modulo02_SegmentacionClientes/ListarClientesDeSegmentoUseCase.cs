using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes;

public class ListarClientesDeSegmentoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarClientesDeSegmentoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaClientesSegmentoDto> ExecuteAsync(int id, int pagina, int tamanoPagina)
    {
        ValidacionSegmentoCliente.ValidarPaginacion(pagina, tamanoPagina);
        bool existe = await _unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .AnyAsync(segmento => segmento.Id == id);
        if (!existe)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El segmento {id} no existe.");
        }

        List<ClienteClasificadoDto> clientes = await ConsultaSegmentacionClientes.ObtenerClientesAsync(_unitOfWork);
        List<ClienteClasificadoDto> asignados = clientes
            .Where(cliente => cliente.SegmentoId == id)
            .OrderByDescending(cliente => cliente.MontoTotal)
            .ThenBy(cliente => cliente.ClienteId)
            .ToList();
        return new PaginaClientesSegmentoDto
        {
            Items = asignados.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList(),
            Total = asignados.Count,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
