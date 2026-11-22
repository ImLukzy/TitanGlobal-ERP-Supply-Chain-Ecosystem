using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo73_Sucursales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
namespace TitanGlobal.Application.UseCases.Modulo73_Sucursales;

public class ActualizarSucursalUseCase
{
    private readonly IUnitOfWork _unidad;
    public ActualizarSucursalUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(int id, GuardarSucursalRequestDto datos)
    {
        Sucursal sucursal = await ValidacionSucursal.BuscarAsync(_unidad, id);
        ValidacionSucursal.Validar(datos);
        await ValidacionSucursal.CodigoLibreAsync(_unidad, datos.Codigo, id);
        ValidacionSucursal.Asignar(sucursal, datos);
        await ValidacionSucursal.GuardarAsync(_unidad, datos.Codigo);
        return id;
    }
}
