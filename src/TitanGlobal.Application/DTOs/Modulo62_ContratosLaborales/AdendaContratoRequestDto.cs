namespace TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;

public class AdendaContratoRequestDto
{
    public DateOnly? FechaVigencia { get; set; }

    public string? Cargo { get; set; }

    public decimal? SueldoBasico { get; set; }
}
