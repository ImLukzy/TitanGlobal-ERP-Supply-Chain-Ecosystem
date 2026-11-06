// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Producto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal Precio { get; set; }

    public string? Descripcion { get; set; }

    public virtual ICollection<DetalleCotizacion> DetallesCotizacion { get; set; } = new List<DetalleCotizacion>();

    public virtual ICollection<DetalleDevolucion> DetallesDevolucion { get; set; } = new List<DetalleDevolucion>();

    public virtual ICollection<DetalleOrdenCompra> DetallesOrdenCompra { get; set; } = new List<DetalleOrdenCompra>();

    public virtual ICollection<DetalleOrdenAbastecimiento> DetallesOrdenesAbastecimiento { get; set; } = new List<DetalleOrdenAbastecimiento>();

    public virtual ICollection<DetalleSolicitudCompra> DetallesSolicitudCompra { get; set; } = new List<DetalleSolicitudCompra>();

    public virtual ICollection<ItemListaPrecios> ItemsListaPrecios { get; set; } = new List<ItemListaPrecios>();

    public virtual ICollection<Lote> Lotes { get; set; } = new List<Lote>();

    public virtual ICollection<Promocion> Promociones { get; set; } = new List<Promocion>();

    public virtual ICollection<ReclamoPostventa> ReclamosPostventa { get; set; } = new List<ReclamoPostventa>();

    public virtual ICollection<UnidadSerie> UnidadesSerie { get; set; } = new List<UnidadSerie>();
}
