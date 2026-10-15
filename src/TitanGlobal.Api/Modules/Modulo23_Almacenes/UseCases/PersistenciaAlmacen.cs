using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Middlewares;

namespace TitanGlobal.Api.Modules.Modulo23_Almacenes.UseCases;

internal static class PersistenciaAlmacen
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
            $"Ya existe un almacén con el código {codigo}.");
    }
}
