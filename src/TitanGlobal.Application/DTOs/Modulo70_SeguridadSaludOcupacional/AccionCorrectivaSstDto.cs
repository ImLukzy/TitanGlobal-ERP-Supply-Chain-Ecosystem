namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class AccionCorrectivaSstDto
{
    public int Id { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public string Responsable { get; set; } = string.Empty;

    public DateOnly FechaCompromiso { get; set; }

    public bool Completada { get; set; }

    public DateOnly? FechaCompletada { get; set; }
}
