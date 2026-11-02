using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes;

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

    public static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }

    public static ReglaNegocioException TraducirViolacionUnica(ViolacionRestriccionException excepcion, string codigo, int prioridad)
    {
        if (excepcion.Restriccion?.EndsWith("Codigo", StringComparison.Ordinal) == true)
        {
            return CrearConflictoCodigo(codigo);
        }

        if (excepcion.Restriccion?.EndsWith("Prioridad", StringComparison.Ordinal) == true)
        {
            return CrearConflictoPrioridad(prioridad);
        }

        return CrearConflictoCodigo(codigo);
    }

    private static ReglaNegocioException CrearConflictoCodigo(string codigo)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe un segmento con el código {codigo}.");
    }

    private static ReglaNegocioException CrearConflictoPrioridad(int prioridad)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe un segmento con la prioridad {prioridad}.");
    }
}
