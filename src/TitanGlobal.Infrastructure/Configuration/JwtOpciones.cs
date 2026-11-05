namespace TitanGlobal.Infrastructure.Configuration;

public class JwtOpciones
{
    public const int LongitudMinimaClave = 32;

    public string Clave { get; set; } = string.Empty;

    public string Emisor { get; set; } = "titanglobal-api";

    public string Audiencia { get; set; } = "titanglobal-web";

    public int MinutosExpiracion { get; set; } = 480;

    public byte[] ObtenerBytesClave()
    {
        return System.Text.Encoding.UTF8.GetBytes(Clave);
    }
}
