namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;

public class GuardarSolicitudRequestDto
{
    public string Solicitante { get; set; } = string.Empty;

    public int? ProveedorId { get; set; }

    public string? Observaciones { get; set; }

    public List<LineaSolicitudRequestDto>? Lineas { get; set; } = new List<LineaSolicitudRequestDto>();
}
