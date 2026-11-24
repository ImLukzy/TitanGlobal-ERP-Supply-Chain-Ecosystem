namespace TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;

public class FleteTerceroListadoDto
{
    public int Id { get; set; }

    public int TransportistaId { get; set; }

    public string TransportistaRazonSocial { get; set; } = string.Empty;

    public string Referencia { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string Origen { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public string Importe { get; set; } = string.Empty;

    public string Moneda { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;
}
