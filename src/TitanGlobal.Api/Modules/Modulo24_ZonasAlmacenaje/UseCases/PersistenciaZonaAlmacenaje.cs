using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TitanGlobal.Api.Modules.Modulo24_ZonasAlmacenaje.UseCases;

internal static class PersistenciaZonaAlmacenaje
{
    public static bool EsViolacionIndiceUnico(DbUpdateException excepcion)
    {
        return excepcion.InnerException is PostgresException excepcionPostgres
            && excepcionPostgres.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
