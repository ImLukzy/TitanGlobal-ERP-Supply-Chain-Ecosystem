using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

public class CancelarFleteTerceroUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelarFleteTerceroUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, CancelarFleteTerceroRequestDto solicitud)
    {
        FleteTercero? flete = await _unitOfWork.Repository<FleteTercero>().AsQueryable().FirstOrDefaultAsync(f => f.Id == id);
        if (flete is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El flete {id} no existe."); }
        if (flete.Estado == "Cancelado") { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, $"El flete {id} ya está cancelado."); }
        string motivo = ValidacionLogisticaExterna.Obligatorio(solicitud.Motivo, 500, "El motivo de cancelación es obligatorio.", "El motivo de cancelación no puede superar 500 caracteres.");
        flete.Estado = "Cancelado";
        flete.MotivoCancelacion = motivo;
        flete.FechaCancelacion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<FleteTercero>().Update(flete);
        await _unitOfWork.CompleteAsync();
        return id;
    }
}
