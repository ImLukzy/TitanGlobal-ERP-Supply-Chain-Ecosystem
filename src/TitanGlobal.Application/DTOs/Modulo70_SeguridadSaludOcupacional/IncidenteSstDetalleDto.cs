namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class IncidenteSstDetalleDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int EmpleadoId { get; set; }

    public string Empleado { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Severidad { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }

    public string? Lugar { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public int DiasBaja { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateOnly? FechaCierre { get; set; }

    public List<AccionCorrectivaSstDto> Acciones { get; set; } = new List<AccionCorrectivaSstDto>();
}
