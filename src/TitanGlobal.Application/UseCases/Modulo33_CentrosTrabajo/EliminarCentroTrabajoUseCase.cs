using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo;

public class EliminarCentroTrabajoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarCentroTrabajoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        CentroTrabajo? centro = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (centro is null)
        {
            throw PersistenciaCentroTrabajo.CrearNoEncontrado(id);
        }

        bool tieneMantenimientos = await _unitOfWork.Repository<MantenimientoPreventivo>()
            .AsQueryable()
            .AnyAsync(mantenimiento => mantenimiento.CentroTrabajoId == id);
        if (tieneMantenimientos)
        {
            throw ConMantenimientos(id);
        }

        _unitOfWork.Repository<CentroTrabajo>().Remove(centro);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException error) when (
            error.Tipo == TipoViolacionRestriccion.ReferenciaForanea
            && error.Restriccion == PersistenciaMantenimientoPreventivo.ReferenciaCentro)
        {
            throw ConMantenimientos(id);
        }
    }

    private static ReglaNegocioException ConMantenimientos(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict,
            $"No se puede eliminar el centro de trabajo {id} porque tiene mantenimientos preventivos.");
    }
}
