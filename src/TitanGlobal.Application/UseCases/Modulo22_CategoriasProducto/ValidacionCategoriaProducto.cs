using TitanGlobal.Application.Exceptions;


namespace TitanGlobal.Application.UseCases.Modulo22_CategoriasProducto;

internal static class ValidacionCategoriaProducto
{
    public static string NormalizarNombre(string? nombre)
    {
        string valor = nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El nombre de la categoría es obligatorio.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El nombre de la categoría no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static string? NormalizarDescripcion(string? descripcion)
    {
        string valor = descripcion?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        if (valor.Length > 250)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La descripción de la categoría no puede superar 250 caracteres.");
        }

        return valor;
    }

    public static ReglaNegocioException CrearConflictoNombre(string nombre)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe una categoría con el nombre {nombre}.");
    }

    public static ReglaNegocioException CrearNoEncontrada(int id)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound,
            $"La categoría {id} no existe.");
    }
}
