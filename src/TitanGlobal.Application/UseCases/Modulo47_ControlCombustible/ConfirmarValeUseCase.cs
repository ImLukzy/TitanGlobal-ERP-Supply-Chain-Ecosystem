using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo47_ControlCombustible;

public class ConfirmarValeUseCase
{
    private readonly IUnitOfWork _unidad;

    public ConfirmarValeUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        ValeCombustible vale = await ValidacionVale.ObtenerAsync(_unidad, id);
        ValidacionVale.ExigirBorrador(vale, "confirmar");
        if (!vale.OdometroFinal.HasValue)
        {
            throw ValidacionVale.Conflicto("Registra el odómetro final antes de confirmar el vale.");
        }
        vale.Estado = "Confirmado";
        vale.FechaConfirmacion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unidad.Repository<ValeCombustible>().Update(vale);
        await _unidad.CompleteAsync();
        return id;
    }
}
