using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo71_Usuarios;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo71_Usuarios.Commands;

public class ActualizarUsuarioCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarUsuarioRequestDto Solicitud { get; set; }
}

public class ActualizarUsuarioCommandHandler : IRequestHandler<ActualizarUsuarioCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public ActualizarUsuarioCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(ActualizarUsuarioCommand request, CancellationToken cancellationToken)
    {
        (string nombreUsuario, string nombreCompleto, string email) = ValidacionUsuario.Normalizar(request.Solicitud);
        await ValidacionUsuario.ValidarRolesAsync(_unidad, request.Solicitud.RolIds);
        await ValidacionUsuario.ValidarUnicidadAsync(_unidad, nombreUsuario, email, request.Id);
        Usuario usuario = await ValidacionUsuario.ObtenerAsync(_unidad, request.Id);
        await OperacionesUsuarios.ValidarAdminPosteriorAsync(_unidad, request.Id, request.Solicitud.Activo, request.Solicitud.RolIds);
        List<UsuarioRol> rolesActuales = await _unidad.Repository<UsuarioRol>().AsQueryable()
            .Where(enlace => enlace.UsuarioId == request.Id)
            .ToListAsync();
        usuario.NombreUsuario = nombreUsuario;
        usuario.NombreCompleto = nombreCompleto;
        usuario.Email = email;
        usuario.Activo = request.Solicitud.Activo;
        _unidad.Repository<Usuario>().Update(usuario);
        foreach (UsuarioRol enlace in rolesActuales)
        {
            _unidad.Repository<UsuarioRol>().Remove(enlace);
        }

        foreach (int rolId in request.Solicitud.RolIds)
        {
            await _unidad.Repository<UsuarioRol>().AddAsync(new UsuarioRol { UsuarioId = request.Id, RolId = rolId });
        }

        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (OperacionesUsuarios.EsDuplicado(excepcion))
        {
            throw OperacionesUsuarios.CrearConflictoDuplicado(excepcion, nombreUsuario, email);
        }

        return usuario.Id;
    }
}
