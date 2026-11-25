namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class RegistrarIncidenteSstRequestDto
{
    public int? EmpleadoId { get; set; }

    public string? Tipo { get; set; }

    public string? Severidad { get; set; }

    public DateOnly? Fecha { get; set; }

    public string? Lugar { get; set; }

    public string? Descripcion { get; set; }

    public int? DiasBaja { get; set; }
}
