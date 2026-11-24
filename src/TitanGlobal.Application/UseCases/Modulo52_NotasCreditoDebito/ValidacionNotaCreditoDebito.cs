using System.Text;
using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

internal static class ValidacionNotaCreditoDebito
{
    public const decimal ImporteMaximo = 9999999999999999.99m;
    private const decimal CantidadMaxima = 99999999999999.9999m;

    public static void ValidarFacturaId(int facturaId)
    {
        if (facturaId <= 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "Debe indicar una factura válida.");
        }
    }

    public static string NormalizarMotivo(string? entrada)
    {
        string motivo = entrada?.Trim() ?? string.Empty;
        int longitud = motivo.EnumerateRunes().Count();
        if (longitud < 3 || longitud > 300)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest, "El motivo debe tener entre 3 y 300 caracteres.");
        }

        return motivo;
    }

    public static List<DetalleNotaCreditoDebitoRequestDto> ValidarDetalles(
        List<DetalleNotaCreditoDebitoRequestDto?>? detalles)
    {
        if (detalles is null || detalles.Count == 0)
        {
            throw DetalleInvalido();
        }

        HashSet<int> ids = new();
        List<DetalleNotaCreditoDebitoRequestDto> validados = new();
        foreach (DetalleNotaCreditoDebitoRequestDto? detalle in detalles)
        {
            if (detalle is null || detalle.DetalleFacturaId <= 0 || !ids.Add(detalle.DetalleFacturaId))
            {
                throw DetalleInvalido();
            }

            if (detalle.Cantidad <= 0 || detalle.Cantidad > CantidadMaxima
                || decimal.Round(detalle.Cantidad, 4) != detalle.Cantidad
                || detalle.PrecioUnitario <= 0 || detalle.PrecioUnitario > ImporteMaximo
                || decimal.Round(detalle.PrecioUnitario, 2) != detalle.PrecioUnitario)
            {
                throw ImporteInvalido();
            }

            validados.Add(detalle);
        }

        return validados;
    }

    public static string ValidarTipo(string? entrada)
    {
        return NormalizarTipo(entrada) ?? throw new ReglaNegocioException(
            CodigosEstado.Status400BadRequest, "El tipo de nota debe ser Credito o Debito.");
    }

    public static string? NormalizarTipo(string? entrada)
    {
        return entrada?.Trim().ToUpperInvariant() switch
        {
            "CREDITO" => NotaCreditoDebito.TipoCredito,
            "DEBITO" => NotaCreditoDebito.TipoDebito,
            _ => null
        };
    }

    public static string? NormalizarEstado(string? entrada)
    {
        return entrada?.Trim().ToUpperInvariant() switch
        {
            "BORRADOR" => NotaCreditoDebito.EstadoBorrador,
            "EMITIDA" => NotaCreditoDebito.EstadoEmitida,
            "ANULADA" => NotaCreditoDebito.EstadoAnulada,
            _ => null
        };
    }

    public static int ObtenerDesplazamiento(int pagina, int tamanoPagina)
    {
        long desplazamiento = ((long)pagina - 1) * tamanoPagina;
        if (pagina <= 0 || tamanoPagina < 1 || tamanoPagina > 100 || desplazamiento > int.MaxValue)
        {
            throw FiltroInvalido();
        }

        return (int)desplazamiento;
    }

    public static void ValidarEstado(NotaCreditoDebito nota, string estadoRequerido)
    {
        if (nota.Estado != estadoRequerido)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict, "El estado de la nota no permite esta operación.");
        }
    }

    public static ReglaNegocioException FiltroInvalido()
    {
        return new ReglaNegocioException(
            CodigosEstado.Status400BadRequest, "Los filtros o la paginación de notas no son válidos.");
    }

    public static ReglaNegocioException ImporteInvalido()
    {
        return new ReglaNegocioException(
            CodigosEstado.Status400BadRequest, "La cantidad o el importe de la nota no es válido.");
    }

    private static ReglaNegocioException DetalleInvalido()
    {
        return new ReglaNegocioException(
            CodigosEstado.Status400BadRequest, "Indique al menos una línea válida y sin duplicados.");
    }
}
