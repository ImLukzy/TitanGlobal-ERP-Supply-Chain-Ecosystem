using MediatR;
using System.Transactions;
using TitanGlobal.Application.DTOs.Modulo71_Usuarios;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo71_Usuarios.Commands;

public class CrearUsuarioCommand : IRequest<int>
{
    public required GuardarUsuarioRequestDto Solicitud { get; set; }
}

public class CrearUsuarioCommandHandler : IRequestHandler<CrearUsuarioCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public CrearUsuarioCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(CrearUsuarioCommand request, CancellationToken cancellationToken)
    {
        (string nombreUsuario, string nombreCompleto, string email) = ValidacionUsuario.Normalizar(request.Solicitud);
        await ValidacionUsuario.ValidarRolesAsync(_unidad, request.Solicitud.RolIds);
        await ValidacionUsuario.ValidarUnicidadAsync(_unidad, nombreUsuario, email, null);
        await OperacionesUsuarios.ValidarAdminPosteriorAsync(_unidad, null, request.Solicitud.Activo, request.Solicitud.RolIds);
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        Usuario usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            NombreCompleto = nombreCompleto,
            Email = email,
            Activo = request.Solicitud.Activo,
            FechaAlta = DateTime.UtcNow
        };
        await _unidad.Repository<Usuario>().AddAsync(usuario);
        try
        {
            await _unidad.CompleteAsync();
            foreach (int rolId in request.Solicitud.RolIds)
            {
                await _unidad.Repository<UsuarioRol>().AddAsync(new UsuarioRol
                {
                    UsuarioId = usuario.Id,
                    RolId = rolId
                });
            }

            await _unidad.CompleteAsync();
            transaccion.Complete();
        }
        catch (ViolacionRestriccionException excepcion) when (OperacionesUsuarios.EsDuplicado(excepcion))
        {
            throw OperacionesUsuarios.CrearConflictoDuplicado(excepcion, nombreUsuario, email);
        }

        return usuario.Id;
    }
}
