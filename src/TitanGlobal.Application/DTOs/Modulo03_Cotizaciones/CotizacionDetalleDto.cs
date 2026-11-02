namespace TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;

public class CotizacionDetalleDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public DateTime FechaValidez { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool Vencida { get; set; }
    public string? Observaciones { get; set; }
    public string? MotivoRechazo { get; set; }
    public DateTime? FechaDecision { get; set; }
    public int? OrdenCompraId { get; set; }
    public string? OrdenCompraNumero { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public List<LineaCotizacionDto> Lineas { get; set; } = new List<LineaCotizacionDto>();
}
