namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;

public class LineaCotizacionDto
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Importe { get; set; }
}
