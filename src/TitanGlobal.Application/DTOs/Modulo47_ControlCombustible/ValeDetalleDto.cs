namespace TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;

public class ValeDetalleDto : ValeListadoDto
{
    public string? Observaciones { get; set; }

    public DateTime? FechaConfirmacion { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }
}
