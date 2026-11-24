namespace TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;

public class RegistrarFleteTerceroRequestDto
{
    public int TransportistaId { get; set; }

    public string Referencia { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string Origen { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public decimal Importe { get; set; }

    public string Moneda { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}
