namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.Dtos;

public class GuardarTasaImpuestoRequestDto
{
    public decimal Porcentaje { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string? Observacion { get; set; }
}
