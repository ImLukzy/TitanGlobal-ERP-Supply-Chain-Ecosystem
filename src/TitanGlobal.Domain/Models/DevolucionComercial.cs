namespace TitanGlobal.Domain.Models;

public class DevolucionComercial
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ClienteId { get; set; }

    public int OrdenCompraId { get; set; }

    public int? ReclamoPostventaId { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public string Estado { get; set; } = "Solicitada";

    public string? Observaciones { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    public DateTime? FechaRechazo { get; set; }

    public virtual ICollection<DetalleDevolucion> Detalles { get; set; } = new List<DetalleDevolucion>();
}
