// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Cliente
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public virtual ICollection<ContratoComercial> ContratosComerciales { get; set; } = new List<ContratoComercial>();

    public virtual ICollection<Cotizacion> Cotizaciones { get; set; } = new List<Cotizacion>();

    public virtual ICollection<DevolucionComercial> DevolucionesComerciales { get; set; } = new List<DevolucionComercial>();

    public virtual ICollection<OrdenCompra> OrdenesCompra { get; set; } = new List<OrdenCompra>();

    public virtual ICollection<ReclamoPostventa> ReclamosPostventa { get; set; } = new List<ReclamoPostventa>();
}
