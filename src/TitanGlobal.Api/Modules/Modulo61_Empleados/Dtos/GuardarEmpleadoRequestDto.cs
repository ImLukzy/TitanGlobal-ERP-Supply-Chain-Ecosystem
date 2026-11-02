namespace TitanGlobal.Api.Modules.Modulo61_Empleados.Dtos;

public class GuardarEmpleadoRequestDto
{
    public string? TipoDocumento { get; set; }

    public string? NumeroDocumento { get; set; }

    public string? Nombres { get; set; }

    public string? Apellidos { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    public DateOnly? FechaIngreso { get; set; }

    public string? Cargo { get; set; }

    public string? Area { get; set; }

    public string? Email { get; set; }

    public string? Telefono { get; set; }
}
