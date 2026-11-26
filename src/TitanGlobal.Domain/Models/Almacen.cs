// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Almacen
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    public string Ciudad { get; set; } = string.Empty;

    public decimal CapacidadM3 { get; set; }

    public string? Responsable { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<MovimientoKardex> MovimientosKardex { get; set; } = new List<MovimientoKardex>();

    public virtual ICollection<TomaInventario> TomasInventario { get; set; } = new List<TomaInventario>();

    public virtual ICollection<TransferenciaAlmacen> TransferenciasAlmacenAlmacenDestino { get; set; } = new List<TransferenciaAlmacen>();

    public virtual ICollection<TransferenciaAlmacen> TransferenciasAlmacenAlmacenOrigen { get; set; } = new List<TransferenciaAlmacen>();

    public virtual ICollection<ZonaAlmacenaje> Zonas { get; set; } = new List<ZonaAlmacenaje>();
}
