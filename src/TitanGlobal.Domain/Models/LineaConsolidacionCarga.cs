// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class LineaConsolidacionCarga
{
    public int Id { get; set; }

    public int ConsolidacionCargaId { get; set; }

    public int Posicion { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public int CantidadRecogida { get; set; }

    public int UnidadesPorBulto { get; set; }

    public decimal LargoCm { get; set; }

    public decimal AnchoCm { get; set; }

    public decimal AltoCm { get; set; }

    public decimal PesoBultoKg { get; set; }

    public virtual ConsolidacionCarga ConsolidacionCarga { get; set; } = null!;
}
