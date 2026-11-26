// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class LineaGuiaRemision
{
    public int Id { get; set; }

    public int GuiaRemisionId { get; set; }

    public int Posicion { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string UnidadMedida { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public virtual GuiaRemision GuiaRemision { get; set; } = null!;
}
