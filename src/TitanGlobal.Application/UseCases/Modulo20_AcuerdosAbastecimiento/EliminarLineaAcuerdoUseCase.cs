using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class EliminarLineaAcuerdoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarLineaAcuerdoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int acuerdoId, int lineaId)
    {
        AcuerdoAbastecimiento? acuerdo = await _unitOfWork.Repository<AcuerdoAbastecimiento>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == acuerdoId);
        if (acuerdo is null)
        {
            throw ValidacionAcuerdoAbastecimiento.NoExiste(acuerdoId);
        }

        LineaAcuerdoAbastecimiento? linea = await _unitOfWork.Repository<LineaAcuerdoAbastecimiento>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == lineaId && entidad.AcuerdoId == acuerdoId);
        if (linea is null)
        {
            throw ValidacionAcuerdoAbastecimiento.LineaNoExiste(acuerdoId, lineaId);
        }

        if (acuerdo.Estado == ValidacionAcuerdoAbastecimiento.Cancelado)
        {
            throw ValidacionAcuerdoAbastecimiento.EstaCancelado(acuerdoId);
        }

        if (linea.CantidadConsumida > 0)
        {
            throw ValidacionAcuerdoAbastecimiento.Conflicto(
                $"La línea {lineaId} tiene consumos y no se puede eliminar.");
        }

        _unitOfWork.Repository<LineaAcuerdoAbastecimiento>().Remove(linea);
        await _unitOfWork.CompleteAsync();
    }
}
