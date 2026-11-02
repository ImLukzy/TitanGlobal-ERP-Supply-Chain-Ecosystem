namespace TitanGlobal.Application.DTOs.Modulo13_SolicitudesCompra;

public class GuardarSolicitudRequestDto
{
    public string Solicitante { get; set; } = string.Empty;

    public int? ProveedorId { get; set; }

    public string? Observaciones { get; set; }

    public List<LineaSolicitudRequestDto>? Lineas { get; set; } = new List<LineaSolicitudRequestDto>();
}
