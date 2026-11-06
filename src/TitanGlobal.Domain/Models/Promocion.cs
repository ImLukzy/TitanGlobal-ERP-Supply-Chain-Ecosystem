// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class Promocion
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public int? ProductoId { get; set; }

    public decimal? MontoMinimo { get; set; }

    public decimal PorcentajeDescuento { get; set; }

    public int? SegmentoClienteId { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public bool Activa { get; set; }

    public virtual Producto? Producto { get; set; }

    public virtual SegmentoCliente? SegmentoCliente { get; set; }
}
