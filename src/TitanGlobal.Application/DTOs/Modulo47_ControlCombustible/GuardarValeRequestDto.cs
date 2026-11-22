namespace TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;

public class GuardarValeRequestDto
{
    public string Codigo { get; set; } = string.Empty;

    public string Placa { get; set; } = string.Empty;

    public string Estacion { get; set; } = string.Empty;

    public DateTime? FechaCarga { get; set; }

    public string TipoCombustible { get; set; } = string.Empty;

    public decimal Litros { get; set; }

    public decimal PrecioLitro { get; set; }

    public string Moneda { get; set; } = string.Empty;

    public int OdometroInicial { get; set; }

    public int? OdometroFinal { get; set; }

    public string? Observaciones { get; set; }
}
