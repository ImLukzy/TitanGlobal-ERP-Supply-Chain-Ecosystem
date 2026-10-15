namespace TitanGlobal.Api.Models;

public class ContratoComercial
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int ClienteId { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public string Titulo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public decimal MontoComprometido { get; set; }

    public string CondicionPago { get; set; } = string.Empty;

    public string Estado { get; set; } = "Borrador";

    public string? Observaciones { get; set; }

    public string? MotivoCancelacion { get; set; }

    public DateTime? FechaActivacion { get; set; }

    public DateTime? FechaCierre { get; set; }
}
