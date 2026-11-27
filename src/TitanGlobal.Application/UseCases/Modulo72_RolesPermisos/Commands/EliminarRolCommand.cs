using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.Commands;

public class EliminarRolCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarRolCommandHandler : IRequestHandler<EliminarRolCommand>
{
    private readonly IUnitOfWork _unidad;

    public EliminarRolCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task Handle(EliminarRolCommand request, CancellationToken cancellationToken)
    {
        Rol rol = await ValidacionRol.ObtenerAsync(_unidad, request.Id);
        if (rol.EsSistema)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El rol {rol.Codigo} es de sistema y no se puede modificar de esa forma.");
        }

        bool tieneUsuarios = await _unidad.Repository<UsuarioRol>().AsQueryable()
            .AnyAsync(enlace => enlace.RolId == request.Id);
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
