namespace TitanGlobal.Application.DTOs.Modulo00_Autenticacion;

public class SesionDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiraEn { get; set; }

    public UsuarioSesionDto Usuario { get; set; } = new UsuarioSesionDto();

    public List<string> Roles { get; set; } = new List<string>();
}

public class UsuarioSesionDto
{
    public int Id { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;
}

public class PerfilDto
{
    public int Id { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new List<string>();
}
