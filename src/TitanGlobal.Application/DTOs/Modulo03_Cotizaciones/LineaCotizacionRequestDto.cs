namespace TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;

public class LineaCotizacionRequestDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
}
