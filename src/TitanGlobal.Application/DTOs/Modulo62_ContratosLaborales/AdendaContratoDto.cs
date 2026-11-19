namespace TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;

public class AdendaContratoDto
{
    public int Id { get; set; }

    public DateOnly FechaVigencia { get; set; }

    public string Cargo { get; set; } = string.Empty;

    public decimal SueldoBasico { get; set; }
}
