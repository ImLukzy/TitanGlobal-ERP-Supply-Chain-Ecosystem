using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo16_Importaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo16_Importaciones;

public class ArribarExpedienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ArribarExpedienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, ArribarExpedienteRequestDto solicitud)
    {
        ExpedienteImportacion? expediente = await _unitOfWork.Repository<ExpedienteImportacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (expediente is null)
        {
            throw ValidacionExpedienteImportacion.NoExiste(id);
        }

        if (expediente.Estado != ValidacionExpedienteImportacion.EnTransito)
        {
            throw ValidacionExpedienteImportacion.Conflicto($"El expediente {id} no está en tránsito.");
        }

        DateOnly fechaLlegada = solicitud.FechaLlegadaReal;
        if (fechaLlegada > ValidacionExpedienteImportacion.Hoy())
        {
            throw ValidacionExpedienteImportacion.Error("La fecha de llegada no puede ser futura.");
        }

        if (expediente.FechaEmbarque is not null && fechaLlegada < expediente.FechaEmbarque.Value)
        {
            throw ValidacionExpedienteImportacion.Error("La fecha de llegada no puede ser anterior al embarque.");
        }

        expediente.FechaLlegadaReal = fechaLlegada;
        expediente.Estado = ValidacionExpedienteImportacion.EnAduana;
        await _unitOfWork.CompleteAsync();

        return expediente.Id;
    }
}
