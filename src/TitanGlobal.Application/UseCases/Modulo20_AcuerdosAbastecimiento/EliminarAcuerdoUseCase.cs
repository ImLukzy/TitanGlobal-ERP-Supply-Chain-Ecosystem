using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class EliminarAcuerdoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarAcuerdoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        AcuerdoAbastecimiento? acuerdo = await _unitOfWork.Repository<AcuerdoAbastecimiento>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (acuerdo is null)
        {
            throw ValidacionAcuerdoAbastecimiento.NoExiste(id);
        }

        List<LineaAcuerdoAbastecimiento> lineas = await _unitOfWork.Repository<LineaAcuerdoAbastecimiento>()
            .AsQueryable()
            .Where(linea => linea.AcuerdoId == id)
            .ToListAsync();
        bool tieneConsumos = lineas.Any(linea => linea.CantidadConsumida > 0);
        if (tieneConsumos)
        {
            throw ValidacionAcuerdoAbastecimiento.Conflicto(
                $"El acuerdo {id} tiene consumos y no se puede eliminar; cancélelo.");
        }

        foreach (LineaAcuerdoAbastecimiento linea in lineas)
        {
            _unitOfWork.Repository<LineaAcuerdoAbastecimiento>().Remove(linea);
        }

        _unitOfWork.Repository<AcuerdoAbastecimiento>().Remove(acuerdo);
        await _unitOfWork.CompleteAsync();
    }
}
