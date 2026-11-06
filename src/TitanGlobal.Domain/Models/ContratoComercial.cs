// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ContratoComercial
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int ClienteId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public decimal MontoComprometido { get; set; }

    public string CondicionPago { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public string? MotivoCancelacion { get; set; }

    public DateTime? FechaActivacion { get; set; }

    public DateTime? FechaCierre { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;
}
