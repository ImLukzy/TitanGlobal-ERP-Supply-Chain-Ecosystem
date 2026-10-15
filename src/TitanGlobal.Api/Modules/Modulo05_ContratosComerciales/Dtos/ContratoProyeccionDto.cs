namespace TitanGlobal.Api.Modules.Modulo05_ContratosComerciales.Dtos;

public class ContratoProyeccionDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int ClienteId { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string Estado { get; set; } = string.Empty;

    public decimal MontoComprometido { get; set; }

    public decimal MontoConsumido { get; set; }

    public string CondicionPago { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public string? MotivoCancelacion { get; set; }

    public DateTime? FechaActivacion { get; set; }

    public DateTime? FechaCierre { get; set; }
}
