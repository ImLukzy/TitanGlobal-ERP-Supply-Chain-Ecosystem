namespace TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;

public class GuardarCotizacionRequestDto
{
    public int ClienteId { get; set; }
    public DateTime FechaValidez { get; set; }
    public string? Observaciones { get; set; }
    public List<LineaCotizacionRequestDto>? Lineas { get; set; }
}
