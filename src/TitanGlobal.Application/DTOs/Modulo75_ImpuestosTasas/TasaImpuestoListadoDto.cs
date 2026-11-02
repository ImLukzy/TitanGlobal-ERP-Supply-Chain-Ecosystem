namespace TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;

public class TasaImpuestoListadoDto
{
    public int Id { get; set; }
    public decimal Porcentaje { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool Activo { get; set; }
    public string? Observacion { get; set; }
}
