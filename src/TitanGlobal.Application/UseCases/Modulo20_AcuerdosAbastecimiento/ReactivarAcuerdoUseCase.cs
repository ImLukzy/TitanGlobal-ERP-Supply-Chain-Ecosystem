using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class ReactivarAcuerdoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ReactivarAcuerdoUseCase(IUnitOfWork unitOfWork)
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

        if (acuerdo.Estado != ValidacionAcuerdoAbastecimiento.Suspendido)
        {
            throw ValidacionAcuerdoAbastecimiento.Conflicto($"El acuerdo {id} no está suspendido.");
        }

        acuerdo.Estado = ValidacionAcuerdoAbastecimiento.Vigente;
        await _unitOfWork.CompleteAsync();

        return acuerdo.Id;
    }
}
