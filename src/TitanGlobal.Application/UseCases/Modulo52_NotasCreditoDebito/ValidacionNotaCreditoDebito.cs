using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

internal static class ValidacionNotaCreditoDebito
{
    private const int LongitudMaximaSerie = 10;
    private const int LongitudMaximaMotivo = 500;
    private const decimal ImporteMaximo = 9999999999999999.99m;

    public static DatosNotaCreditoDebito ValidarSolicitud(GuardarNotaCreditoDebitoRequestDto solicitud)
    {
        ValidarFacturaId(solicitud.FacturaId);
        string tipo = NormalizarTipo(solicitud.Tipo);
        string motivo = NormalizarMotivo(solicitud.Motivo, "El motivo de la nota es obligatorio.");
        string serie = solicitud.Serie?.Trim().ToUpperInvariant() ?? string.Empty;
        if (serie.Length == 0 || serie.Length > LongitudMaximaSerie || solicitud.Numero <= 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La serie o el número de la nota no son válidos.");
        }

        if (solicitud.Importe <= 0m || solicitud.Importe > ImporteMaximo
            || decimal.Round(solicitud.Importe, 2) != solicitud.Importe)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El importe del ajuste no es válido.");
        }

        return new DatosNotaCreditoDebito(
            solicitud.FacturaId, tipo, serie, solicitud.Numero, motivo, solicitud.Importe);
    }

    public static void ValidarFacturaId(int facturaId, int? facturaOriginalId = null)
    {
        if (facturaId <= 0 || (facturaOriginalId.HasValue && facturaId != facturaOriginalId.Value))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Debe indicar una factura válida.");
        }
    }

    public static int ObtenerDesplazamiento(int pagina, int tamanoPagina)
    {
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        if (pagina <= 0 || tamanoPagina < 1 || tamanoPagina > 100 || desplazamiento > int.MaxValue)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La paginación solicitada no es válida.");
        }

        return (int)desplazamiento;
    }

    public static void ValidarEstado(NotaCreditoDebito nota, string estadoRequerido)
    {
        if (nota.Estado != estadoRequerido)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "El estado de la nota no permite esta operación.");
        }
    }

    public static string NormalizarMotivoAnulacion(string? motivo)
    {
        return NormalizarMotivo(motivo, "El motivo de anulación es obligatorio.");
    }

    private static string NormalizarTipo(string? entrada)
    {
        string? tipo = entrada?.Trim();
        if (string.Equals(tipo, NotaCreditoDebito.TipoCredito, StringComparison.OrdinalIgnoreCase))
        {
            return NotaCreditoDebito.TipoCredito;
        }

        if (string.Equals(tipo, NotaCreditoDebito.TipoDebito, StringComparison.OrdinalIgnoreCase))
        {
            return NotaCreditoDebito.TipoDebito;
        }

        throw new ReglaNegocioException(
            CodigosEstado.Status400BadRequest,
            "El tipo de nota debe ser Credito o Debito.");
    }

    private static string NormalizarMotivo(string? entrada, string mensajeObligatorio)
    {
        string motivo = entrada?.Trim() ?? string.Empty;
        if (motivo.Length == 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensajeObligatorio);
        }

        if (motivo.Length > LongitudMaximaMotivo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El motivo de la nota supera la longitud permitida.");
        }

        return motivo;
    }
}

internal sealed record DatosNotaCreditoDebito(
    int FacturaId, string Tipo, string Serie, int Numero, string Motivo, decimal Importe);
