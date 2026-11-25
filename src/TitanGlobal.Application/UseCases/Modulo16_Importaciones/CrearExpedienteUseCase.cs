using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo16_Importaciones;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo16_Importaciones;

public class CrearExpedienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearExpedienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(CrearExpedienteRequestDto solicitud)
    {
        string numero = ValidacionExpedienteImportacion.NormalizarNumero(solicitud.Numero);
        ValidacionExpedienteImportacion.DatosExpediente datos = ValidacionExpedienteImportacion.NormalizarDatos(
            solicitud.Incoterm,
            solicitud.MedioTransporte,
            solicitud.PaisOrigen,
            solicitud.LugarOrigen,
            solicitud.LugarDestino,
            solicitud.FechaLlegadaEstimada,
            solicitud.Observaciones);
        await ValidarOrdenAsync(solicitud.OrdenAbastecimientoId);

        bool numeroExiste = await _unitOfWork.Repository<ExpedienteImportacion>()
            .AsQueryable()
            .AnyAsync(expediente => expediente.Numero == numero);
        if (numeroExiste)
        {
            throw CrearConflictoNumero(numero);
        }

        ExpedienteImportacion expediente = new ExpedienteImportacion
        {
            Numero = numero,
            OrdenAbastecimientoId = solicitud.OrdenAbastecimientoId,
            Incoterm = datos.Incoterm,
            MedioTransporte = datos.MedioTransporte,
            PaisOrigen = datos.PaisOrigen,
            LugarOrigen = datos.LugarOrigen,
            LugarDestino = datos.LugarDestino,
            FechaLlegadaEstimada = datos.FechaLlegadaEstimada,
            Estado = ValidacionExpedienteImportacion.Programado,
            Observaciones = datos.Observaciones,
            FechaRegistro = DateTime.UtcNow
        };
        await _unitOfWork.Repository<ExpedienteImportacion>().AddAsync(expediente);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoNumero(numero);
        }

        return expediente.Id;
    }

    private async Task ValidarOrdenAsync(int ordenId)
    {
        if (ordenId <= 0)
        {
            throw ValidacionExpedienteImportacion.Error("La orden de abastecimiento es obligatoria.");
        }

        string? estadoOrden = await _unitOfWork.Repository<OrdenAbastecimiento>()
            .AsQueryable()
            .Where(orden => orden.Id == ordenId)
            .Select(orden => orden.Estado)
            .SingleOrDefaultAsync();
        if (estadoOrden is null)
        {
            throw ValidacionExpedienteImportacion.Error($"La orden de abastecimiento {ordenId} no existe.");
        }

        if (estadoOrden == ValidacionExpedienteImportacion.OrdenAnulada)
        {
            throw ValidacionExpedienteImportacion.Conflicto($"La orden de abastecimiento {ordenId} está anulada.");
        }
    }

    private static ReglaNegocioException CrearConflictoNumero(string numero)
    {
        return ValidacionExpedienteImportacion.Conflicto($"Ya existe un expediente con el número {numero}.");
    }
}
