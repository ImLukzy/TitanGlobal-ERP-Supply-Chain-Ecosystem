namespace TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;

public class DeclaracionListadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public string ExpedienteNumero { get; set; } = string.Empty;

    public string AgenteRazonSocial { get; set; } = string.Empty;

    public string Regimen { get; set; } = string.Empty;

    public string? Canal { get; set; }

    public string Estado { get; set; } = string.Empty;

    public decimal DerechosArancel { get; set; }
}
