namespace TitanGlobal.Api.Modules.Modulo72_RolesPermisos.Dtos;

public class GuardarRolRequestDto
{
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}
