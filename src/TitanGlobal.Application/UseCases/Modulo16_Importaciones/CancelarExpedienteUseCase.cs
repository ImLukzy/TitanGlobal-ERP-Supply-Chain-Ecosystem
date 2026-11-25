using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo16_Importaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo16_Importaciones;

public class CancelarExpedienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelarExpedienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, CancelarExpedienteRequestDto solicitud)
    {
        ExpedienteImportacion? expediente = await _unitOfWork.Repository<ExpedienteImportacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (expediente is null)
        {
            throw ValidacionExpedienteImportacion.NoExiste(id);
        }

        string motivo = solicitud.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length == 0)
        {
            throw ValidacionExpedienteImportacion.Error("El motivo de cancelación es obligatorio.");
        }

        if (motivo.Length > ValidacionExpedienteImportacion.MaximoTexto)
        {
            throw ValidacionExpedienteImportacion.Error(
                $"El motivo no puede superar {ValidacionExpedienteImportacion.MaximoTexto} caracteres.");
        }

        if (expediente.Estado == ValidacionExpedienteImportacion.Liberado
            || expediente.Estado == ValidacionExpedienteImportacion.Cancelado)
        {
            throw ValidacionExpedienteImportacion.Conflicto(
                $"El expediente {id} no se puede cancelar porque está {expediente.Estado}.");
        }

        expediente.MotivoCancelacion = motivo;
        expediente.Estado = ValidacionExpedienteImportacion.Cancelado;
        await _unitOfWork.CompleteAsync();

        return expediente.Id;
    }
}
