using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;

public static class ValidacionFactura
{
    public const string TipoFactura = "Factura";

    public const string TipoBoleta = "Boleta";

    public const string EstadoBorrador = "Borrador";

    public const string EstadoEmitida = "Emitida";

    public const string EstadoAnulada = "Anulada";

    public const decimal ImporteMaximo = 9999999999999999.99m;

    public const decimal CantidadMaxima = 99999999999999.9999m;

    public static void ValidarSolicitud(GuardarFacturaRequestDto request)
    {
        if (request.ClienteId <= 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Debe seleccionar un cliente válido.");
        }

        if (request.OrdenCompraId.HasValue &&
            request.OrdenCompraId.Value <= 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La orden de compra no es válida.");
        }

        request.TipoComprobante =
            NormalizarTipoComprobante(request.TipoComprobante);

        request.Serie = (request.Serie ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(request.Serie))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La serie del comprobante es obligatoria.");
        }

        if (request.Serie.Length > 10)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La serie no puede superar los 10 caracteres.");
        }

        if (request.Numero <= 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El número del comprobante debe ser mayor que cero.");
        }

        if (!request.FechaEmision.HasValue ||
            request.FechaEmision.Value == default)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha de emisión es obligatoria.");
        }

        request.FechaEmision =
            FechasUtc.Normalizar(request.FechaEmision.Value);

        if (request.FechaVencimiento.HasValue)
        {
            request.FechaVencimiento =
                FechasUtc.Normalizar(request.FechaVencimiento.Value);

            if (request.FechaVencimiento.Value <
                request.FechaEmision.Value)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    "La fecha de vencimiento no puede ser anterior a la fecha de emisión.");
            }
        }

        request.Moneda = (request.Moneda ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        if (request.Moneda.Length != 3 ||
            !request.Moneda.All(caracter => caracter >= 'A' && caracter <= 'Z'))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La moneda debe tener un código de tres letras.");
        }

        request.Observaciones =
            NormalizarTextoOpcional(request.Observaciones, 500);

        if (request.Detalles == null ||
            request.Detalles.Count == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La factura debe contener al menos un producto.");
        }

        foreach (GuardarDetalleFacturaRequestDto detalle in request.Detalles)
        {
            ValidarDetalle(detalle);
        }
    }

    public static void ValidarDetalle(
        GuardarDetalleFacturaRequestDto detalle)
    {
        if (detalle == null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El detalle de la factura no es válido.");
        }

        if (detalle.ProductoId <= 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Debe seleccionar un producto válido.");
        }

        if (detalle.ImpuestoId.HasValue &&
            detalle.ImpuestoId.Value <= 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El impuesto seleccionado no es válido.");
        }

        detalle.Descripcion =
            (detalle.Descripcion ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(detalle.Descripcion))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La descripción del producto es obligatoria.");
        }

        if (detalle.Descripcion.Length > 300)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La descripción no puede superar los 300 caracteres.");
        }

        if (detalle.Cantidad <= 0 ||
            detalle.Cantidad > CantidadMaxima)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La cantidad del producto no es válida.");
        }

        if (decimal.Round(detalle.Cantidad, 4) != detalle.Cantidad)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La cantidad admite como máximo cuatro decimales.");
        }

        if (detalle.PrecioUnitario < 0 ||
            detalle.PrecioUnitario > ImporteMaximo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El precio unitario no es válido.");
        }

        if (decimal.Round(detalle.PrecioUnitario, 2) !=
            detalle.PrecioUnitario)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El precio unitario admite como máximo dos decimales.");
        }

        if (detalle.DescuentoPorcentaje < 0 ||
            detalle.DescuentoPorcentaje > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El descuento debe estar entre 0 y 100.");
        }

        if (decimal.Round(detalle.DescuentoPorcentaje, 2) !=
            detalle.DescuentoPorcentaje)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El descuento admite como máximo dos decimales.");
        }

        if (detalle.PrecioUnitario > 0 &&
            detalle.Cantidad > ImporteMaximo / detalle.PrecioUnitario)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El subtotal del producto supera el importe permitido.");
        }
    }

    public static string NormalizarTipoComprobante(string? tipo)
    {
        string valor = (tipo ?? string.Empty).Trim();

        if (string.Equals(
            valor,
            TipoFactura,
            StringComparison.OrdinalIgnoreCase))
        {
            return TipoFactura;
        }

        if (string.Equals(
            valor,
            TipoBoleta,
            StringComparison.OrdinalIgnoreCase))
        {
            return TipoBoleta;
        }

        throw new ReglaNegocioException(
            CodigosEstado.Status400BadRequest,
            "El tipo de comprobante debe ser Factura o Boleta.");
    }

    public static string NormalizarMotivoAnulacion(string? motivo)
    {
        string valor = (motivo ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El motivo de anulación es obligatorio.");
        }

        if (valor.Length > 500)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El motivo de anulación no puede superar los 500 caracteres.");
        }

        return valor;
    }

    public static string? NormalizarTextoOpcional(
        string? texto,
        int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        string valor = texto.Trim();

        if (valor.Length > longitudMaxima)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                $"El texto no puede superar los {longitudMaxima} caracteres.");
        }

        return valor;
    }

    public static decimal RedondearImporte(decimal importe)
    {
        return decimal.Round(
            importe,
            2,
            MidpointRounding.AwayFromZero);
    }

    public static void ValidarImporte(decimal importe)
    {
        if (importe < 0 || importe > ImporteMaximo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El importe calculado supera los límites permitidos.");
        }
    }
}
