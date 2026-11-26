namespace TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;

public class GuardarFacturaRequestDto
{
    public int ClienteId { get; set; }

    public int? OrdenCompraId { get; set; }

    public string TipoComprobante { get; set; } = string.Empty;

    public string Serie { get; set; } = string.Empty;

    public int Numero { get; set; }

    public DateTime? FechaEmision { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public string Moneda { get; set; } = "PEN";

    public string? Observaciones { get; set; }

    public List<GuardarDetalleFacturaRequestDto> Detalles { get; set; } = new();
}
