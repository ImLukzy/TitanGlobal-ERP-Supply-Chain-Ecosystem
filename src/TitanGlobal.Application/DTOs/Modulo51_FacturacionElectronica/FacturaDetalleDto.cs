namespace TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;

public class FacturaDetalleDto
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;

    public string ClienteRuc { get; set; } = string.Empty;

    public int? OrdenCompraId { get; set; }

    public string? NumeroOrdenCompra { get; set; }

    public string TipoComprobante { get; set; } = string.Empty;

    public string Serie { get; set; } = string.Empty;

    public int Numero { get; set; }

    public string NumeroComprobante { get; set; } = string.Empty;

    public DateTime FechaEmision { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public string Moneda { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }

    public decimal TotalDescuento { get; set; }

    public decimal TotalImpuesto { get; set; }

    public decimal Total { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }

    public List<DetalleFacturaDto> Detalles { get; set; } = new();
}
