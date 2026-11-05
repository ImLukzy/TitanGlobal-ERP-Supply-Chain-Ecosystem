namespace TitanGlobal.Application.DTOs.Modulo00_Autenticacion;

public class IniciarSesionRequestDto
{
    public string NombreUsuario { get; set; } = string.Empty;

    public string Contrasena { get; set; } = string.Empty;
}
