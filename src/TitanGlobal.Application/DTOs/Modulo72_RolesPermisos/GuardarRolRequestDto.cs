namespace TitanGlobal.Application.DTOs.Modulo72_RolesPermisos;

public class GuardarRolRequestDto
{
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}
