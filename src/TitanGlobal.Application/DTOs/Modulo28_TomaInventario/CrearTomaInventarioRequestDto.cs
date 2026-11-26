namespace TitanGlobal.Application.DTOs.Modulo28_TomaInventario;

public class CrearTomaInventarioRequestDto
{
    public string Codigo { get; set; } = string.Empty;

    public int AlmacenId { get; set; }

    public DateTime? FechaToma { get; set; }

    public string Responsable { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public List<LineaTomaInventarioRequestDto> Lineas { get; set; } = new List<LineaTomaInventarioRequestDto>();
}
