using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores;

public class EliminarEvaluacionProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarEvaluacionProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        EvaluacionProveedor? evaluacion = await _unitOfWork.Repository<EvaluacionProveedor>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (evaluacion is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La evaluación {id} no existe.");
        }

        _unitOfWork.Repository<EvaluacionProveedor>().Remove(evaluacion);
        await _unitOfWork.CompleteAsync();
    }
}
