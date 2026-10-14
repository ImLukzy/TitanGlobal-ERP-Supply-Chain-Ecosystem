using TitanGlobal.Api.Middlewares;

namespace TitanGlobal.Api.Modules.Modulo01_Clientes.UseCases;

internal static class ValidacionCliente
{
    public static string NormalizarNombre(string? nombre)
    {
        string nombreNormalizado = nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El nombre del cliente es obligatorio.");
        }

        if (nombreNormalizado.Length > 200)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El nombre del cliente no puede superar 200 caracteres.");
        }

        return nombreNormalizado;
    }

    public static string NormalizarRuc(string? ruc)
    {
        string rucNormalizado = ruc?.Trim() ?? string.Empty;
        if (rucNormalizado.Length != 11 || !rucNormalizado.All(digito => digito >= '0' && digito <= '9'))
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El RUC debe tener 11 dígitos numéricos.");
        }

        return rucNormalizado;
    }

    public static string? NormalizarDescripcion(string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            return null;
        }

        return descripcion.Trim();
    }
}
