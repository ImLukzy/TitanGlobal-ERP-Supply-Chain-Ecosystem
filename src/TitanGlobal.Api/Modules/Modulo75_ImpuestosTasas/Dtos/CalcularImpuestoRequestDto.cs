namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.Dtos;

public class CalcularImpuestoRequestDto
{
    public int ImpuestoId { get; set; }
    public decimal BaseImponible { get; set; }
    public DateTime FechaOperacion { get; set; }
}
