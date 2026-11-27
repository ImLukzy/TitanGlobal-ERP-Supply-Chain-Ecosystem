namespace TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;

public class ActualizarDeclaracionRequestDto
{
    public int AgenteAduaneroId { get; set; }

    public string Regimen { get; set; } = string.Empty;

    public decimal ValorAduanero { get; set; }

    public decimal TasaArancelaria { get; set; }
}
