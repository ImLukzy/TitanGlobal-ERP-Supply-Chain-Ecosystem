namespace TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;

public class GuardarConsolidacionRequestDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public List<LineaCargaRequestDto>? Lineas { get; set; }
}
