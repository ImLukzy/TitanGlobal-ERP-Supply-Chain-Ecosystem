using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros.Commands;

public class RegistrarFleteTerceroCommand : IRequest<int>
{
    public required RegistrarFleteTerceroRequestDto Solicitud { get; set; }
}

public class RegistrarFleteTerceroCommandHandler : IRequestHandler<RegistrarFleteTerceroCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarFleteTerceroCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(RegistrarFleteTerceroCommand request, CancellationToken cancellationToken)
    {
        RegistrarFleteTerceroRequestDto datos = ValidacionLogisticaExterna.Flete(request.Solicitud);
        TransportistaTercero? transportista = await _unitOfWork.Repository<TransportistaTercero>().AsQueryable().FirstOrDefaultAsync(t => t.Id == datos.TransportistaId);
        if (transportista is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El transportista {datos.TransportistaId} no existe."); }
        if (!transportista.Activo) { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se pueden registrar fletes para un transportista inactivo."); }
        string referenciaNormalizada = datos.Referencia.ToLowerInvariant();
        if (await _unitOfWork.Repository<FleteTercero>().AsQueryable().AnyAsync(f => f.TransportistaTerceroId == datos.TransportistaId && f.Referencia.Trim().ToLower() == referenciaNormalizada))
        {
            throw ValidacionLogisticaExterna.ReferenciaDuplicada(datos.Referencia);
        }
        FleteTercero entidad = new FleteTercero
        {
            TransportistaTerceroId = datos.TransportistaId,
            Referencia = datos.Referencia,
            Fecha = datos.Fecha,
            Origen = datos.Origen,
            Destino = datos.Destino,
            Importe = datos.Importe,
            Moneda = datos.Moneda,
            Estado = "Registrado",
            Observaciones = datos.Observaciones
        };
        await _unitOfWork.Repository<FleteTercero>().AddAsync(entidad);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw ValidacionLogisticaExterna.ReferenciaDuplicada(datos.Referencia);
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "El transportista indicado ya no está disponible.");
        }
        return entidad.Id;
    }
}
