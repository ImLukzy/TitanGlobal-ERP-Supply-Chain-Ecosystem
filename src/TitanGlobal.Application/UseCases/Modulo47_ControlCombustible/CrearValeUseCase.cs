using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo47_ControlCombustible;

public class CrearValeUseCase
{
    private readonly IUnitOfWork _unidad;

    public CrearValeUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(GuardarValeRequestDto solicitud)
    {
        GuardarValeRequestDto datos = ValidacionVale.Normalizar(solicitud);
        await ValidacionVale.ExigirCodigoLibreAsync(_unidad, datos.Codigo);
        ValeCombustible vale = new ValeCombustible { Estado = "Borrador" };
        ValidacionVale.Asignar(vale, datos);
        await _unidad.Repository<ValeCombustible>().AddAsync(vale);
        await ValidacionVale.GuardarAsync(_unidad, datos.Codigo);
        return vale.Id;
    }
}
