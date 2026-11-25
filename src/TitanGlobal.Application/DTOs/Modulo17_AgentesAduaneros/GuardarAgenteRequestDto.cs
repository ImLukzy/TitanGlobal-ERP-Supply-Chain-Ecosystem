namespace TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;

public class GuardarAgenteRequestDto
{
    public string Ruc { get; set; } = string.Empty;

    public string RazonSocial { get; set; } = string.Empty;

    public string NumeroRegistro { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }
}
