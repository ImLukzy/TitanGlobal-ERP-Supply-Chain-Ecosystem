namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.Dtos;

public class CalcularImpuestoResponseDto
{
    public int ImpuestoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal BaseImponible { get; set; }
    public decimal PorcentajeAplicado { get; set; }
    public decimal ImporteImpuesto { get; set; }
    public decimal Total { get; set; }
    public DateTime FechaOperacion { get; set; }
}
