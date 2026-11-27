using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo16_Importaciones;

public class LiberarExpedienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public LiberarExpedienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        ExpedienteImportacion? expediente = await _unitOfWork.Repository<ExpedienteImportacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (expediente is null)
        {
            throw ValidacionExpedienteImportacion.NoExiste(id);
        }

        if (expediente.Estado != ValidacionExpedienteImportacion.EnAduana)
        {
            throw ValidacionExpedienteImportacion.Conflicto($"El expediente {id} no está en aduana.");
        }

        expediente.Estado = ValidacionExpedienteImportacion.Liberado;
        await _unitOfWork.CompleteAsync();

        return expediente.Id;
    }
}
