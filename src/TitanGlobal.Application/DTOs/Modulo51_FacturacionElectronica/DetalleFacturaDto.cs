namespace TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;

public class DetalleFacturaDto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int? ImpuestoId { get; set; }

    public string? ImpuestoNombre { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal DescuentoPorcentaje { get; set; }

    public decimal PorcentajeImpuesto { get; set; }

    public decimal Subtotal { get; set; }

    public decimal ImporteDescuento { get; set; }

    public decimal BaseImponible { get; set; }

    public decimal ImporteImpuesto { get; set; }

    public decimal Total { get; set; }
}
