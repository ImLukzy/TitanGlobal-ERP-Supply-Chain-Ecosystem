using MediatR;
using TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.Commands;

public class ActualizarRolCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarRolRequestDto Solicitud { get; set; }
}

public class ActualizarRolCommandHandler : IRequestHandler<ActualizarRolCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public ActualizarRolCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(ActualizarRolCommand request, CancellationToken cancellationToken)
    {
        (string codigo, string nombre, string? descripcion) = ValidacionRol.Normalizar(request.Solicitud);
        Rol rol = await ValidacionRol.ObtenerAsync(_unidad, request.Id);
        await ValidacionRol.ValidarCodigoUnicoAsync(_unidad, codigo, request.Id);
        if (rol.EsSistema && rol.Codigo != codigo)
        {
            throw OperacionesRoles.ConflictoSistema(rol.Codigo);
        }

        rol.Codigo = codigo;
        rol.Nombre = nombre;
        rol.Descripcion = descripcion;
        _unidad.Repository<Rol>().Update(rol);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (OperacionesRoles.EsCodigoDuplicado(excepcion))
        {
            throw OperacionesRoles.ConflictoCodigo(codigo);
        }

        return rol.Id;
    }
}
