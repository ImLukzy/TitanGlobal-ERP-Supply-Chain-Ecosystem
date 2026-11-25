// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class OperacionRutaProduccion
{
    public int Id { get; set; }

    public int RutaProduccionId { get; set; }

    public int Secuencia { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public int CentroTrabajoId { get; set; }

    public decimal MinutosPreparacion { get; set; }

    public decimal MinutosPorUnidad { get; set; }

    public virtual CentroTrabajo CentroTrabajo { get; set; } = null!;

    public virtual RutaProduccion RutaProduccion { get; set; } = null!;
}
