namespace TitanGlobal.Api.Modules.Modulo42_Conductores.Dtos;

public class ConductorListadoDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string DocumentoLicencia { get; set; } = string.Empty;

    public DateTime VencimientoLicencia { get; set; }

    public bool Habilitado { get; set; }

    public bool Activo { get; set; }
}
