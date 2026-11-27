using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo06_ListasPrecios;

// Lógica compartida por los commands y queries de ListasPrecios.
internal static class OperacionesListasPrecios
{
    internal static ReglaNegocioException Error(int codigo, string mensaje)
    {
        return new ReglaNegocioException(codigo, mensaje);
    }
}
