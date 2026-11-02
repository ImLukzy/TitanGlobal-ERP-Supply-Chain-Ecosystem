namespace TitanGlobal.Application.DTOs.Modulo42_Conductores;

public class ConductorDetalleDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string DocumentoLicencia { get; set; } = string.Empty;

    public DateTime VencimientoLicencia { get; set; }

    public bool Habilitado { get; set; }

    public bool Activo { get; set; }

    public string? Observaciones { get; set; }
}
