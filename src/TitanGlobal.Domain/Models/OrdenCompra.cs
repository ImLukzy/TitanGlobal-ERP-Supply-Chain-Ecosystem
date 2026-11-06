// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class OrdenCompra
{
    public int Id { get; set; }

    public string NumeroOrden { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public decimal Total { get; set; }

    public string? Descripcion { get; set; }

    public int ClienteId { get; set; }

    public virtual AsignacionVenta? AsignacionVenta { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<Cotizacion> Cotizaciones { get; set; } = new List<Cotizacion>();

    public virtual ICollection<DetalleOrdenCompra> Detalles { get; set; } = new List<DetalleOrdenCompra>();

    public virtual ICollection<DevolucionComercial> DevolucionesComerciales { get; set; } = new List<DevolucionComercial>();

    public virtual ICollection<ReclamoPostventa> ReclamosPostventa { get; set; } = new List<ReclamoPostventa>();
}
