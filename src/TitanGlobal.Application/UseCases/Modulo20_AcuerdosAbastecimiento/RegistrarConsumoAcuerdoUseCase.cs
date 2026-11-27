using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class RegistrarConsumoAcuerdoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarConsumoAcuerdoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int acuerdoId, int lineaId, RegistrarConsumoRequestDto solicitud)
    {
        AcuerdoAbastecimiento? acuerdo = await _unitOfWork.Repository<AcuerdoAbastecimiento>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == acuerdoId);
        if (acuerdo is null)
        {
            throw ValidacionAcuerdoAbastecimiento.NoExiste(acuerdoId);
        }

        LineaAcuerdoAbastecimiento? linea = await _unitOfWork.Repository<LineaAcuerdoAbastecimiento>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == lineaId && entidad.AcuerdoId == acuerdoId);
        if (linea is null)
        {
            throw ValidacionAcuerdoAbastecimiento.LineaNoExiste(acuerdoId, lineaId);
        }

        if (acuerdo.Estado != ValidacionAcuerdoAbastecimiento.Vigente)
        {
            throw ValidacionAcuerdoAbastecimiento.NoVigente(acuerdoId);
        }

        DateTime ahora = DateTime.UtcNow;
        if (acuerdo.FechaInicio > ahora)
        {
            throw ValidacionAcuerdoAbastecimiento.Conflicto($"El acuerdo {acuerdoId} aún no inicia.");
        }

        if (acuerdo.FechaFin <= ahora)
        {
            throw ValidacionAcuerdoAbastecimiento.Conflicto($"El acuerdo {acuerdoId} ya venció.");
        }

        if (solicitud.Cantidad <= 0)
        {
            throw ValidacionAcuerdoAbastecimiento.Error("La cantidad debe ser mayor a 0.");
        }

        int saldo = linea.CantidadComprometida - linea.CantidadConsumida;
        if (solicitud.Cantidad > saldo)
        {
            throw ValidacionAcuerdoAbastecimiento.Conflicto(
                $"La cantidad supera el saldo de la línea ({saldo}).");
        }

        linea.CantidadConsumida += solicitud.Cantidad;
        await _unitOfWork.CompleteAsync();

        return linea.Id;
    }
}
