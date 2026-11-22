using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo47_ControlCombustible;

public class EliminarValeUseCase
{
    private readonly IUnitOfWork _unidad;

    public EliminarValeUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task ExecuteAsync(int id)
    {
        ValeCombustible vale = await ValidacionVale.ObtenerAsync(_unidad, id);
        ValidacionVale.ExigirBorrador(vale, "eliminar");
        _unidad.Repository<ValeCombustible>().Remove(vale);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw ValidacionVale.Conflicto("No se puede eliminar el vale porque está en uso.");
        }
    }
}
