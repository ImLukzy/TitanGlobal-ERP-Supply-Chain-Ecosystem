using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes.Queries;

public class ListarSegmentosClientesQuery : IRequest<PaginaSegmentosDto>
{
    public string? Busqueda { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarSegmentosClientesQueryHandler : IRequestHandler<ListarSegmentosClientesQuery, PaginaSegmentosDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarSegmentosClientesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaSegmentosDto> Handle(ListarSegmentosClientesQuery request, CancellationToken cancellationToken)
    {
        ValidacionSegmentoCliente.ValidarPaginacion(request.Pagina, request.TamanoPagina);
        IQueryable<SegmentoCliente> consulta = _unitOfWork.Repository<SegmentoCliente>().AsQueryable();
        string busquedaNormalizada = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            consulta = consulta.Where(segmento =>
                segmento.Codigo.ToLower().Contains(busquedaNormalizada)
                || segmento.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        if (request.Activo.HasValue)
        {
            consulta = consulta.Where(segmento => segmento.Activo == request.Activo.Value);
        }

        int total = await consulta.CountAsync();
        List<SegmentoCliente> segmentos = await consulta
            .OrderBy(segmento => segmento.Prioridad)
            .ThenBy(segmento => segmento.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
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
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
