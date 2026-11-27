namespace TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;

public class AgenteDto
{
    public int Id { get; set; }

    public string Ruc { get; set; } = string.Empty;

    public string RazonSocial { get; set; } = string.Empty;

    public string NumeroRegistro { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public bool Activo { get; set; }

    public int TotalDeclaraciones { get; set; }

    public DateTime FechaRegistro { get; set; }
}
