using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class SuspenderAcuerdoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public SuspenderAcuerdoUseCase(IUnitOfWork unitOfWork)
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

        if (acuerdo.Estado != ValidacionAcuerdoAbastecimiento.Vigente)
        {
            throw ValidacionAcuerdoAbastecimiento.NoVigente(id);
        }

        acuerdo.Estado = ValidacionAcuerdoAbastecimiento.Suspendido;
        await _unitOfWork.CompleteAsync();

        return acuerdo.Id;
    }
}
