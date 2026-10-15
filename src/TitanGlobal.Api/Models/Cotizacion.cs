namespace TitanGlobal.Api.Models;

public partial class Cotizacion
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ClienteId { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public DateTime FechaValidez { get; set; }

    public string Estado { get; set; } = "Emitida";

    public string? Observaciones { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? FechaDecision { get; set; }

    public int? OrdenCompraId { get; set; }

    public virtual OrdenCompra? OrdenCompra { get; set; }

    public virtual ICollection<DetalleCotizacion> Detalles { get; set; } = new List<DetalleCotizacion>();
}
