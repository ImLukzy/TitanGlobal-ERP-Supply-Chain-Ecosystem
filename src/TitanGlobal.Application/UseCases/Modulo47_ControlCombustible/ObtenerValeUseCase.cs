using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo47_ControlCombustible;

public class ObtenerValeUseCase
{
    private readonly IUnitOfWork _unidad;

    public ObtenerValeUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ValeDetalleDto> ExecuteAsync(int id)
    {
        ValeCombustible vale = await ValidacionVale.ObtenerAsync(_unidad, id);
        return CalculoVale.Detalle(vale);
    }
}
