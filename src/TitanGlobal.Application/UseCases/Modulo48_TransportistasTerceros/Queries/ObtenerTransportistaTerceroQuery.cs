using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Queries;

public class ObtenerTransportistaTerceroQuery : IRequest<TransportistaTerceroDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerTransportistaTerceroQueryHandler : IRequestHandler<ObtenerTransportistaTerceroQuery, TransportistaTerceroDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerTransportistaTerceroQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TransportistaTerceroDetalleDto> Handle(ObtenerTransportistaTerceroQuery request, CancellationToken cancellationToken)
    {
        TransportistaTerceroDetalleDto? detalle = await _unitOfWork.Repository<TransportistaTercero>().AsQueryable().Where(t => t.Id == request.Id)
            .Select(t => new TransportistaTerceroDetalleDto
            {
                Id = t.Id,
                RazonSocial = t.RazonSocial,
                DocumentoFiscal = t.DocumentoFiscal,
                Pais = t.Pais,
                TipoServicio = t.TipoServicio,
                Contacto = t.Contacto,
                Email = t.Email,
                Telefono = t.Telefono,
                Observaciones = t.Observaciones,
                Activo = t.Activo
            }).FirstOrDefaultAsync();
        if (detalle is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El transportista {request.Id} no existe."); }
        return detalle;
    }
}
