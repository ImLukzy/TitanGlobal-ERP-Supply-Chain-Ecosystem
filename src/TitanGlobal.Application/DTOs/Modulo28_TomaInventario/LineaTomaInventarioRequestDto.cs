namespace TitanGlobal.Application.DTOs.Modulo28_TomaInventario;

public class LineaTomaInventarioRequestDto
{
    public int ProductoId { get; set; }

    public int CantidadContada { get; set; }

    public string? Observacion { get; set; }
}
