namespace TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;

public sealed class LineaNotaCreditoDebitoDto
{
    public int Id { get; set; }
    public int DetalleFacturaId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal ImporteImpuesto { get; set; }
    public decimal Total { get; set; }
}
