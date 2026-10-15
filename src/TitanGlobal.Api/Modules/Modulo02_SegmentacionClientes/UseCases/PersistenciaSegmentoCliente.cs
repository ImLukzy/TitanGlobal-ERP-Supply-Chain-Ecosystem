using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.UseCases;

internal static class PersistenciaSegmentoCliente
{
    public static async Task ValidarUnicidadAsync(
        IUnitOfWork unitOfWork,
        string codigo,
        int prioridad,
        int? idIgnorado)
    {
        bool codigoExiste = await unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .AnyAsync(segmento => segmento.Codigo == codigo && (!idIgnorado.HasValue || segmento.Id != idIgnorado.Value));
        if (codigoExiste)
        {
            throw CrearConflictoCodigo(codigo);
        }

        bool prioridadExiste = await unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .AnyAsync(segmento => segmento.Prioridad == prioridad && (!idIgnorado.HasValue || segmento.Id != idIgnorado.Value));
        if (prioridadExiste)
        {
            throw CrearConflictoPrioridad(prioridad);
        }
    }

    public static bool EsViolacionUnica(DbUpdateException excepcion)
    {
        PostgresException? postgres = excepcion.InnerException as PostgresException;
        return postgres?.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    public static ReglaNegocioException TraducirViolacionUnica(DbUpdateException excepcion, string codigo, int prioridad)
    {
        PostgresException? postgres = excepcion.InnerException as PostgresException;
        if (postgres?.ConstraintName?.EndsWith("Codigo", StringComparison.Ordinal) == true)
        {
            return CrearConflictoCodigo(codigo);
        }

        if (postgres?.ConstraintName?.EndsWith("Prioridad", StringComparison.Ordinal) == true)
        {
            return CrearConflictoPrioridad(prioridad);
        }

        return CrearConflictoCodigo(codigo);
    }

    private static ReglaNegocioException CrearConflictoCodigo(string codigo)
    {
        return new ReglaNegocioException(
            StatusCodes.Status409Conflict,
            $"Ya existe un segmento con el código {codigo}.");
    }

    private static ReglaNegocioException CrearConflictoPrioridad(int prioridad)
    {
        return new ReglaNegocioException(
            StatusCodes.Status409Conflict,
            $"Ya existe un segmento con la prioridad {prioridad}.");
    }
}
