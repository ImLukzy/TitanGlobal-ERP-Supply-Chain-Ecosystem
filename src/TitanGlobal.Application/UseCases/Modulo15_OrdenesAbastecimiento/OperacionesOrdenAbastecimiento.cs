using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento;

// Lógica compartida por los commands y queries de OrdenesAbastecimiento.
internal static class OperacionesOrdenAbastecimiento
{
    internal static void ExigirEmitida(OrdenAbastecimiento orden)
    {
        if (orden.Estado != "Emitida")
        {
            string estadoNormalizado = orden.Estado.ToLowerInvariant();
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La orden {orden.Numero} ya está {estadoNormalizado}.");
        }
    }
}
