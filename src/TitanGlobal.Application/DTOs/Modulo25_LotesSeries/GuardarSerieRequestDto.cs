namespace TitanGlobal.Application.DTOs.Modulo25_LotesSeries;

public class GuardarSerieRequestDto
{
    public int ProductoId { get; set; }

    public int? LoteId { get; set; }

    public string NumeroSerie { get; set; } = string.Empty;
}
