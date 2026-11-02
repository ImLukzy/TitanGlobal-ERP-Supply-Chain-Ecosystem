namespace TitanGlobal.Domain.Models;

public partial class OrdenAbastecimiento
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ProveedorId { get; set; }

    public virtual Proveedor Proveedor { get; set; } = null!;

    public int? SolicitudCompraId { get; set; }

    public virtual SolicitudCompra? SolicitudCompra { get; set; }

    public string Estado { get; set; } = "Emitida";

    public DateTime FechaEntregaEstimada { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    public string? Observaciones { get; set; }

    public string? MotivoAnulacion { get; set; }

    public virtual ICollection<DetalleOrdenAbastecimiento> Detalles { get; set; } = new List<DetalleOrdenAbastecimiento>();
}
