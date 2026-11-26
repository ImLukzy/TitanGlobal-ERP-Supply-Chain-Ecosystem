// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ReglaReabastecimiento
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public int ProductoId { get; set; }

    public int AlmacenId { get; set; }

    public int? ProveedorPreferidoId { get; set; }

    public int StockActual { get; set; }

    public int StockMinimo { get; set; }

    public int PuntoReorden { get; set; }

    public int StockMaximo { get; set; }

    public int CantidadLoteCompra { get; set; }

    public bool Activa { get; set; }

    public DateTime? UltimaEvaluacion { get; set; }

    public string? Observaciones { get; set; }

    public virtual Almacen Almacen { get; set; } = null!;

    public virtual ICollection<EjecucionReabastecimiento> EjecucionesReabastecimiento { get; set; } = new List<EjecucionReabastecimiento>();

    public virtual Producto Producto { get; set; } = null!;

    public virtual Proveedor? ProveedorPreferido { get; set; }
}
