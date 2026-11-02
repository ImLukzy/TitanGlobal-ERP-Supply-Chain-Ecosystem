namespace TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;

public class GuardarTasaImpuestoRequestDto
{
    public decimal Porcentaje { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string? Observacion { get; set; }
}
