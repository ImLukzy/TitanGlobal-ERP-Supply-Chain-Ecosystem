namespace TitanGlobal.Application.DTOs.Modulo42_Conductores;

public class GuardarConductorRequestDto
{
    public string Nombre { get; set; } = string.Empty;

    public string DocumentoLicencia { get; set; } = string.Empty;

    public DateTime VencimientoLicencia { get; set; }

    public bool Habilitado { get; set; } = true;

    public bool Activo { get; set; } = true;

    public string? Observaciones { get; set; }
}
