using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class CancelarAcuerdoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelarAcuerdoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        AcuerdoAbastecimiento? acuerdo = await _unitOfWork.Repository<AcuerdoAbastecimiento>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (acuerdo is null)
        {
            throw ValidacionAcuerdoAbastecimiento.NoExiste(id);
        }

        if (acuerdo.Estado == ValidacionAcuerdoAbastecimiento.Cancelado)
        {
            throw ValidacionAcuerdoAbastecimiento.Conflicto($"El acuerdo {id} ya está cancelado.");
        }

        acuerdo.Estado = ValidacionAcuerdoAbastecimiento.Cancelado;
        await _unitOfWork.CompleteAsync();

        return acuerdo.Id;
    }
}
