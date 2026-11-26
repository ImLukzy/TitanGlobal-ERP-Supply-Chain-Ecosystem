namespace TitanGlobal.Application.DTOs.Modulo28_TomaInventario;

public class ActualizarTomaInventarioRequestDto
{
    public string Responsable { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public List<LineaTomaInventarioRequestDto> Lineas { get; set; } = new List<LineaTomaInventarioRequestDto>();
}
