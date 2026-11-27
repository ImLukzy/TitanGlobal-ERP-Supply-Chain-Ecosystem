using MediatR;
using TitanGlobal.Application.DTOs.Modulo71_Usuarios;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo71_Usuarios.Queries;

public class ObtenerPermisosUsuarioQuery : IRequest<List<PermisoEfectivoDto>>
{
    public int Id { get; set; }
}

public class ObtenerPermisosUsuarioQueryHandler : IRequestHandler<ObtenerPermisosUsuarioQuery, List<PermisoEfectivoDto>>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerPermisosUsuarioQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<List<PermisoEfectivoDto>> Handle(ObtenerPermisosUsuarioQuery request, CancellationToken cancellationToken)
    {
        Usuario usuario = await ValidacionUsuario.ObtenerAsync(_unidad, request.Id);
        return await OperacionesUsuarios.ObtenerPermisosInternosAsync(_unidad, usuario);
    }
}
