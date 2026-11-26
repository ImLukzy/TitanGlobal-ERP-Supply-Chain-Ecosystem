namespace TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;

public class GuardarDetalleFacturaRequestDto
{
    public int ProductoId { get; set; }

    public int? ImpuestoId { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal DescuentoPorcentaje { get; set; }
}
