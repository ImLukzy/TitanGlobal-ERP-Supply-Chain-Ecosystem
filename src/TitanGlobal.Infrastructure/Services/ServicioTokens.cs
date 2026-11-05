using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TitanGlobal.Application.Services;
using TitanGlobal.Infrastructure.Configuration;

namespace TitanGlobal.Infrastructure.Services;

public class ServicioTokens : IServicioTokens
{
    private readonly JwtOpciones _opciones;
    private readonly JsonWebTokenHandler _manejador = new JsonWebTokenHandler();

    public ServicioTokens(JwtOpciones opciones)
    {
        _opciones = opciones;
    }

    public TokenEmitido Emitir(int usuarioId, string nombreUsuario, IReadOnlyCollection<string> roles)
    {
        DateTime ahora = DateTime.UtcNow;
        DateTime expiraEn = ahora.AddMinutes(_opciones.MinutosExpiracion);
        List<Claim> claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Name, nombreUsuario)
        };
        foreach (string rol in roles)
        {
            claims.Add(new Claim("role", rol));
        }

        SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _opciones.Emisor,
            Audience = _opciones.Audiencia,
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = expiraEn,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_opciones.ObtenerBytesClave()),
                SecurityAlgorithms.HmacSha256)
        };

        return new TokenEmitido
        {
            Token = _manejador.CreateToken(descriptor),
            ExpiraEn = expiraEn
        };
    }
}
