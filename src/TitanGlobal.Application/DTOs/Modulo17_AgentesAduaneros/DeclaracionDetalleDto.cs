namespace TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;

public class DeclaracionDetalleDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int ExpedienteImportacionId { get; set; }

    public string ExpedienteNumero { get; set; } = string.Empty;

    public int AgenteAduaneroId { get; set; }

    public string AgenteRazonSocial { get; set; } = string.Empty;

    public string Regimen { get; set; } = string.Empty;

    public decimal ValorAduanero { get; set; }

    public decimal TasaArancelaria { get; set; }

    public decimal DerechosArancel { get; set; }

    public string? Canal { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime? FechaNumeracion { get; set; }

    public DateTime? FechaLevante { get; set; }

    public string? MotivoAnulacion { get; set; }

    public DateTime FechaRegistro { get; set; }
}
