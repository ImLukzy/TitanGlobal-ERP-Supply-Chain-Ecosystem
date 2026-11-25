namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class EquipoProteccionListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int VidaUtilDias { get; set; }

    public bool Activo { get; set; }
}
