namespace TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;

public class LineaOrdenAbastecimientoRequestDto
{
    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }
}
