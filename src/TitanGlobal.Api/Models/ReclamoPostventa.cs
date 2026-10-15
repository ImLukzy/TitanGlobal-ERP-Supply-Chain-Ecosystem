namespace TitanGlobal.Api.Models;

public class ReclamoPostventa
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ClienteId { get; set; }

    public int OrdenCompraId { get; set; }

    public int? ProductoId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Prioridad { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Estado { get; set; } = "Abierto";

    public DateTime FechaLimite { get; set; }

    public string? Resolucion { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public DateTime? FechaCierre { get; set; }
}
