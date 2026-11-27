using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo71_Usuarios.Commands;

public class EliminarUsuarioCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarUsuarioCommandHandler : IRequestHandler<EliminarUsuarioCommand>
{
    private readonly IUnitOfWork _unidad;

    public EliminarUsuarioCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task Handle(EliminarUsuarioCommand request, CancellationToken cancellationToken)
    {
        Usuario usuario = await ValidacionUsuario.ObtenerAsync(_unidad, request.Id);
        List<int> rolIds = await _unidad.Repository<UsuarioRol>().AsQueryable()
            .Where(enlace => enlace.UsuarioId == request.Id)
            .Select(enlace => enlace.RolId)
            .ToListAsync();
        await OperacionesUsuarios.ValidarAdminPosteriorAsync(_unidad, request.Id, false, rolIds);
        _unidad.Repository<Usuario>().Remove(usuario);
        await _unidad.CompleteAsync();
    }
}
