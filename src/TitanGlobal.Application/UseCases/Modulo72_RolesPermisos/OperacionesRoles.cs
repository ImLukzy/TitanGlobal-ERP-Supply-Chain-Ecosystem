using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo72_RolesPermisos;

// Lógica compartida por los commands y queries de Roles.
internal static class OperacionesRoles
{
    internal static bool EsCodigoDuplicado(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }

    internal static ReglaNegocioException ConflictoCodigo(string codigo)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Ya existe un rol con el código {codigo}.");
    }

    internal static ReglaNegocioException ConflictoSistema(string codigo)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"El rol {codigo} es de sistema y no se puede modificar de esa forma.");
    }
}
