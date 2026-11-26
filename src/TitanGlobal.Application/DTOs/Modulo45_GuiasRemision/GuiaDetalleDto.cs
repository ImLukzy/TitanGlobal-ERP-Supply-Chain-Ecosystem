namespace TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;

public class GuiaDetalleDto : GuiaListadoDto
{
    public string RemitenteNombre { get; set; } = string.Empty;

    public string RemitenteDocumento { get; set; } = string.Empty;

    public string DireccionOrigen { get; set; } = string.Empty;

    public string MotivoTraslado { get; set; } = string.Empty;

    public string Transportista { get; set; } = string.Empty;

    public string Conductor { get; set; } = string.Empty;

    public string Placa { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public DateTime? FechaEmision { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }

    public List<LineaGuiaDto> Lineas { get; set; } = new();
}
