using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes.Queries;

public class ListarClientesDeSegmentoQuery : IRequest<PaginaClientesSegmentoDto>
{
    public int Id { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarClientesDeSegmentoQueryHandler : IRequestHandler<ListarClientesDeSegmentoQuery, PaginaClientesSegmentoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarClientesDeSegmentoQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaClientesSegmentoDto> Handle(ListarClientesDeSegmentoQuery request, CancellationToken cancellationToken)
    {
        ValidacionSegmentoCliente.ValidarPaginacion(request.Pagina, request.TamanoPagina);
        bool existe = await _unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .AnyAsync(segmento => segmento.Id == request.Id);
        if (!existe)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El segmento {request.Id} no existe.");
        }

        List<ClienteClasificadoDto> clientes = await ConsultaSegmentacionClientes.ObtenerClientesAsync(_unitOfWork);
        List<ClienteClasificadoDto> asignados = clientes
            .Where(cliente => cliente.SegmentoId == request.Id)
            .OrderByDescending(cliente => cliente.MontoTotal)
            .ThenBy(cliente => cliente.ClienteId)
            .ToList();
        return new PaginaClientesSegmentoDto
        {
            Items = asignados.Skip((request.Pagina - 1) * request.TamanoPagina).Take(request.TamanoPagina).ToList(),
            Total = asignados.Count,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
