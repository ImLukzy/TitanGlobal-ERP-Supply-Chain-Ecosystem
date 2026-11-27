namespace TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;

public class CrearDeclaracionRequestDto
{
    public string Numero { get; set; } = string.Empty;

    public int ExpedienteImportacionId { get; set; }

    public int AgenteAduaneroId { get; set; }

    public string Regimen { get; set; } = string.Empty;

    public decimal ValorAduanero { get; set; }

    public decimal TasaArancelaria { get; set; }
}
