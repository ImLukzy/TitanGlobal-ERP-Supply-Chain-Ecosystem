using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo47_ControlCombustible;

public class AnularValeUseCase
{
    private readonly IUnitOfWork _unidad;

    public AnularValeUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(int id, AnularValeRequestDto solicitud)
    {
        ValeCombustible vale = await ValidacionVale.ObtenerAsync(_unidad, id);
        if (vale.Estado == "Anulado")
        {
            throw ValidacionVale.Conflicto("El vale ya está anulado.");
        }
        string motivo = solicitud.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length == 0)
        {
            throw ValidacionVale.Error("El motivo de anulación es obligatorio.");
        }
        if (motivo.Length > 500)
        {
            throw ValidacionVale.Error("El motivo de anulación no puede superar 500 caracteres.");
        }
        vale.Estado = "Anulado";
        vale.FechaAnulacion = FechasUtc.Normalizar(DateTime.UtcNow);
        vale.MotivoAnulacion = motivo;
        _unidad.Repository<ValeCombustible>().Update(vale);
        await _unidad.CompleteAsync();
        return id;
    }
}
