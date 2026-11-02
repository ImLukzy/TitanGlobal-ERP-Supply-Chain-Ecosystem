using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas;

public static class ValidacionImpuesto
{
    public const int LongitudMaximaCodigo = 30;
    public const int LongitudMaximaNombre = 120;
    public const int LongitudMaximaDescripcion = 250;
    public const int LongitudMaximaObservacion = 250;

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
                CodigosEstado.Status400BadRequest,
                "El código del impuesto es obligatorio.");

        if (impuesto.Codigo.Length > LongitudMaximaCodigo)
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código del impuesto no puede superar 30 caracteres.");

        if (string.IsNullOrWhiteSpace(impuesto.Nombre))
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El nombre del impuesto es obligatorio.");

        if (impuesto.Nombre.Length > LongitudMaximaNombre)
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El nombre del impuesto no puede superar 120 caracteres.");

        if (!TiposPermitidos.Contains(impuesto.Tipo))
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tipo de impuesto no es válido.");

        if (impuesto.Pais.Length != 3)
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El país debe indicarse mediante un código de tres caracteres.");

        if (impuesto.Descripcion != null &&
            impuesto.Descripcion.Length > LongitudMaximaDescripcion)
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La descripción del impuesto no puede superar 250 caracteres.");
    }

    public static DateTime NormalizarDia(DateTime fecha)
    {
        DateTime dia = DateTime.SpecifyKind(
            fecha.Date,
            DateTimeKind.Unspecified);

        return FechasUtc.Normalizar(dia);
    }

    public static void ValidarTasa(
        decimal porcentaje,
        DateTime fechaInicio,
        DateTime? fechaFin,
        string? observacion)
    {
        if (fechaInicio == default)
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha de inicio es obligatoria.");

        if (porcentaje <= 0 || porcentaje > 100)
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El porcentaje debe ser mayor que 0 y no superar 100.");

        if (fechaFin.HasValue && fechaFin.Value.Date < fechaInicio.Date)
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha final no puede ser anterior a la fecha inicial.");

        if (observacion != null &&
            observacion.Length > LongitudMaximaObservacion)
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La observación de la tasa no puede superar 250 caracteres.");
    }
}
