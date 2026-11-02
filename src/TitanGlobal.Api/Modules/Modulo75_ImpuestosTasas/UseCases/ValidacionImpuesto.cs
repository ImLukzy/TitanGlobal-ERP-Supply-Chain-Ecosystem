using Microsoft.AspNetCore.Http;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;

namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.UseCases;

public static class ValidacionImpuesto
{
    private static readonly string[] TiposPermitidos =
    {
        "IGV",
        "IVA",
        "RETENCION",
        "PERCEPCION",
        "DETRACCION"
    };

    public static void ValidarDatos(Impuesto impuesto)
    {
        impuesto.Codigo = impuesto.Codigo.Trim().ToUpperInvariant();
        impuesto.Nombre = impuesto.Nombre.Trim();
        impuesto.Tipo = impuesto.Tipo.Trim().ToUpperInvariant();
        impuesto.Pais = impuesto.Pais.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(impuesto.Codigo))
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El código del impuesto es obligatorio.");

        if (string.IsNullOrWhiteSpace(impuesto.Nombre))
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El nombre del impuesto es obligatorio.");

        if (!TiposPermitidos.Contains(impuesto.Tipo))
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El tipo de impuesto no es válido.");

        if (impuesto.Pais.Length != 3)
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El país debe indicarse mediante un código de tres caracteres.");
    }

    public static void ValidarTasa(
        decimal porcentaje,
        DateTime fechaInicio,
        DateTime? fechaFin)
    {
        if (porcentaje <= 0 || porcentaje > 100)
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El porcentaje debe ser mayor que 0 y no superar 100.");

        if (fechaFin.HasValue && fechaFin.Value.Date < fechaInicio.Date)
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "La fecha final no puede ser anterior a la fecha inicial.");
    }
}
