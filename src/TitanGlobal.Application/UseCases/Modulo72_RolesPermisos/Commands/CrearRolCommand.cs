using MediatR;
using TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo72_RolesPermisos.Commands;

public class CrearRolCommand : IRequest<int>
{
    public required GuardarRolRequestDto Solicitud { get; set; }
}

public class CrearRolCommandHandler : IRequestHandler<CrearRolCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public CrearRolCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(CrearRolCommand request, CancellationToken cancellationToken)
    {
        (string codigo, string nombre, string? descripcion) = ValidacionRol.Normalizar(request.Solicitud);
        await ValidacionRol.ValidarCodigoUnicoAsync(_unidad, codigo, null);
        Rol rol = new Rol { Codigo = codigo, Nombre = nombre, Descripcion = descripcion, EsSistema = false };
        await _unidad.Repository<Rol>().AddAsync(rol);
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
