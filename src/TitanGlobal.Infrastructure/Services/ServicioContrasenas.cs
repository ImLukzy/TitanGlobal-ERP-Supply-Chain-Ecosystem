using TitanGlobal.Application.Services;

namespace TitanGlobal.Infrastructure.Services;

public class ServicioContrasenas : IServicioContrasenas
{
    private const int FactorTrabajo = 11;

    public string Encriptar(string contrasena)
    {
        return BCrypt.Net.BCrypt.HashPassword(contrasena, FactorTrabajo);
    }

    public bool Verificar(string contrasena, string contrasenaHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(contrasena, contrasenaHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
