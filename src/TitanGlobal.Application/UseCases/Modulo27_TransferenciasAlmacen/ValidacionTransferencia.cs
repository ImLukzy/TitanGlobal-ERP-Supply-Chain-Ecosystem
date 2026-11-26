using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo27_TransferenciasAlmacen;

internal static class ValidacionTransferencia
{
    public static void ValidarAlmacenesDistintos(int origenId, int destinoId)
    {
        if (origenId == destinoId)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El almacén de origen y el de destino deben ser distintos.");
        }
    }

    public static int ValidarCantidad(int cantidad)
    {
        if (cantidad < 1 || cantidad > 1000000)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La cantidad de la transferencia debe estar entre 1 y 1000000.");
        }

        return cantidad;
    }

    public static string? NormalizarObservaciones(string? observaciones)
    {
        string valor = observaciones?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        if (valor.Length > 500)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Las observaciones no pueden superar 500 caracteres.");
        }

        return valor;
    }

    public static ReglaNegocioException CrearNoEncontrada(int id)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound,
            $"La transferencia {id} no existe.");
    }
}
