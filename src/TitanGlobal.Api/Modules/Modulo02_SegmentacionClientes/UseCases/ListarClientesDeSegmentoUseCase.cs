using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.UseCases;

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
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El segmento {id} no existe.");
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
