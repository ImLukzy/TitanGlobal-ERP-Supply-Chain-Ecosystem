namespace TitanGlobal.Application.Services;

public interface IServicioContrasenas
{
    string Encriptar(string contrasena);

    bool Verificar(string contrasena, string contrasenaHash);
}
