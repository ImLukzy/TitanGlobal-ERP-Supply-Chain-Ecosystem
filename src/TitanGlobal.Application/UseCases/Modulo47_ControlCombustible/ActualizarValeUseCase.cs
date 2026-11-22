using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo47_ControlCombustible;

public class ActualizarValeUseCase
{
    private readonly IUnitOfWork _unidad;

    public ActualizarValeUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(int id, GuardarValeRequestDto solicitud)
    {
        ValeCombustible vale = await ValidacionVale.ObtenerAsync(_unidad, id);
        ValidacionVale.ExigirBorrador(vale, "editar");
        GuardarValeRequestDto datos = ValidacionVale.Normalizar(solicitud);
        await ValidacionVale.ExigirCodigoLibreAsync(_unidad, datos.Codigo, id);
        ValidacionVale.Asignar(vale, datos);
        _unidad.Repository<ValeCombustible>().Update(vale);
        await ValidacionVale.GuardarAsync(_unidad, datos.Codigo);
        return id;
    }
}
