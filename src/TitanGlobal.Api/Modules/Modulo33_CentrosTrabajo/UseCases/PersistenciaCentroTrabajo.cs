using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Middlewares;

namespace TitanGlobal.Api.Modules.Modulo33_CentrosTrabajo.UseCases;

internal static class PersistenciaCentroTrabajo
{
    public static bool EsViolacionCodigoUnico(DbUpdateException excepcion)
    {
        return excepcion.InnerException is PostgresException excepcionPostgres
            && excepcionPostgres.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    public static ReglaNegocioException CrearConflictoCodigo(string codigo)
    {
        return new ReglaNegocioException(
            StatusCodes.Status409Conflict,
            $"Ya existe un centro de trabajo con el código {codigo}.");
    }

    public static ReglaNegocioException CrearNoEncontrado(int id)
    {
        return new ReglaNegocioException(
            StatusCodes.Status404NotFound,
            $"El centro de trabajo {id} no existe.");
    }
}
