namespace TitanGlobal.Application.DTOs.Modulo71_Usuarios;

public class GuardarUsuarioRequestDto
{
    public string NombreUsuario { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public List<int> RolIds { get; set; } = new List<int>();
}
