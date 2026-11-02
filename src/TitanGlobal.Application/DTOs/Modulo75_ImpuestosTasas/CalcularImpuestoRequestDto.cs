namespace TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;

public class CalcularImpuestoRequestDto
{
    public int ImpuestoId { get; set; }
    public decimal BaseImponible { get; set; }
    public DateTime FechaOperacion { get; set; }
}
