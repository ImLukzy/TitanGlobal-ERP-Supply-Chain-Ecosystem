namespace TitanGlobal.Api.Models;

public partial class SolicitudCompra
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string Solicitante { get; set; } = string.Empty;

    public int? ProveedorId { get; set; }

    public virtual Proveedor? Proveedor { get; set; }

    public string Estado { get; set; } = "Pendiente";

    public string? Observaciones { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? FechaDecision { get; set; }

    public virtual ICollection<DetalleSolicitudCompra> Detalles { get; set; } = new List<DetalleSolicitudCompra>();

    public virtual ICollection<OrdenAbastecimiento> OrdenesAbastecimiento { get; set; } = new List<OrdenAbastecimiento>();
}
