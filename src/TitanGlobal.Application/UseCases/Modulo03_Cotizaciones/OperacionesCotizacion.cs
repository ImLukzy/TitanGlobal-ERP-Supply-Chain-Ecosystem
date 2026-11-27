using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones;

// Lógica compartida por los commands y queries de Cotizaciones.
internal static class OperacionesCotizacion
{
    internal static void ExigirEmitida(Cotizacion cotizacion)
    {
        if (cotizacion.Estado != "Emitida")
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La cotización {cotizacion.Numero} ya está {cotizacion.Estado.ToLowerInvariant()}.");
        }
    }

    internal static ReglaNegocioException CrearNoEncontrada(int id)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound,
            $"La cotización {id} no existe.");
    }
}
