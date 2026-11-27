using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Commands;

public class CrearTransportistaTerceroCommand : IRequest<int>
{
    public required GuardarTransportistaTerceroRequestDto Solicitud { get; set; }
}

public class CrearTransportistaTerceroCommandHandler : IRequestHandler<CrearTransportistaTerceroCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearTransportistaTerceroCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearTransportistaTerceroCommand request, CancellationToken cancellationToken)
    {
        GuardarTransportistaTerceroRequestDto datos = ValidacionLogisticaExterna.Transportista(request.Solicitud);
        string paisNormalizado = datos.Pais.ToLowerInvariant();
        string documentoNormalizado = datos.DocumentoFiscal.ToLowerInvariant();
        bool existe = await _unitOfWork.Repository<TransportistaTercero>().AsQueryable()
            .AnyAsync(t => t.Pais.Trim().ToLower() == paisNormalizado && t.DocumentoFiscal.Trim().ToLower() == documentoNormalizado);
        if (existe) { throw ValidacionLogisticaExterna.DocumentoDuplicado(datos.DocumentoFiscal, datos.Pais); }
        TransportistaTercero entidad = new TransportistaTercero
        {
            RazonSocial = datos.RazonSocial,
            DocumentoFiscal = datos.DocumentoFiscal,
            Pais = datos.Pais,
            TipoServicio = datos.TipoServicio,
            Contacto = datos.Contacto,
            Email = datos.Email,
            Telefono = datos.Telefono,
            Observaciones = datos.Observaciones,
            Activo = datos.Activo
        };
        await _unitOfWork.Repository<TransportistaTercero>().AddAsync(entidad);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw ValidacionLogisticaExterna.DocumentoDuplicado(datos.DocumentoFiscal, datos.Pais);
        }
        return entidad.Id;
    }
}
