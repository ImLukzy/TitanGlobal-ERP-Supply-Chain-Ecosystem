using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo72_RolesPermisos;

public class EliminarRolUseCase
{
    private readonly IUnitOfWork _unidad;

    public EliminarRolUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task EjecutarAsync(int id)
    {
        Rol rol = await ValidacionRol.ObtenerAsync(_unidad, id);
        if (rol.EsSistema)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El rol {rol.Codigo} es de sistema y no se puede modificar de esa forma.");
        }

        bool tieneUsuarios = await _unidad.Repository<UsuarioRol>().AsQueryable()
            .AnyAsync(enlace => enlace.RolId == id);
        if (tieneUsuarios)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"No se puede eliminar el rol {rol.Codigo} porque tiene usuarios.");
        }

        _unidad.Repository<Rol>().Remove(rol);
        await _unidad.CompleteAsync();
    }
}
