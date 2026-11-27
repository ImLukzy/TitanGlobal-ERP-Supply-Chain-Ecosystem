using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo16_Importaciones;

public class EliminarExpedienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarExpedienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        ExpedienteImportacion? expediente = await _unitOfWork.Repository<ExpedienteImportacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (expediente is null)
        {
            throw ValidacionExpedienteImportacion.NoExiste(id);
        }

        if (expediente.Estado != ValidacionExpedienteImportacion.Programado)
        {
            throw ValidacionExpedienteImportacion.Conflicto(
                $"El expediente {id} no está programado y no se puede eliminar; cancélelo.");
        }

        _unitOfWork.Repository<ExpedienteImportacion>().Remove(expediente);
        await _unitOfWork.CompleteAsync();
    }
}
