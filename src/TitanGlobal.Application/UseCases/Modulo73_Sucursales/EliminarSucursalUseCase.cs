using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo73_Sucursales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
namespace TitanGlobal.Application.UseCases.Modulo73_Sucursales;

public class EliminarSucursalUseCase
{
    private readonly IUnitOfWork _unidad;
    public EliminarSucursalUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task ExecuteAsync(int id)
    {
        Sucursal sucursal = await ValidacionSucursal.BuscarAsync(_unidad, id);
        if (sucursal.Activa)
        {
            throw ValidacionSucursal.Conflicto("Desactiva la sucursal antes de eliminarla.");
        }
        _unidad.Repository<Sucursal>().Remove(sucursal);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw ValidacionSucursal.Conflicto("No se puede eliminar la sucursal porque está en uso.");
        }
    }
}
