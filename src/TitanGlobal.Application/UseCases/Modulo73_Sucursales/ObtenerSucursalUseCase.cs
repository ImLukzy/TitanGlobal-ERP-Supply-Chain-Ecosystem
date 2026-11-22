using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo73_Sucursales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
namespace TitanGlobal.Application.UseCases.Modulo73_Sucursales;

public class ObtenerSucursalUseCase
{
    private readonly IUnitOfWork _unidad;
    public ObtenerSucursalUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<SucursalDetalleDto> ExecuteAsync(int id)
    {
        Sucursal sucursal = await ValidacionSucursal.BuscarAsync(_unidad, id);
        return PresentacionSucursal.Detalle(sucursal);
    }
}
