using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo73_Sucursales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
namespace TitanGlobal.Application.UseCases.Modulo73_Sucursales;

public class DesactivarSucursalUseCase
{
    private readonly IUnitOfWork _unidad;
    public DesactivarSucursalUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(int id)
    {
        return await ValidacionSucursal.CambiarEstadoAsync(_unidad, id, false);
    }
}
