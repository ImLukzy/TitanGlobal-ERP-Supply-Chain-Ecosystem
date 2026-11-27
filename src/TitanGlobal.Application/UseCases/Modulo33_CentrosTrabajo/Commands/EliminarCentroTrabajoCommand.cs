using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo32_RutasProduccion;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo.Commands;

public class EliminarCentroTrabajoCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarCentroTrabajoCommandHandler : IRequestHandler<EliminarCentroTrabajoCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarCentroTrabajoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarCentroTrabajoCommand request, CancellationToken cancellationToken)
    {
        CentroTrabajo? centro = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (centro is null)
        {
            throw PersistenciaCentroTrabajo.CrearNoEncontrado(request.Id);
        }

        _unitOfWork.Repository<CentroTrabajo>().Remove(centro);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException error) when (
            error.Tipo == TipoViolacionRestriccion.ReferenciaForanea
            && error.Restriccion == PersistenciaRutaProduccion.ReferenciaCentro)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict,
                $"No se puede eliminar el centro de trabajo {request.Id} porque está en uso en una ruta de producción.");
        }
    }
}
