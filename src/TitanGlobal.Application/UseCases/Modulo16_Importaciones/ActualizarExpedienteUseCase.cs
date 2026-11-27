using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo16_Importaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo16_Importaciones;

public class ActualizarExpedienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarExpedienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, ActualizarExpedienteRequestDto solicitud)
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

        ValidacionExpedienteImportacion.DatosExpediente datos = ValidacionExpedienteImportacion.NormalizarDatos(
            solicitud.Incoterm,
            solicitud.MedioTransporte,
            solicitud.PaisOrigen,
            solicitud.LugarOrigen,
            solicitud.LugarDestino,
            solicitud.FechaLlegadaEstimada,
            solicitud.Observaciones);

        expediente.Incoterm = datos.Incoterm;
        expediente.MedioTransporte = datos.MedioTransporte;
        expediente.PaisOrigen = datos.PaisOrigen;
        expediente.LugarOrigen = datos.LugarOrigen;
        expediente.LugarDestino = datos.LugarDestino;
        expediente.FechaLlegadaEstimada = datos.FechaLlegadaEstimada;
        expediente.Observaciones = datos.Observaciones;
        await _unitOfWork.CompleteAsync();

        return expediente.Id;
    }
}
