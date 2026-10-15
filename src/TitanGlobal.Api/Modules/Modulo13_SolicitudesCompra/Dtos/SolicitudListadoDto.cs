namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;

public class SolicitudListadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string Solicitante { get; set; } = string.Empty;

    public int? ProveedorId { get; set; }

    public string? ProveedorNombre { get; set; }

    public string Estado { get; set; } = string.Empty;

    public int TotalLineas { get; set; }

    public decimal MontoEstimado { get; set; }
}
