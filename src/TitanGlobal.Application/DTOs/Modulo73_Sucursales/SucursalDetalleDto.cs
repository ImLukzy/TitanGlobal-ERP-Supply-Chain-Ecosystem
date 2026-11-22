namespace TitanGlobal.Application.DTOs.Modulo73_Sucursales;

public class SucursalDetalleDto : SucursalListadoDto
{
    public string Direccion { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Observaciones { get; set; }
    public DateTime? FechaApertura { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaCambioEstado { get; set; }
}
