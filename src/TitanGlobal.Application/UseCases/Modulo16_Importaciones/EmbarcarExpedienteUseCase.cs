using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo16_Importaciones;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo16_Importaciones;

public class EmbarcarExpedienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EmbarcarExpedienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, EmbarcarExpedienteRequestDto solicitud)
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
            throw ValidacionExpedienteImportacion.NoProgramado(id);
        }

        string documento = solicitud.DocumentoTransporte?.Trim() ?? string.Empty;
        if (documento.Length == 0)
        {
            throw ValidacionExpedienteImportacion.Error("El documento de transporte es obligatorio.");
        }

        if (documento.Length > ValidacionExpedienteImportacion.MaximoDocumento)
        {
            throw ValidacionExpedienteImportacion.Error(
                $"El documento de transporte no puede superar {ValidacionExpedienteImportacion.MaximoDocumento} caracteres.");
        }

        DateTime fechaEmbarque = FechasUtc.Normalizar(solicitud.FechaEmbarque);
        if (fechaEmbarque > DateTime.UtcNow)
        {
            throw ValidacionExpedienteImportacion.Error("La fecha de embarque no puede ser futura.");
        }

        expediente.DocumentoTransporte = documento;
        expediente.FechaEmbarque = fechaEmbarque;
        expediente.Estado = ValidacionExpedienteImportacion.EnTransito;
        await _unitOfWork.CompleteAsync();

        return expediente.Id;
    }
}
