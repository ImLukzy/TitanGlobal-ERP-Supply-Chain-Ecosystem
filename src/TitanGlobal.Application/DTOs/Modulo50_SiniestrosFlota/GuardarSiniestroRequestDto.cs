namespace TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;

public class GuardarSiniestroRequestDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public string Lugar { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateTime? FechaIncidente { get; set; }
    public string? Observaciones { get; set; }
}
