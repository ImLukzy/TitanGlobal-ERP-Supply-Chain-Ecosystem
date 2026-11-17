// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class TransferenciaAlmacen
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public int AlmacenOrigenId { get; set; }

    public int AlmacenDestinoId { get; set; }

    public int Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public DateTime Fecha { get; set; }

    public string? Observaciones { get; set; }

    public virtual Almacen AlmacenDestino { get; set; } = null!;

    public virtual Almacen AlmacenOrigen { get; set; } = null!;

    public virtual ICollection<MovimientoKardex> MovimientosKardex { get; set; } = new List<MovimientoKardex>();

    public virtual Producto Producto { get; set; } = null!;
}
