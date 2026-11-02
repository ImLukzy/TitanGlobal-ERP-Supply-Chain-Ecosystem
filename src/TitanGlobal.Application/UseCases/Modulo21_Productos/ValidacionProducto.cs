using TitanGlobal.Application.DTOs.Modulo21_Productos;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo21_Productos;

internal static class ValidacionProducto
{
    public static string NormalizarNombre(string? nombre)
    {
        string nombreNormalizado = nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El nombre del producto es obligatorio.");
        }

        if (nombreNormalizado.Length > 200)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El nombre del producto no puede superar 200 caracteres.");
        }

        return nombreNormalizado;
    }

    public static string NormalizarCategoria(string? categoria)
    {
        string categoriaNormalizada = categoria?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(categoriaNormalizada))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La categoría es obligatoria.");
        }

        if (categoriaNormalizada.Length > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La categoría no puede superar 100 caracteres.");
        }

        return categoriaNormalizada;
    }

    public static void ValidarPrecio(decimal precio)
    {
        if (precio <= 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El precio debe ser mayor que 0.");
        }
    }

    public static string? NormalizarDescripcion(string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            return null;
        }

        return descripcion.Trim();
    }

    public static void ValidarFiltros(int pagina, int tamanoPagina, decimal? precioMin, decimal? precioMax)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        if ((precioMin.HasValue && precioMin.Value < 0) || (precioMax.HasValue && precioMax.Value < 0))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Los precios del filtro no pueden ser negativos.");
        }

        if (precioMin.HasValue && precioMax.HasValue && precioMin.Value > precioMax.Value)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El precio mínimo no puede ser mayor que el máximo.");
        }
    }

    public static void ValidarSolicitud(GuardarProductoRequestDto solicitud)
    {
        solicitud.Nombre = NormalizarNombre(solicitud.Nombre);
        solicitud.Categoria = NormalizarCategoria(solicitud.Categoria);
        ValidarPrecio(solicitud.Precio);
        solicitud.Descripcion = NormalizarDescripcion(solicitud.Descripcion);
    }
}
