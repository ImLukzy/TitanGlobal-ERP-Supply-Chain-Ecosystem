namespace TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;

public class ContratoLaboralDetalleDto : ContratoLaboralListadoDto
{
    public int? ContratoAnteriorId { get; set; }

    public string? ContratoAnteriorNumero { get; set; }

    public DateOnly? FechaCierre { get; set; }

    public string? MotivoCierre { get; set; }

    public string CargoVigente { get; set; } = string.Empty;

    public decimal SueldoVigente { get; set; }

    public List<AdendaContratoDto> Adendas { get; set; } = new List<AdendaContratoDto>();
}
