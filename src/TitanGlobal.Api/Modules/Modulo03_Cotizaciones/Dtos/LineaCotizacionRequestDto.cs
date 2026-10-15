namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;

public class LineaCotizacionRequestDto
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
}
