namespace TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;

public class RenovarContratoLaboralRequestDto
{
    public string? Modalidad { get; set; }

    public DateOnly? FechaFin { get; set; }

    public decimal? SueldoBasico { get; set; }

    public string? Cargo { get; set; }
}
