// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class MovimientoKardex
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public int AlmacenId { get; set; }

    public int? LoteId { get; set; }

    public DateTime Fecha { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal? CostoUnitario { get; set; }

    public string? Referencia { get; set; }

    public int? TransferenciaId { get; set; }

    public int? AjusteInventarioId { get; set; }

    public virtual AjusteInventario? AjusteInventario { get; set; }

    public virtual Almacen Almacen { get; set; } = null!;

    public virtual Lote? Lote { get; set; }

    public virtual Producto Producto { get; set; } = null!;

    public virtual TransferenciaAlmacen? Transferencia { get; set; }
}
