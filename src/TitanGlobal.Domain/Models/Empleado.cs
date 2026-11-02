namespace TitanGlobal.Domain.Models;

public class Empleado
{
    public int Id { get; set; }

    public string Legajo { get; set; } = string.Empty;

    public string TipoDocumento { get; set; } = string.Empty;

    public string NumeroDocumento { get; set; } = string.Empty;

    public string Nombres { get; set; } = string.Empty;

    public string Apellidos { get; set; } = string.Empty;

    public DateOnly FechaNacimiento { get; set; }

    public DateOnly FechaIngreso { get; set; }

    public string Cargo { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateOnly? FechaCese { get; set; }

    public string? MotivoCese { get; set; }
}
