namespace TitanGlobal.Application.Services;

public interface IServicioTokens
{
    TokenEmitido Emitir(int usuarioId, string nombreUsuario, IReadOnlyCollection<string> roles);
}

public class TokenEmitido
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiraEn { get; set; }
}
