namespace TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;

public class ValeListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Placa { get; set; } = string.Empty;

    public string Estacion { get; set; } = string.Empty;

    public DateTime FechaCarga { get; set; }

    public string TipoCombustible { get; set; } = string.Empty;

    public string Litros { get; set; } = string.Empty;

    public string PrecioLitro { get; set; } = string.Empty;

    public string Moneda { get; set; } = string.Empty;

    public int OdometroInicial { get; set; }

    public int? OdometroFinal { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string Importe { get; set; } = string.Empty;

    public int? KilometrosRecorridos { get; set; }

    public string? KmPorLitro { get; set; }

    public string? LitrosPor100Km { get; set; }
}
