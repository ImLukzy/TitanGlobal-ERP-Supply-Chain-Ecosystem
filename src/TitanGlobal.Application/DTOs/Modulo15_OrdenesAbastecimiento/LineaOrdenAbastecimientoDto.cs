namespace TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;

public class LineaOrdenAbastecimientoDto
{
    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }
}
