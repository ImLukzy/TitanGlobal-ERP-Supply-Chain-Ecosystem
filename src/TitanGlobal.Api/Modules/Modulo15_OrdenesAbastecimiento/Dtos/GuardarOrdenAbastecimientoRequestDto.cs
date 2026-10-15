namespace TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Dtos;

public class GuardarOrdenAbastecimientoRequestDto
{
    public int ProveedorId { get; set; }

    public DateTime FechaEntregaEstimada { get; set; }

    public string? Observaciones { get; set; }

    public List<LineaOrdenAbastecimientoRequestDto>? Lineas { get; set; }
}
