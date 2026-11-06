// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ReclamoPostventa
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ClienteId { get; set; }

    public int OrdenCompraId { get; set; }

    public int? ProductoId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Prioridad { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaLimite { get; set; }

    public string? Resolucion { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public DateTime? FechaCierre { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<DevolucionComercial> DevolucionesComerciales { get; set; } = new List<DevolucionComercial>();

    public virtual OrdenCompra OrdenCompra { get; set; } = null!;

    public virtual Producto? Producto { get; set; }
}
