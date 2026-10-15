namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;

public class SolicitudDetalleDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string Solicitante { get; set; } = string.Empty;

    public int? ProveedorId { get; set; }

    public string? ProveedorNombre { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? FechaDecision { get; set; }

    public decimal MontoEstimado { get; set; }

    public List<LineaSolicitudDto> Lineas { get; set; } = new List<LineaSolicitudDto>();
}
