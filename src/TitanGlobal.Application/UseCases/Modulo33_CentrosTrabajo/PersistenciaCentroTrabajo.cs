using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo;

internal static class PersistenciaCentroTrabajo
{
    public static bool EsViolacionCodigoUnico(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }

    public static ReglaNegocioException CrearConflictoCodigo(string codigo)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe un centro de trabajo con el código {codigo}.");
    }

    public static ReglaNegocioException CrearNoEncontrado(int id)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound,
            $"El centro de trabajo {id} no existe.");
    }
}
