using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes.Queries;

public class ObtenerSegmentoClienteQuery : IRequest<SegmentoDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerSegmentoClienteQueryHandler : IRequestHandler<ObtenerSegmentoClienteQuery, SegmentoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerSegmentoClienteQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SegmentoDetalleDto> Handle(ObtenerSegmentoClienteQuery request, CancellationToken cancellationToken)
    {
        SegmentoCliente? segmento = await _unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (segmento is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El segmento {request.Id} no existe.");
        }

        List<ClienteClasificadoDto> clientes = await ConsultaSegmentacionClientes.ObtenerClientesAsync(_unitOfWork);
        int asignados = clientes.Count(cliente => cliente.SegmentoId == request.Id);
        return new SegmentoDetalleDto
        {
            Id = segmento.Id,
            Codigo = segmento.Codigo,
            Nombre = segmento.Nombre,
            Prioridad = segmento.Prioridad,
            MontoMinimo = segmento.MontoMinimo,
            OrdenesMinimas = segmento.OrdenesMinimas,
            Activo = segmento.Activo,
            Clientes = segmento.Activo ? asignados : 0,
            Descripcion = segmento.Descripcion
        };
    }
}
