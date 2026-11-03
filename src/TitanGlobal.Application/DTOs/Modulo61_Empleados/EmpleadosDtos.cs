namespace TitanGlobal.Application.DTOs.Modulo61_Empleados;

public class PaginaEmpleadosDto
{
    public List<EmpleadoListadoDto> Items { get; set; } = new List<EmpleadoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class EmpleadoListadoDto
{
    public int Id { get; set; }

    public string Legajo { get; set; } = string.Empty;

    public string TipoDocumento { get; set; } = string.Empty;

    public string NumeroDocumento { get; set; } = string.Empty;

    public string Nombres { get; set; } = string.Empty;

    public string Apellidos { get; set; } = string.Empty;

    public string Cargo { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public DateOnly FechaIngreso { get; set; }

    public string Estado { get; set; } = string.Empty;
}

public class EmpleadoDetalleDto : EmpleadoListadoDto
{
    public DateOnly FechaNacimiento { get; set; }

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public DateOnly? FechaCese { get; set; }

    public string? MotivoCese { get; set; }
}

public class EmpleadoIdDto
{
    public int Id { get; set; }
}
