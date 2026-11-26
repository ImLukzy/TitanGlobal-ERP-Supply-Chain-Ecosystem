using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo59_CentrosCosto;

internal static class ConsultasCentroCosto
{
    internal static async Task<CentroCosto> CargarParaEscrituraAsync(IQueryable<CentroCosto> consulta, int id)
    {
        return await consulta.FirstOrDefaultAsync(centro => centro.Id == id)
            ?? throw new ReglaNegocioException(CodigosEstado.Status404NotFound, ValidacionCentroCosto.CentroInexistente);
    }

    internal static async Task<CentroCostoDetalleDto> ObtenerDetalleAsync(IQueryable<CentroCosto> consulta, int id)
    {
        // Proyectar el nivel en la misma consulta mantiene una vista consistente del árbol.
        return await consulta.AsNoTracking()
            .Where(centro => centro.Id == id)
            .Select(centro => new CentroCostoDetalleDto
            {
                Id = centro.Id,
                Codigo = centro.Codigo,
                Nombre = centro.Nombre,
                Descripcion = centro.Descripcion,
                ResponsableEmpleadoId = centro.ResponsableEmpleadoId,
                CentroCostoPadreId = centro.CentroCostoPadreId,
                Nivel = centro.CentroCostoPadre == null ? 1
                    : centro.CentroCostoPadre.CentroCostoPadreId == null ? 2 : 3,
                Activo = centro.Activo,
                FechaRegistro = centro.FechaRegistro,
                FechaDesactivacion = centro.FechaDesactivacion
            })
            .SingleOrDefaultAsync()
            ?? throw new ReglaNegocioException(CodigosEstado.Status404NotFound, ValidacionCentroCosto.CentroInexistente);
    }
}
