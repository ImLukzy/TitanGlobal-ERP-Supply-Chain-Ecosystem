using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo73_Sucursales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
namespace TitanGlobal.Application.UseCases.Modulo73_Sucursales;

public class CrearSucursalUseCase
{
    private readonly IUnitOfWork _unidad;
    public CrearSucursalUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(GuardarSucursalRequestDto datos)
    {
        ValidacionSucursal.Validar(datos);
        await ValidacionSucursal.CodigoLibreAsync(_unidad, datos.Codigo);
        Sucursal sucursal = new Sucursal { Activa = true, FechaRegistro = FechasUtc.Normalizar(DateTime.UtcNow) };
        ValidacionSucursal.Asignar(sucursal, datos);
        await _unidad.Repository<Sucursal>().AddAsync(sucursal);
        await ValidacionSucursal.GuardarAsync(_unidad, datos.Codigo);
        return sucursal.Id;
    }
}
