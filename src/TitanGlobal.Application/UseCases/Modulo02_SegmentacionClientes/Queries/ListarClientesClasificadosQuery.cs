using MediatR;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes.Queries;

public class ListarClientesClasificadosQuery : IRequest<PaginaClientesSegmentoDto>
{
    public int? SegmentoId { get; set; }

    public bool? SinSegmento { get; set; }

    public string? Busqueda { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarClientesClasificadosQueryHandler : IRequestHandler<ListarClientesClasificadosQuery, PaginaClientesSegmentoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarClientesClasificadosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaClientesSegmentoDto> Handle(ListarClientesClasificadosQuery request, CancellationToken cancellationToken)
    {
        ValidacionSegmentoCliente.ValidarPaginacion(request.Pagina, request.TamanoPagina);
        if (request.SegmentoId.HasValue && request.SinSegmento == true)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "No se puede filtrar por segmento y sin segmento a la vez.");
        }

        List<ClienteClasificadoDto> clientes = await ConsultaSegmentacionClientes.ObtenerClientesAsync(_unitOfWork);
        string busquedaNormalizada = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        IEnumerable<ClienteClasificadoDto> consulta = clientes;
        if (request.SegmentoId.HasValue)
        {
            consulta = consulta.Where(cliente => cliente.SegmentoId == request.SegmentoId.Value);
        }

        if (request.SinSegmento == true)
        {
            consulta = consulta.Where(cliente => cliente.SegmentoId is null);
        }

        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            consulta = consulta.Where(cliente =>
                cliente.Nombre.ToLowerInvariant().Contains(busquedaNormalizada)
                || cliente.Ruc.ToLowerInvariant().Contains(busquedaNormalizada));
        }

        List<ClienteClasificadoDto> filtrados = consulta
            .OrderByDescending(cliente => cliente.MontoTotal)
            .ThenBy(cliente => cliente.ClienteId)
            .ToList();
        return new PaginaClientesSegmentoDto
        {
            Items = filtrados.Skip((request.Pagina - 1) * request.TamanoPagina).Take(request.TamanoPagina).ToList(),
            Total = filtrados.Count,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
