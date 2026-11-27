using MediatR;
using TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.Queries;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.Commands;

public class GuardarPermisosCommand : IRequest<List<PermisoModuloDto>>
{
    public int Id { get; set; }

    public required GuardarPermisosRequestDto Solicitud { get; set; }
}

public class GuardarPermisosCommandHandler : IRequestHandler<GuardarPermisosCommand, List<PermisoModuloDto>>
{
    private readonly IUnitOfWork _unidad;

    private readonly ISender _sender;

    public GuardarPermisosCommandHandler(IUnitOfWork unidad, ISender sender)
    {
        _unidad = unidad;
        _sender = sender;
    }

    public async Task<List<PermisoModuloDto>> Handle(GuardarPermisosCommand request, CancellationToken cancellationToken)
    {
        ValidacionRol.ValidarPermisos(request.Solicitud);
        Rol rol = await ValidacionRol.ObtenerAsync(_unidad, request.Id);
        List<PermisoRol> actuales = await _unidad.Repository<PermisoRol>().AsQueryable()
            .Where(permiso => permiso.RolId == request.Id).ToListAsync();
        if (rol.EsSistema && actuales.Any(actual =>
            !request.Solicitud.Permisos.Any(solicitado => solicitado.ModuloNumero == actual.ModuloNumero
                && (!actual.PuedeLeer || solicitado.PuedeLeer)
                && (!actual.PuedeEscribir || solicitado.PuedeEscribir))))
        {
            throw OperacionesRoles.ConflictoSistema(rol.Codigo);
        }

        foreach (PermisoRol actual in actuales)
        {
            _unidad.Repository<PermisoRol>().Remove(actual);
        }

        foreach (PermisoSolicitadoDto solicitado in request.Solicitud.Permisos)
        {
            if (solicitado.PuedeLeer || solicitado.PuedeEscribir)
            {
                await _unidad.Repository<PermisoRol>().AddAsync(new PermisoRol
                {
                    RolId = request.Id,
                    ModuloNumero = solicitado.ModuloNumero,
                    PuedeLeer = solicitado.PuedeLeer,
                    PuedeEscribir = solicitado.PuedeEscribir
                });
            }
        }

        await _unidad.CompleteAsync();
        return await _sender.Send(new ObtenerPermisosQuery { Id = request.Id }, cancellationToken);
    }
}
