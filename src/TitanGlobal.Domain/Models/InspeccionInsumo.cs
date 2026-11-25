// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class InspeccionInsumo
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int ProductoId { get; set; }

    public int ProveedorId { get; set; }

    public string Lote { get; set; } = string.Empty;

    public DateOnly FechaInspeccion { get; set; }

    public decimal CantidadRecibida { get; set; }

    public decimal CantidadMuestra { get; set; }

    public string UnidadMedida { get; set; } = string.Empty;

    public decimal ToleranciaDefectosPorcentaje { get; set; }

    public string Inspector { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public decimal? CantidadDefectuosa { get; set; }

    public DateTime? FechaDictamen { get; set; }

    public string? Observaciones { get; set; }

    public string? MotivoAnulacion { get; set; }

    public virtual Producto Producto { get; set; } = null!;

    public virtual Proveedor Proveedor { get; set; } = null!;
}
