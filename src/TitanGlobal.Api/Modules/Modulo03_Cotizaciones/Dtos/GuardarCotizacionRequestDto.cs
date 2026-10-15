namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;

public class GuardarCotizacionRequestDto
{
    public int ClienteId { get; set; }
    public DateTime FechaValidez { get; set; }
    public string? Observaciones { get; set; }
    public List<LineaCotizacionRequestDto>? Lineas { get; set; }
}
